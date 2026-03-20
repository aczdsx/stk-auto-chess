using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 슈퍼노바 시너지 준비 페이즈 행동.
    /// 보드 위 빈 타일에 오브젝트를 생성하고, 유저가 오브젝트를 유닛에 드래그하면
    /// 해당 유닛에 시너지 버프를 부여. 스탯 적용은 ApplyEffects() PrepTarget 파이프라인.
    /// </summary>
    public class SynergyPrepSupernova : SynergyPrepBehaviorBase
    {
        /// <summary>오브젝트 보드 위치 (-1이면 미배치)</summary>
        public sbyte ObjectCol = -1;
        public sbyte ObjectRow = -1;

        /// <summary>티어별 유닛 뷰 스케일 보너스</summary>
        public float ViewScaleBonus => Tier switch
        {
            1 => 0.3f,
            2 => 0.4f,
            3 => 0.5f,
            _ => 0.3f,
        };

        public override void OnActivate(GameWorld world)
        {
            // 빈 타일 랜덤 선택하여 오브젝트 배치
            PlaceObjectOnEmptyTile(world);
            PrepParam0 = Tier; // View VFX 갱신용

            if (ObjectCol >= 0)
            {
                world.EventQueue.PushSupernovaObjectEvent(
                    PlayerIndex, TraitId, SupernovaSubType.Spawn, (byte)ObjectCol, (byte)ObjectRow);
            }
        }

        public override void OnDeactivate(GameWorld world)
        {
            if (ObjectCol >= 0)
            {
                world.EventQueue.PushSupernovaObjectEvent(
                    PlayerIndex, TraitId, SupernovaSubType.Remove, (byte)ObjectCol, (byte)ObjectRow);
            }

            if (PrepTargetEntityId >= 0)
            {
                world.EventQueue.PushSupernovaObjectEvent(
                    PlayerIndex, TraitId, SupernovaSubType.TargetRemoved,
                    0, 0, PrepTargetEntityId);
            }

            ObjectCol = -1;
            ObjectRow = -1;
            PrepTargetEntityId = -1;
            PrepParam0 = 0;
        }

        public override void OnTierChanged(GameWorld world, byte oldTier, byte newTier)
        {
            PrepParam0 = newTier;

            if (ObjectCol >= 0)
            {
                world.EventQueue.PushSupernovaObjectEvent(
                    PlayerIndex, TraitId, SupernovaSubType.TierChanged,
                    (byte)ObjectCol, (byte)ObjectRow);
            }
            else if (PrepTargetEntityId >= 0)
            {
                world.EventQueue.PushSupernovaObjectEvent(
                    PlayerIndex, TraitId, SupernovaSubType.TierChanged,
                    0, 0, PrepTargetEntityId);
            }
        }

        public override void OnBoardChanged(GameWorld world)
        {
            // PrepTarget 유닛이 보드에서 사라졌는지 확인
            if (PrepTargetEntityId >= 0)
            {
                if (!IsEntityOnBoard(world, PrepTargetEntityId))
                {
                    int removedEntityId = PrepTargetEntityId;
                    PrepTargetEntityId = -1;

                    // 타겟 VFX 제거
                    world.EventQueue.PushSupernovaObjectEvent(
                        PlayerIndex, TraitId, SupernovaSubType.TargetRemoved,
                        0, 0, removedEntityId);

                    // 오브젝트 재배치
                    PlaceObjectOnEmptyTile(world);
                    if (ObjectCol >= 0)
                    {
                        world.EventQueue.PushSupernovaObjectEvent(
                            PlayerIndex, TraitId, SupernovaSubType.Spawn, (byte)ObjectCol, (byte)ObjectRow);
                    }
                }
            }

            // 오브젝트 위치에 유닛이 배치되었는지 확인
            if (ObjectCol >= 0 && PrepTargetEntityId == -1)
            {
                int slot = ObjectRow * world.BoardWidth + ObjectCol;
                if (slot >= 0 && slot < world.BoardSize && world.BoardSlots[PlayerIndex][slot] != UnitData.InvalidId)
                {
                    int occupantId = world.BoardSlots[PlayerIndex][slot];
                    int traitBit = 1 << TraitId;
                    int unitIdx = world.FindUnitIndex(occupantId);

                    if (unitIdx >= 0 && (world.Units[unitIdx].TraitFlags & traitBit) != 0)
                    {
                        // trait 보유 유닛 → 자동 부여
                        world.EventQueue.PushSupernovaObjectEvent(
                            PlayerIndex, TraitId, SupernovaSubType.Remove,
                            (byte)ObjectCol, (byte)ObjectRow);
                        ObjectCol = -1;
                        ObjectRow = -1;

                        PrepTargetEntityId = occupantId;

                        world.EventQueue.PushSupernovaObjectEvent(
                            PlayerIndex, TraitId, SupernovaSubType.TargetAssigned,
                            0, 0, occupantId);
                        world.EventQueue.PushSynergyUpdated(PlayerIndex);
                    }
                    else
                    {
                        // trait 없는 유닛 → 토스트 + 다른 빈 타일로 이동
                        world.EventQueue.PushSupernovaObjectEvent(
                            PlayerIndex, TraitId, SupernovaSubType.InvalidDrop, 0, 0);

                        byte oldCol = (byte)ObjectCol;
                        byte oldRow = (byte)ObjectRow;
                        ObjectCol = -1;
                        ObjectRow = -1;

                        world.EventQueue.PushSupernovaObjectEvent(
                            PlayerIndex, TraitId, SupernovaSubType.Remove, oldCol, oldRow);

                        PlaceObjectOnEmptyTile(world);
                        if (ObjectCol >= 0)
                        {
                            world.EventQueue.PushSupernovaObjectEvent(
                                PlayerIndex, TraitId, SupernovaSubType.Spawn, (byte)ObjectCol, (byte)ObjectRow);
                        }
                    }
                }
            }
        }

        public override void HandleCommand(GameWorld world, in GameCommand cmd)
        {
            // cmd.Param0 = traitId (이미 검증됨)
            // cmd.Param1 >= 0: 유닛에 부여 (entityId = Param1)
            // cmd.Param1 == -1: 오브젝트 위치 이동 (Param2=col, Param3=row)

            if (cmd.Param1 >= 0)
            {
                // 유닛에 부여
                int targetEntityId = cmd.Param1;

                // 유효성 검증: 보드 위에 있고, SUPERNOVA TraitFlag를 가진 유닛인지
                int unitIdx = world.FindUnitIndex(targetEntityId);
                if (unitIdx < 0) return;

                ref var unit = ref world.Units[unitIdx];
                if (!unit.IsValid) return;

                int traitBit = 1 << TraitId;
                if ((unit.TraitFlags & traitBit) == 0) return;
                if (!IsEntityOnBoard(world, targetEntityId)) return;

                // 기존 오브젝트 제거
                if (ObjectCol >= 0)
                {
                    world.EventQueue.PushSupernovaObjectEvent(
                        PlayerIndex, TraitId, SupernovaSubType.Remove, (byte)ObjectCol, (byte)ObjectRow);
                    ObjectCol = -1;
                    ObjectRow = -1;
                }

                PrepTargetEntityId = targetEntityId;

                world.EventQueue.PushSupernovaObjectEvent(
                    PlayerIndex, TraitId, SupernovaSubType.TargetAssigned,
                    0, 0, targetEntityId);
                world.EventQueue.PushSynergyUpdated(PlayerIndex);
            }
            else if (cmd.Param1 == -1)
            {
                // 오브젝트 위치 이동
                byte newCol = (byte)cmd.Param2;
                byte newRow = (byte)cmd.Param3;

                // 유효성 검증: 보드 범위 내, 빈 타일
                if (newCol >= world.BoardWidth || newRow >= world.BoardHeight) return;
                int slot = newRow * world.BoardWidth + newCol;
                if (world.BoardSlots[PlayerIndex][slot] != UnitData.InvalidId) return;

                ObjectCol = (sbyte)newCol;
                ObjectRow = (sbyte)newRow;

                world.EventQueue.PushSupernovaObjectEvent(
                    PlayerIndex, TraitId, SupernovaSubType.Move, newCol, newRow);
            }
        }

        // ── 직렬화 ──

        public override PrepBehaviorSnapshot CaptureSnapshot()
        {
            var snap = base.CaptureSnapshot();
            snap.ObjectCol = ObjectCol;
            snap.ObjectRow = ObjectRow;
            return snap;
        }

        public override void RestoreFromSnapshot(PrepBehaviorSnapshot snapshot)
        {
            base.RestoreFromSnapshot(snapshot);
            ObjectCol = snapshot.ObjectCol;
            ObjectRow = snapshot.ObjectRow;
        }

        // ── 헬퍼 ──

        private void PlaceObjectOnEmptyTile(GameWorld world)
        {
            // 이미 유닛에 부여된 상태면 오브젝트 불필요
            if (PrepTargetEntityId >= 0) return;

            var boardSlots = world.BoardSlots[PlayerIndex];

            // 빈 타일 후보 수집
            var candidates = new int[world.BoardSize];
            int candidateCount = 0;

            for (int slot = 0; slot < world.BoardSize; slot++)
            {
                if (boardSlots[slot] != UnitData.InvalidId) continue;

                // 다른 PrepBehavior의 오브젝트 위치와 겹치지 않도록
                int col = slot % world.BoardWidth;
                int row = slot / world.BoardWidth;
                if (!IsSlotOccupiedByOtherObject(world, col, row))
                    candidates[candidateCount++] = slot;
            }

            if (candidateCount > 0)
            {
                int pick = world.RNG.Range(0, candidateCount);
                int pickedSlot = candidates[pick];
                ObjectCol = (sbyte)(pickedSlot % world.BoardWidth);
                ObjectRow = (sbyte)(pickedSlot / world.BoardWidth);
            }
        }

        private bool IsSlotOccupiedByOtherObject(GameWorld world, int col, int row)
        {
            for (int i = 0; i < world.PrepBehaviorCounts[PlayerIndex]; i++)
            {
                var other = world.PrepBehaviors[PlayerIndex][i];
                if (other == this) continue;
                if (other is SynergyPrepSupernova sn && sn.ObjectCol == col && sn.ObjectRow == row)
                    return true;
            }
            return false;
        }

        private bool IsEntityOnBoard(GameWorld world, int entityId)
        {
            var boardSlots = world.BoardSlots[PlayerIndex];
            for (int slot = 0; slot < world.BoardSize; slot++)
            {
                if (boardSlots[slot] == entityId)
                    return true;
            }
            return false;
        }
    }
}
