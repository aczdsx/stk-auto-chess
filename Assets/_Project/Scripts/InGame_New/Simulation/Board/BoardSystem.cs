namespace CookApps.AutoChess
{
    /// <summary>
    /// 보드 시스템. 유닛 배치, 이동, 회수, 교환, 판매를 처리.
    /// 모든 메서드는 GameWorld 상태를 직접 변경하는 순수 함수.
    /// </summary>
    public static class BoardSystem
    {
        /// <summary>유닛 생성 후 벤치에 추가. 성공 시 EntityId 반환, 실패 시 -1.</summary>
        public static int CreateUnit(GameWorld world, byte ownerIndex, int championSpecId, byte starLevel)
        {
            // 빈 유닛 슬롯 찾기
            int unitIndex = world.AllocateUnit();
            if (unitIndex < 0) return UnitData.InvalidId;

            // 빈 벤치 슬롯 찾기
            int benchSlot = FindEmptyBenchSlot(world, ownerIndex);
            if (benchSlot < 0) return UnitData.InvalidId;

            int entityId = world.NextEntityId++;

            // 유닛 데이터 설정
            ref var unit = ref world.Units[unitIndex];
            unit.EntityId = entityId;
            unit.ChampionSpecId = championSpecId;
            unit.StarLevel = starLevel;
            unit.Location = UnitLocation.Bench;
            unit.BenchIndex = (byte)benchSlot;
            unit.OwnerIndex = ownerIndex;
            unit.ItemSlot0 = UnitData.InvalidId;
            unit.ItemSlot1 = UnitData.InvalidId;
            unit.ItemSlot2 = UnitData.InvalidId;

            // 챔피언 스펙에서 기본 스탯 복사 + 별 보정
            ApplySpecStats(world, ref unit);

            // 벤치에 등록
            world.BenchSlots[ownerIndex][benchSlot] = entityId;
            world.Boards[ownerIndex].BenchCount++;

            return entityId;
        }

        /// <summary>벤치 → 보드 배치. 레벨 제한 검사 포함.</summary>
        public static bool PlaceUnit(GameWorld world, byte playerIndex, int entityId, byte col, byte row)
        {
            if (!BoardHelper.IsValidBoardPosition(col, row)) return false;

            int unitIndex = world.FindUnitIndex(entityId);
            if (unitIndex < 0) return false;

            ref var unit = ref world.Units[unitIndex];
            if (unit.OwnerIndex != playerIndex) return false;
            if (unit.Location != UnitLocation.Bench) return false;

            // 레벨 제한: 보드 유닛 수 < 플레이어 레벨
            int boardCount = GetBoardUnitCount(world, playerIndex);
            int boardIndex = BoardHelper.ToIndex(col, row);
            int existingEntityId = world.BoardSlots[playerIndex][boardIndex];

            // 목표 위치에 유닛이 있으면 교환
            if (existingEntityId != UnitData.InvalidId)
            {
                return SwapBenchToBoard(world, playerIndex, unitIndex, existingEntityId, col, row);
            }

            // 보드가 꽉 찼는지 체크
            byte level = world.Economies[playerIndex].Level;
            if (boardCount >= level) return false;

            // 벤치에서 제거
            RemoveFromBench(world, playerIndex, ref unit);

            // 보드에 배치
            unit.Location = UnitLocation.Board;
            unit.BoardCol = col;
            unit.BoardRow = row;
            world.BoardSlots[playerIndex][boardIndex] = entityId;
            world.Boards[playerIndex].UnitCount++;

            return true;
        }

        /// <summary>보드 내 위치 이동. 목표에 유닛 있으면 위치 교환.</summary>
        public static bool MoveUnit(GameWorld world, byte playerIndex, int entityId, byte toCol, byte toRow)
        {
            if (!BoardHelper.IsValidBoardPosition(toCol, toRow)) return false;

            int unitIndex = world.FindUnitIndex(entityId);
            if (unitIndex < 0) return false;

            ref var unit = ref world.Units[unitIndex];
            if (unit.OwnerIndex != playerIndex) return false;
            if (unit.Location != UnitLocation.Board) return false;

            int fromIndex = BoardHelper.ToIndex(unit.BoardCol, unit.BoardRow);
            int toIndex = BoardHelper.ToIndex(toCol, toRow);

            if (fromIndex == toIndex) return true; // 같은 위치

            int targetEntityId = world.BoardSlots[playerIndex][toIndex];

            if (targetEntityId != UnitData.InvalidId)
            {
                // 목표 위치의 유닛과 위치 교환
                int targetUnitIndex = world.FindUnitIndex(targetEntityId);
                if (targetUnitIndex < 0) return false;

                ref var targetUnit = ref world.Units[targetUnitIndex];
                targetUnit.BoardCol = unit.BoardCol;
                targetUnit.BoardRow = unit.BoardRow;
                world.BoardSlots[playerIndex][fromIndex] = targetEntityId;
            }
            else
            {
                world.BoardSlots[playerIndex][fromIndex] = UnitData.InvalidId;
            }

            unit.BoardCol = toCol;
            unit.BoardRow = toRow;
            world.BoardSlots[playerIndex][toIndex] = entityId;

            return true;
        }

        /// <summary>보드 → 벤치 회수.</summary>
        public static bool WithdrawUnit(GameWorld world, byte playerIndex, int entityId)
        {
            int unitIndex = world.FindUnitIndex(entityId);
            if (unitIndex < 0) return false;

            ref var unit = ref world.Units[unitIndex];
            if (unit.OwnerIndex != playerIndex) return false;
            if (unit.Location != UnitLocation.Board) return false;

            int benchSlot = FindEmptyBenchSlot(world, playerIndex);
            if (benchSlot < 0) return false; // 벤치 가득 참

            // 보드에서 제거
            int boardIndex = BoardHelper.ToIndex(unit.BoardCol, unit.BoardRow);
            world.BoardSlots[playerIndex][boardIndex] = UnitData.InvalidId;
            world.Boards[playerIndex].UnitCount--;

            // 벤치에 추가
            unit.Location = UnitLocation.Bench;
            unit.BenchIndex = (byte)benchSlot;
            unit.BoardCol = 0;
            unit.BoardRow = 0;
            world.BenchSlots[playerIndex][benchSlot] = entityId;
            world.Boards[playerIndex].BenchCount++;

            return true;
        }

        /// <summary>두 유닛 위치 교환 (보드↔보드, 보드↔벤치, 벤치↔벤치 모두 지원).</summary>
        public static bool SwapUnits(GameWorld world, byte playerIndex, int entityA, int entityB)
        {
            int indexA = world.FindUnitIndex(entityA);
            int indexB = world.FindUnitIndex(entityB);
            if (indexA < 0 || indexB < 0) return false;

            ref var unitA = ref world.Units[indexA];
            ref var unitB = ref world.Units[indexB];
            if (unitA.OwnerIndex != playerIndex || unitB.OwnerIndex != playerIndex) return false;

            // 위치 정보 교환
            var tempLocation = unitA.Location;
            var tempCol = unitA.BoardCol;
            var tempRow = unitA.BoardRow;
            var tempBench = unitA.BenchIndex;

            SetUnitPosition(world, playerIndex, ref unitA, unitB.Location, unitB.BoardCol, unitB.BoardRow, unitB.BenchIndex, entityA);
            SetUnitPosition(world, playerIndex, ref unitB, tempLocation, tempCol, tempRow, tempBench, entityB);

            return true;
        }

        /// <summary>유닛 판매. 유닛 제거 + 골드 환급.</summary>
        public static bool SellUnit(GameWorld world, byte playerIndex, int entityId)
        {
            int unitIndex = world.FindUnitIndex(entityId);
            if (unitIndex < 0) return false;

            ref var unit = ref world.Units[unitIndex];
            if (unit.OwnerIndex != playerIndex) return false;

            // TODO: 판매 가격 계산 (챔피언 코스트 × 별 등급)
            int sellPrice = unit.StarLevel; // 임시

            // 위치에서 제거
            RemoveUnitFromPosition(world, playerIndex, ref unit);

            // 유닛 데이터 초기화
            world.Units[unitIndex] = UnitData.CreateEmpty();

            // 골드 지급
            world.Economies[playerIndex].Gold += sellPrice;

            return true;
        }

        /// <summary>유닛 제거 (보드/벤치에서 제거 + 유닛 데이터 초기화)</summary>
        public static bool RemoveUnit(GameWorld world, byte playerIndex, int entityId)
        {
            int unitIndex = world.FindUnitIndex(entityId);
            if (unitIndex < 0) return false;

            ref var unit = ref world.Units[unitIndex];
            if (unit.OwnerIndex != playerIndex) return false;

            RemoveUnitFromPosition(world, playerIndex, ref unit);
            world.Units[unitIndex] = UnitData.CreateEmpty();
            return true;
        }

        // ── 스펙 기반 스탯 계산 ──

        /// <summary>
        /// ChampionSpec에서 기본 스탯을 복사하고 별 보정을 적용.
        /// 유닛 생성 및 합성 후 호출.
        /// </summary>
        public static void ApplySpecStats(GameWorld world, ref UnitData unit)
        {
            if (world.Pool == null) return;

            // 스펙 조회
            int specIndex = -1;
            for (int i = 0; i < world.Pool.SpecCount; i++)
            {
                if (world.Pool.Specs[i].ChampionId == unit.ChampionSpecId)
                {
                    specIndex = i;
                    break;
                }
            }
            if (specIndex < 0) return;

            ref var spec = ref world.Pool.Specs[specIndex];

            // 기본 스탯 복사
            unit.MaxHP = spec.BaseHP;
            unit.Attack = spec.BaseAttack;
            unit.Armor = spec.BaseArmor;
            unit.MagicResist = spec.BaseMagicResist;
            unit.AttackSpeed = spec.AttackSpeed;
            unit.AttackRange = spec.AttackRange;
            unit.MoveSpeed = spec.MoveSpeed;
            unit.MaxMana = spec.MaxMana;
            unit.TraitFlags = spec.TraitFlags;

            // 별 보정 (HP, Attack에 배율 적용)
            if (unit.StarLevel >= 2 && spec.Star2Multiplier > 0)
            {
                int mult = unit.StarLevel == 2 ? spec.Star2Multiplier : spec.Star3Multiplier;
                if (mult <= 0) mult = unit.StarLevel == 2 ? 180 : 320; // 기본값
                unit.MaxHP = unit.MaxHP * mult / 100;
                unit.Attack = unit.Attack * mult / 100;
            }
        }

        // ── 조회 ──

        /// <summary>보드 위 유닛 수</summary>
        public static int GetBoardUnitCount(GameWorld world, byte playerIndex)
        {
            return world.Boards[playerIndex].UnitCount;
        }

        /// <summary>빈 벤치 슬롯 인덱스 (-1이면 가득 참)</summary>
        public static int FindEmptyBenchSlot(GameWorld world, byte playerIndex)
        {
            var bench = world.BenchSlots[playerIndex];
            for (int i = 0; i < PlayerBoard.BenchSize; i++)
            {
                if (bench[i] == UnitData.InvalidId)
                    return i;
            }
            return -1;
        }

        // ── 내부 헬퍼 ──

        private static void RemoveFromBench(GameWorld world, byte playerIndex, ref UnitData unit)
        {
            world.BenchSlots[playerIndex][unit.BenchIndex] = UnitData.InvalidId;
            world.Boards[playerIndex].BenchCount--;
        }

        private static void RemoveUnitFromPosition(GameWorld world, byte playerIndex, ref UnitData unit)
        {
            if (unit.Location == UnitLocation.Board)
            {
                int boardIndex = BoardHelper.ToIndex(unit.BoardCol, unit.BoardRow);
                world.BoardSlots[playerIndex][boardIndex] = UnitData.InvalidId;
                world.Boards[playerIndex].UnitCount--;
            }
            else if (unit.Location == UnitLocation.Bench)
            {
                world.BenchSlots[playerIndex][unit.BenchIndex] = UnitData.InvalidId;
                world.Boards[playerIndex].BenchCount--;
            }
            unit.Location = UnitLocation.None;
        }

        private static void SetUnitPosition(GameWorld world, byte playerIndex, ref UnitData unit,
            UnitLocation location, byte col, byte row, byte benchIndex, int entityId)
        {
            // 이전 위치에서 제거
            RemoveUnitFromPosition(world, playerIndex, ref unit);

            // 새 위치에 배치
            unit.Location = location;
            if (location == UnitLocation.Board)
            {
                unit.BoardCol = col;
                unit.BoardRow = row;
                int boardIdx = BoardHelper.ToIndex(col, row);
                world.BoardSlots[playerIndex][boardIdx] = entityId;
                world.Boards[playerIndex].UnitCount++;
            }
            else if (location == UnitLocation.Bench)
            {
                unit.BenchIndex = benchIndex;
                world.BenchSlots[playerIndex][benchIndex] = entityId;
                world.Boards[playerIndex].BenchCount++;
            }
        }

        private static bool SwapBenchToBoard(GameWorld world, byte playerIndex,
            int benchUnitIndex, int boardEntityId, byte col, byte row)
        {
            int boardUnitIndex = world.FindUnitIndex(boardEntityId);
            if (boardUnitIndex < 0) return false;

            ref var benchUnit = ref world.Units[benchUnitIndex];
            ref var boardUnit = ref world.Units[boardUnitIndex];

            byte oldBenchSlot = benchUnit.BenchIndex;

            // 보드 유닛 → 벤치로
            int boardSlotIndex = BoardHelper.ToIndex(col, row);
            world.BoardSlots[playerIndex][boardSlotIndex] = UnitData.InvalidId;
            world.Boards[playerIndex].UnitCount--;

            boardUnit.Location = UnitLocation.Bench;
            boardUnit.BenchIndex = oldBenchSlot;
            world.BenchSlots[playerIndex][oldBenchSlot] = boardEntityId;
            // BenchCount는 변하지 않음 (하나 나가고 하나 들어옴)

            // 벤치 유닛 → 보드로
            world.BenchSlots[playerIndex][oldBenchSlot] = boardEntityId; // 이미 설정됨
            benchUnit.Location = UnitLocation.Board;
            benchUnit.BoardCol = col;
            benchUnit.BoardRow = row;
            world.BoardSlots[playerIndex][boardSlotIndex] = benchUnit.EntityId;
            world.Boards[playerIndex].UnitCount++;

            return true;
        }
    }
}
