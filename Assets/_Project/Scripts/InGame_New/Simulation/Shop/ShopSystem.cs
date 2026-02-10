namespace CookApps.AutoChess
{
    /// <summary>
    /// 상점 시스템. 공유 챔피언 풀에서 추출하여 상점 갱신.
    /// 구매, 판매, 리롤, 잠금 처리.
    /// </summary>
    public static class ShopSystem
    {
        // ── 상점 갱신 ──

        /// <summary>
        /// 모든 생존 플레이어의 상점 갱신.
        /// Preparation 진입 시 호출 (잠금된 상점은 스킵).
        /// </summary>
        public static void RefreshAllShops(GameWorld world)
        {
            if (!world.Config.EnableShop) return;
            if (world.Pool == null) return;

            for (int i = 0; i < world.Config.PlayerCount; i++)
            {
                if (!world.Players[i].IsAlive) continue;
                if (world.ShopLocked[i]) continue; // 잠금 상점은 유지

                RefreshShop(world, (byte)i);
            }
        }

        /// <summary>개별 플레이어 상점 갱신</summary>
        public static void RefreshShop(GameWorld world, byte playerIndex)
        {
            // 기존 미구매 슬롯의 챔피언을 풀에 반환
            ReturnUnsoldToPool(world, playerIndex);

            // 새 챔피언 추출
            ref var economy = ref world.Economies[playerIndex];
            var config = world.Config;
            var pool = world.Pool;

            for (int slot = 0; slot < config.ShopSlotCount; slot++)
            {
                world.Shops[playerIndex][slot] = ExtractChampionForSlot(
                    pool, config, economy.Level, ref world.RNG);
            }
        }

        /// <summary>미구매 슬롯의 챔피언을 풀에 반환</summary>
        private static void ReturnUnsoldToPool(GameWorld world, byte playerIndex)
        {
            var config = world.Config;
            var pool = world.Pool;

            for (int slot = 0; slot < config.ShopSlotCount; slot++)
            {
                ref var shopSlot = ref world.Shops[playerIndex][slot];
                if (shopSlot.ChampionSpecId != 0 && !shopSlot.IsPurchased)
                {
                    ReturnChampionToPool(pool, shopSlot.ChampionSpecId, 1);
                }
                shopSlot = ShopSlot.CreateEmpty();
            }
        }

        /// <summary>레어리티 롤 → 챔피언 추출하여 ShopSlot 생성</summary>
        private static ShopSlot ExtractChampionForSlot(
            ChampionPool pool, GameConfig config, byte playerLevel, ref DeterministicRNG rng)
        {
            // 1. 레어리티 결정
            int rarity = RollRarity(config, playerLevel, ref rng);

            // 2. 해당 레어리티에서 사용 가능한 챔피언 탐색
            int specIndex = PickAvailableChampion(pool, rarity, ref rng);

            // 풀 고갈 시 대안 레어리티 시도
            if (specIndex < 0)
            {
                specIndex = FindAlternativeChampion(pool, rarity, ref rng);
            }

            // 여전히 없으면 빈 슬롯
            if (specIndex < 0)
            {
                return ShopSlot.CreateEmpty();
            }

            // 3. 풀에서 차감 (예약)
            pool.Stock[specIndex]--;

            // 4. ShopSlot 생성
            return new ShopSlot
            {
                ChampionSpecId = pool.Specs[specIndex].ChampionId,
                Cost = pool.Specs[specIndex].Cost,
                IsPurchased = false,
            };
        }

        /// <summary>레벨 기반 레어리티 롤 (확률 테이블 참조)</summary>
        private static int RollRarity(GameConfig config, byte level, ref DeterministicRNG rng)
        {
            if (level <= 0 || level >= config.RarityOdds.Length || config.RarityOdds[level] == null)
                return 1;

            int[] odds = config.RarityOdds[level];
            int roll = rng.Range(0, 100);
            int cumulative = 0;

            for (int i = 0; i < odds.Length; i++)
            {
                cumulative += odds[i];
                if (roll < cumulative)
                    return i + 1; // 레어리티 1-5
            }

            return 1; // 기본값
        }

        /// <summary>특정 레어리티에서 재고 있는 챔피언 랜덤 선택</summary>
        private static int PickAvailableChampion(ChampionPool pool, int rarity, ref DeterministicRNG rng)
        {
            int r = rarity - 1;
            if (r < 0 || r >= 5) return -1;

            int count = pool.RarityIndexCounts[r];
            if (count <= 0) return -1;

            // 사용 가능한 챔피언 수 세기
            int availableCount = 0;
            for (int i = 0; i < count; i++)
            {
                int specIdx = pool.RarityIndices[r][i];
                if (pool.Stock[specIdx] > 0)
                    availableCount++;
            }

            if (availableCount <= 0) return -1;

            // 랜덤 선택 (균등 가중치)
            int pick = rng.Range(0, availableCount);
            int current = 0;
            for (int i = 0; i < count; i++)
            {
                int specIdx = pool.RarityIndices[r][i];
                if (pool.Stock[specIdx] > 0)
                {
                    if (current == pick)
                        return specIdx;
                    current++;
                }
            }

            return -1;
        }

        /// <summary>레어리티 고갈 시 대안 탐색 (인접 레어리티부터)</summary>
        private static int FindAlternativeChampion(ChampionPool pool, int originalRarity, ref DeterministicRNG rng)
        {
            // 낮은 레어리티 → 높은 레어리티 순서로 탐색
            for (int delta = 1; delta <= 4; delta++)
            {
                int lower = originalRarity - delta;
                if (lower >= 1)
                {
                    int idx = PickAvailableChampion(pool, lower, ref rng);
                    if (idx >= 0) return idx;
                }

                int higher = originalRarity + delta;
                if (higher <= 5)
                {
                    int idx = PickAvailableChampion(pool, higher, ref rng);
                    if (idx >= 0) return idx;
                }
            }
            return -1;
        }

        // ── 구매 ──

        /// <summary>상점에서 챔피언 구매</summary>
        public static bool TryPurchase(GameWorld world, byte playerIndex, int shopSlotIndex)
        {
            if (!world.Config.EnableShop) return false;

            var config = world.Config;
            if (shopSlotIndex < 0 || shopSlotIndex >= config.ShopSlotCount) return false;

            ref var slot = ref world.Shops[playerIndex][shopSlotIndex];
            if (!slot.IsAvailable) return false;

            ref var economy = ref world.Economies[playerIndex];

            // 골드 체크
            if (economy.Gold < slot.Cost) return false;

            // 벤치 공간 체크
            int benchIdx = FindEmptyBenchSlot(world, playerIndex);
            if (benchIdx < 0) return false;

            // 골드 차감
            economy.Gold -= slot.Cost;

            // 유닛 생성 (벤치에 배치)
            int specIndex = FindSpecIndex(world.Pool, slot.ChampionSpecId);
            if (specIndex < 0)
            {
                // 스펙을 찾을 수 없으면 기본 유닛 생성 (StarLevel=1)
                int entityId = BoardSystem.CreateUnit(world, playerIndex, slot.ChampionSpecId, 1);
                if (entityId != UnitData.InvalidId)
                {
                    slot.IsPurchased = true;
                    CombineSystem.TryAutoCombine(world, playerIndex, slot.ChampionSpecId);
                }
                return entityId != UnitData.InvalidId;
            }

            // 스펙 기반 유닛 생성
            int unitEntityId = CreateUnitFromSpec(world, playerIndex, specIndex);
            if (unitEntityId == UnitData.InvalidId) return false;

            slot.IsPurchased = true;

            // 자동 합성 체크
            CombineSystem.TryAutoCombine(world, playerIndex, slot.ChampionSpecId);

            return true;
        }

        /// <summary>챔피언 스펙 기반으로 유닛 생성 (벤치에 배치)</summary>
        private static int CreateUnitFromSpec(GameWorld world, byte playerIndex, int specIndex)
        {
            var spec = world.Pool.Specs[specIndex];

            // BoardSystem.CreateUnit이 벤치 배치 + ApplySpecStats까지 처리
            return BoardSystem.CreateUnit(world, playerIndex, spec.ChampionId, 1);
        }

        // ── 판매 ──

        /// <summary>유닛 판매: 골드 획득 + 풀 반환</summary>
        public static bool TrySellUnit(GameWorld world, byte playerIndex, int entityId)
        {
            int unitIdx = world.FindUnitIndex(entityId);
            if (unitIdx < 0) return false;

            ref var unit = ref world.Units[unitIdx];
            if (unit.OwnerIndex != playerIndex) return false;

            // 판매 가격
            int sellPrice = EconomySystem.GetSellPrice(
                (byte)GetChampionCost(world, unit.ChampionSpecId), unit.StarLevel);
            world.Economies[playerIndex].Gold += sellPrice;

            // 풀에 반환 (별 레벨에 따른 수량)
            int returnCount = GetReturnCount(unit.StarLevel);
            ReturnChampionToPool(world.Pool, unit.ChampionSpecId, returnCount);

            // 보드/벤치에서 제거
            BoardSystem.RemoveUnit(world, playerIndex, entityId);

            return true;
        }

        /// <summary>별 레벨에 따른 풀 반환 수량 (1★=1, 2★=3, 3★=9)</summary>
        private static int GetReturnCount(byte starLevel)
        {
            return starLevel switch
            {
                1 => 1,
                2 => 3,
                3 => 9,
                _ => 1,
            };
        }

        // ── 리롤 ──

        /// <summary>상점 리롤 (2골드)</summary>
        public static bool TryReroll(GameWorld world, byte playerIndex)
        {
            if (!world.Config.EnableShop) return false;

            ref var economy = ref world.Economies[playerIndex];
            if (economy.Gold < world.Config.RerollCost) return false;

            economy.Gold -= world.Config.RerollCost;

            // 잠금 해제 후 갱신
            world.ShopLocked[playerIndex] = false;
            RefreshShop(world, playerIndex);

            return true;
        }

        // ── 잠금 ──

        /// <summary>상점 잠금/해제 토글</summary>
        public static void ToggleLock(GameWorld world, byte playerIndex)
        {
            world.ShopLocked[playerIndex] = !world.ShopLocked[playerIndex];
        }

        // ── 풀 유틸리티 ──

        /// <summary>챔피언을 풀에 반환</summary>
        public static void ReturnChampionToPool(ChampionPool pool, int championSpecId, int count)
        {
            if (pool == null) return;
            int specIndex = FindSpecIndexInPool(pool, championSpecId);
            if (specIndex < 0) return;

            pool.Stock[specIndex] += count;

            // 최대치 제한 (원래 풀 사이즈 초과 방지는 하지 않음 - 정상 흐름에선 발생 안 함)
        }

        /// <summary>플레이어 탈락 시 모든 유닛 + 미구매 상점을 풀에 반환</summary>
        public static void ReturnAllToPool(GameWorld world, byte playerIndex)
        {
            if (world.Pool == null) return;

            // 상점 미구매 슬롯 반환
            ReturnUnsoldToPool(world, playerIndex);

            // 보드 + 벤치 유닛 반환
            for (int i = 0; i < GameWorld.MaxUnits; i++)
            {
                ref var unit = ref world.Units[i];
                if (!unit.IsValid || unit.OwnerIndex != playerIndex) continue;

                int returnCount = GetReturnCount(unit.StarLevel);
                ReturnChampionToPool(world.Pool, unit.ChampionSpecId, returnCount);
            }
        }

        private static int FindSpecIndex(ChampionPool pool, int championSpecId)
        {
            return FindSpecIndexInPool(pool, championSpecId);
        }

        private static int FindSpecIndexInPool(ChampionPool pool, int championSpecId)
        {
            if (pool == null) return -1;
            for (int i = 0; i < pool.SpecCount; i++)
            {
                if (pool.Specs[i].ChampionId == championSpecId)
                    return i;
            }
            return -1;
        }

        private static int FindEmptyBenchSlot(GameWorld world, byte playerIndex)
        {
            for (int i = 0; i < PlayerBoard.BenchSize; i++)
            {
                if (world.BenchSlots[playerIndex][i] == UnitData.InvalidId)
                    return i;
            }
            return -1;
        }

        private static int GetChampionCost(GameWorld world, int championSpecId)
        {
            if (world.Pool != null)
            {
                int idx = FindSpecIndexInPool(world.Pool, championSpecId);
                if (idx >= 0) return world.Pool.Specs[idx].Cost;
            }
            return 1; // 기본값
        }
    }
}
