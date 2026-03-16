namespace CookApps.AutoChess
{
    public static partial class StatusEffectSystem
    {
        /// <summary>유닛에 활성 Silence 디버프가 있는지 확인</summary>
        public static bool HasSilence(CombatMatchState state, int unitIndex)
        {
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type == StatusEffectType.Silence) return true;
            }
            return false;
        }

        /// <summary>유닛에 활성 Slow 디버프가 있는지 확인</summary>
        public static bool HasSlow(CombatMatchState state, int unitIndex)
        {
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type == StatusEffectType.Slow) return true;
            }
            return false;
        }

        /// <summary>유닛에 활성 Taunt 디버프가 있는지 확인. 있으면 도발자 CombatId 반환.</summary>
        public static bool HasTaunt(CombatMatchState state, int unitIndex, out int forcedTargetId)
        {
            forcedTargetId = CombatUnit.InvalidId;
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type == StatusEffectType.Taunt)
                {
                    forcedTargetId = effect.Value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>유닛에 적용된 HealReduction 최대값 반환 (0이면 없음)</summary>
        public static int GetHealReduction(CombatMatchState state, int unitIndex)
        {
            int max = 0;
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type != StatusEffectType.HealReduction) continue;
                if (effect.Value > max) max = effect.Value;
            }
            return max;
        }

        /// <summary>유닛의 활성 CC 즉시 해제</summary>
        public static void RemoveCC(CombatMatchState state, int unitIndex)
        {
            if (unitIndex < 0 || unitIndex >= state.UnitCount) return;
            ref var unit = ref state.Units[unitIndex];
            if (unit.ActiveCC == CrowdControlType.None) return;

            // CC VFX 제거 이벤트
            var vfxType = CCToVfxType(unit.ActiveCC);
            if (vfxType != CombatVfxType.None)
                state.EventQueue?.PushCCRemoved(unit.CombatId, vfxType);

            unit.ActiveCC = CrowdControlType.None;
            unit.CCRemainingFrames = 0;
            if (unit.State == CombatState.CrowdControlled)
                unit.State = CombatState.Idle;
        }

        /// <summary>유닛의 특정 타입 StatusEffect 모두 제거 (역산 포함)</summary>
        public static void RemoveEffectsByType(CombatMatchState state, int unitIndex, StatusEffectType type)
        {
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type != type) continue;
                OnEffectExpired(state, ref effect);
                effect.IsActive = false;
            }
        }

        /// <summary>유닛의 특정 markerId(Value)를 가진 SkillMarker 수 반환</summary>
        public static int CountMarkers(CombatMatchState state, int unitIndex, int markerId)
        {
            int count = 0;
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type == StatusEffectType.SkillMarker && effect.Value == markerId)
                    count++;
            }
            return count;
        }

        /// <summary>유닛의 특정 markerId를 가진 가장 오래된(남은 프레임 최소) SkillMarker 제거</summary>
        public static void RemoveOldestMarker(CombatMatchState state, int unitIndex, int markerId)
        {
            int oldestIdx = -1;
            int lowestRemaining = int.MaxValue;

            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type != StatusEffectType.SkillMarker) continue;
                if (effect.Value != markerId) continue;

                if (effect.RemainingFrames < lowestRemaining)
                {
                    lowestRemaining = effect.RemainingFrames;
                    oldestIdx = i;
                }
            }

            if (oldestIdx >= 0)
            {
                state.StatusEffects[oldestIdx].IsActive = false;
            }
        }

        /// <summary>해당 유닛에 특정 면역 상태효과가 활성화되어 있는지 확인</summary>
        public static bool HasImmunity(CombatMatchState state, int unitIndex, StatusEffectType immunityType)
        {
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type == immunityType) return true;
            }
            return false;
        }
    }
}
