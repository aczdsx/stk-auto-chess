namespace CookApps.AutoChess
{
    /// <summary>
    /// 별 합성 시스템. 동일 챔피언 N개 → 1개 상위 별 유닛.
    /// 기본 N=3이지만 시너지 효과로 N=2로 감소 가능.
    /// 합성 후 재귀적으로 상위 합성도 체크.
    /// </summary>
    public static class CombineSystem
    {
        public const int DefaultCombineCount = 3;
        private const int MaxCombineTargets = 3; // 최대 합성 재료 수

        // 재사용 배열 (GC 방지)
        private static readonly int[] _targetBuffer = new int[MaxCombineTargets];
        private static readonly int[] _candidateBuffer = new int[GameWorld.MaxUnits];

        /// <summary>
        /// 자동 합성 시도. 유닛 획득/배치 후 호출.
        /// 합성 가능하면 실행하고 true 반환. 재귀적으로 연쇄 합성도 처리.
        /// </summary>
        public static bool TryAutoCombine(GameWorld world, byte playerIndex, int championSpecId)
        {
            int requiredCount = GetRequiredCombineCount(world, playerIndex);
            byte maxStar = world.Config.MaxStarLevel;
            bool anyCombined = false;

            // 1★ 합성 → 2★ 합성 순서로 체크
            for (byte starLevel = 1; starLevel < maxStar; starLevel++)
            {
                while (FindCombineTargets(world, playerIndex, championSpecId, starLevel, requiredCount, _targetBuffer))
                {
                    ExecuteCombine(world, playerIndex, _targetBuffer, requiredCount, starLevel);
                    anyCombined = true;
                }
            }

            return anyCombined;
        }

        /// <summary>현재 플레이어의 합성 필요 수량 (시너지 효과 반영)</summary>
        public static int GetRequiredCombineCount(GameWorld world, byte playerIndex)
        {
            // TODO: Phase 4에서 시너지 효과 조회 (합성 수량 감소 버프)
            return world.Config.DefaultCombineCount;
        }

        /// <summary>합성 대상 찾기. 같은 ChampionSpecId + 같은 StarLevel인 유닛 requiredCount개.</summary>
        private static bool FindCombineTargets(GameWorld world, byte playerIndex,
            int specId, byte starLevel, int requiredCount, int[] outTargets)
        {
            int found = 0;

            // 보드 유닛 우선 검색 (보드 유닛이 합성 결과 위치를 결정)
            for (int i = 0; i < GameWorld.MaxUnits && found < requiredCount; i++)
            {
                ref var unit = ref world.Units[i];
                if (!unit.IsValid) continue;
                if (unit.OwnerIndex != playerIndex) continue;
                if (unit.ChampionSpecId != specId) continue;
                if (unit.StarLevel != starLevel) continue;
                if (unit.Location == UnitLocation.Board)
                {
                    outTargets[found++] = i;
                }
            }

            // 벤치 유닛 검색
            for (int i = 0; i < GameWorld.MaxUnits && found < requiredCount; i++)
            {
                ref var unit = ref world.Units[i];
                if (!unit.IsValid) continue;
                if (unit.OwnerIndex != playerIndex) continue;
                if (unit.ChampionSpecId != specId) continue;
                if (unit.StarLevel != starLevel) continue;
                if (unit.Location == UnitLocation.Bench)
                {
                    // 이미 추가된 인덱스 중복 체크
                    bool alreadyAdded = false;
                    for (int j = 0; j < found; j++)
                    {
                        if (outTargets[j] == i) { alreadyAdded = true; break; }
                    }
                    if (!alreadyAdded)
                        outTargets[found++] = i;
                }
            }

            return found >= requiredCount;
        }

        /// <summary>합성 실행. 재료 N개 제거, 결과 1개 생성.</summary>
        private static void ExecuteCombine(GameWorld world, byte playerIndex,
            int[] targetIndices, int targetCount, byte sourceStarLevel)
        {
            // 결과 유닛 위치 결정: 첫 번째 보드 유닛의 위치, 없으면 첫 번째 벤치
            int resultIndex = targetIndices[0];
            ref var resultUnit = ref world.Units[resultIndex];
            UnitLocation resultLocation = resultUnit.Location;
            byte resultCol = resultUnit.BoardCol;
            byte resultRow = resultUnit.BoardRow;
            byte resultBench = resultUnit.BenchIndex;
            int resultEntityId = resultUnit.EntityId;
            int specId = resultUnit.ChampionSpecId;

            // 아이템 수집: 재료 유닛(1~N)의 장착 아이템을 결과 유닛으로 이전
            TransferItemsFromMaterials(world, playerIndex, targetIndices, targetCount);

            // 재료 유닛 제거 (첫 번째 제외)
            for (int i = 1; i < targetCount; i++)
            {
                int idx = targetIndices[i];
                ref var material = ref world.Units[idx];

                // 위치에서 제거
                if (material.Location == UnitLocation.Board)
                {
                    int boardIdx = BoardHelper.ToIndex(material.BoardCol, material.BoardRow);
                    world.BoardSlots[playerIndex][boardIdx] = UnitData.InvalidId;
                    world.Boards[playerIndex].UnitCount--;
                }
                else if (material.Location == UnitLocation.Bench)
                {
                    world.BenchSlots[playerIndex][material.BenchIndex] = UnitData.InvalidId;
                    world.Boards[playerIndex].BenchCount--;
                }

                // 유닛 데이터 초기화
                world.Units[idx] = UnitData.CreateEmpty();
            }

            // 결과 유닛 승급
            resultUnit.StarLevel = (byte)(sourceStarLevel + 1);

            // 별 보정 스탯 재계산
            BoardSystem.ApplySpecStats(world, ref resultUnit);
        }

        /// <summary>재료 유닛들의 장착 아이템을 결과 유닛으로 이전. 슬롯 초과분은 인벤토리로.</summary>
        private static void TransferItemsFromMaterials(GameWorld world, byte playerIndex,
            int[] targetIndices, int targetCount)
        {
            int resultIndex = targetIndices[0];
            ref var resultUnit = ref world.Units[resultIndex];
            int resultEntityId = resultUnit.EntityId;

            // 재료 유닛(1~N)의 아이템 수집 → 결과 유닛 빈 슬롯 또는 인벤토리
            for (int i = 1; i < targetCount; i++)
            {
                ref var material = ref world.Units[targetIndices[i]];

                for (int s = 0; s < UnitData.MaxItemSlots; s++)
                {
                    int itemId = material.GetItemSlot(s);
                    if (itemId == UnitData.InvalidId) continue;

                    // 아이템 인스턴스 조회
                    int itemIdx = ItemSystem.FindItemIndex(world, itemId);
                    if (itemIdx < 0) continue;

                    // 재료 유닛 슬롯 해제
                    material.SetItemSlot(s, UnitData.InvalidId);

                    // 결과 유닛에 빈 슬롯이 있으면 장착
                    int emptySlot = resultUnit.GetEmptyItemSlot();
                    if (emptySlot != UnitData.InvalidId)
                    {
                        resultUnit.SetItemSlot(emptySlot, itemId);
                        world.Items[itemIdx].EquippedEntityId = resultEntityId;
                        world.Items[itemIdx].SlotIndex = (byte)emptySlot;
                        // Location은 Equipped 유지
                    }
                    else
                    {
                        // 슬롯 초과 → 인벤토리로
                        world.Items[itemIdx].Location = ItemLocation.Inventory;
                        world.Items[itemIdx].EquippedEntityId = UnitData.InvalidId;
                        world.Items[itemIdx].SlotIndex = 0;
                        ItemSystem.AddToInventory(world, playerIndex, itemId);
                    }
                }
            }
        }
    }
}
