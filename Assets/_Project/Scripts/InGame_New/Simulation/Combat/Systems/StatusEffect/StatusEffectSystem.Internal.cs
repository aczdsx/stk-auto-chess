namespace CookApps.AutoChess
{
    public static partial class StatusEffectSystem
    {
        /// <summary>해당 유닛의 쉴드 합산 재계산 → CombatUnit.ShieldAmount 갱신</summary>
        private static void RecalcShieldCache(CombatMatchState state, int unitIndex)
        {
            if (unitIndex < 0 || unitIndex >= state.UnitCount) return;

            int total = 0;
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.Type != StatusEffectType.Shield) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                total += effect.Value;
            }

            state.Units[unitIndex].ShieldAmount = total;
        }

        /// <summary>만료 시 처리 (스탯 역산 등)</summary>
        private static void OnEffectExpired(CombatMatchState state, ref StatusEffect effect)
        {
            if (effect.OwnerUnitIndex < 0 || effect.OwnerUnitIndex >= state.UnitCount) return;
            ref var unit = ref state.Units[effect.OwnerUnitIndex];
            if (!unit.IsAlive) return;

            switch (effect.Type)
            {
                case StatusEffectType.StatBuff:
                    SkillBuffHelper.ModifyStat(ref unit, effect.StatType, -effect.Value);
                    break;
                case StatusEffectType.StatDebuff:
                    SkillBuffHelper.ModifyStat(ref unit, effect.StatType, effect.Value);
                    break;
                case StatusEffectType.Slow:
                    SkillBuffHelper.ModifyStat(ref unit, StatModType.AttackSpeed, effect.Value);
                    break;
                case StatusEffectType.TargetImpossible:
                    unit.IsUntargetable = false;
                    break;
            }

            // VFX 제거 이벤트 발행
            var vfxType = ToVfxType(effect.Type, effect.StatType);
            if (vfxType != CombatVfxType.None)
                state.EventQueue?.PushStatusEffectRemoved(unit.CombatId, vfxType, effect.StatType);
        }

        /// <summary>주기적 효과 적용 (DOT/HOT)</summary>
        private static void ApplyPeriodicEffect(CombatMatchState state, ref StatusEffect effect)
        {
            if (effect.OwnerUnitIndex < 0 || effect.OwnerUnitIndex >= state.UnitCount) return;
            ref var unit = ref state.Units[effect.OwnerUnitIndex];
            if (!unit.IsAlive) return;

            switch (effect.Type)
            {
                case StatusEffectType.DamageOverTime:
                    int dmg = DamageSystem.CalculateDamage(effect.Value, effect.DmgType, ref unit);
                    DamageSystem.ApplyDamage(state, ref unit, dmg);
                    break;
                case StatusEffectType.HealOverTime:
                    SkillDamageHelper.Heal(state, ref unit, effect.Value);
                    break;
            }
        }

        /// <summary>StatusEffectType → CombatVfxType 변환 (StatBuff/StatDebuff는 카테고리만 반환)</summary>
        public static CombatVfxType ToVfxType(StatusEffectType type, StatModType statType = default)
        {
            switch (type)
            {
                case StatusEffectType.Shield: return CombatVfxType.Shield;
                case StatusEffectType.DamageOverTime: return CombatVfxType.ContinuousDamage;
                case StatusEffectType.HealOverTime: return CombatVfxType.ContinuousHeal;
                case StatusEffectType.CCImmunity: return CombatVfxType.CCImmunity;
                case StatusEffectType.DOTImmunity: return CombatVfxType.DOTImmunity;
                case StatusEffectType.DebuffImmunity: return CombatVfxType.DebuffImmunity;
                case StatusEffectType.StatBuff: return CombatVfxType.StatBuff;
                case StatusEffectType.StatDebuff: return CombatVfxType.StatDebuff;
                case StatusEffectType.HealReduction: return CombatVfxType.HealAmountDown;
                case StatusEffectType.Silence: return CombatVfxType.CC_Silence;
                case StatusEffectType.Slow: return CombatVfxType.CC_Slow;
                case StatusEffectType.Taunt: return CombatVfxType.CC_Taunt;
                case StatusEffectType.TargetImpossible: return CombatVfxType.CC_TargetImpossible;
                default: return CombatVfxType.None;
            }
        }

        /// <summary>CrowdControlType → CombatVfxType 변환</summary>
        public static CombatVfxType CCToVfxType(CrowdControlType ccType)
        {
            switch (ccType)
            {
                case CrowdControlType.Stun: return CombatVfxType.CC_Stun;
                case CrowdControlType.Silence: return CombatVfxType.CC_Silence;
                case CrowdControlType.Slow: return CombatVfxType.CC_Slow;
                case CrowdControlType.Freeze: return CombatVfxType.CC_Freeze;
                case CrowdControlType.Taunt: return CombatVfxType.CC_Taunt;
                case CrowdControlType.Airborne: return CombatVfxType.CC_Airborne;
                case CrowdControlType.Knockback: return CombatVfxType.CC_KnockBack;
                default: return CombatVfxType.None;
            }
        }
    }
}
