namespace CookApps.AutoChess
{
    /// <summary>버프/디버프 헬퍼</summary>
    public static class SkillBuffHelper
    {
        /// <summary>스탯 증감 (고정값 가산, 즉시 영구 적용)</summary>
        public static void ModifyStat(ref CombatUnit target, StatModType stat, int value)
        {
            if (!target.IsAlive) return;

            switch (stat)
            {
                case StatModType.Attack:
                    target.Attack += value;
                    if (target.Attack < 0) target.Attack = 0;
                    break;
                case StatModType.Armor:
                    target.Armor += value;
                    if (target.Armor < 0) target.Armor = 0;
                    break;
                case StatModType.MagicResist:
                    target.MagicResist += value;
                    if (target.MagicResist < 0) target.MagicResist = 0;
                    break;
                case StatModType.AttackSpeed:
                    target.AttackSpeed += value;
                    if (target.AttackSpeed < 1) target.AttackSpeed = 1;
                    break;
                case StatModType.ManaRegenRate:
                    target.ManaRegenRateBonus += value;
                    break;
                case StatModType.MaxMana:
                    target.MaxMana += value;
                    if (target.MaxMana < 1) target.MaxMana = 1;
                    if (target.CurrentMana > target.MaxMana)
                        target.CurrentMana = target.MaxMana;
                    break;
                case StatModType.DodgeChance:
                    target.DodgeChance += value;
                    if (target.DodgeChance < 0) target.DodgeChance = 0;
                    break;
            }
        }

        /// <summary>지속시간 있는 스탯 버프 (만료 시 자동 역산)</summary>
        public static void ApplyTimedBuff(CombatMatchState state, int unitIndex,
            StatModType stat, int value, int durationFrames)
        {
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.StatBuff,
                value, durationFrames, statType: stat);
        }

        /// <summary>지속시간 있는 스탯 디버프 (만료 시 자동 역산)</summary>
        public static void ApplyTimedDebuff(CombatMatchState state, int unitIndex,
            StatModType stat, int value, int durationFrames)
        {
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.StatDebuff,
                value, durationFrames, statType: stat);
        }

        /// <summary>쉴드 부여 (지속시간 기반, 스태킹)</summary>
        public static void AddShield(CombatMatchState state, int unitIndex,
            int amount, int durationFrames)
        {
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.Shield,
                amount, durationFrames);
        }

        /// <summary>DOT(지속 데미지) 부여</summary>
        public static void ApplyDOT(CombatMatchState state, int unitIndex,
            int damagePerTick, int durationFrames, int tickInterval, DamageType dmgType)
        {
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.DamageOverTime,
                damagePerTick, durationFrames, tickInterval, dmgType: dmgType);
        }

        /// <summary>HOT(지속 회복) 부여</summary>
        public static void ApplyHOT(CombatMatchState state, int unitIndex,
            int healPerTick, int durationFrames, int tickInterval)
        {
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.HealOverTime,
                healPerTick, durationFrames, tickInterval);
        }

        /// <summary>CC 면역 부여 (기존 CC 즉시 해제)</summary>
        public static void ApplyCCImmunity(CombatMatchState state, int unitIndex, int durationFrames)
        {
            StatusEffectSystem.RemoveCC(state, unitIndex);
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.CCImmunity, 0, durationFrames);
        }

        /// <summary>DOT 면역 부여 (기존 DOT 즉시 제거)</summary>
        public static void ApplyDOTImmunity(CombatMatchState state, int unitIndex, int durationFrames)
        {
            StatusEffectSystem.RemoveEffectsByType(state, unitIndex, StatusEffectType.DamageOverTime);
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.DOTImmunity, 0, durationFrames);
        }

        /// <summary>디버프(스탯감소) 면역 부여 (기존 StatDebuff 즉시 제거, 스탯 역산 포함)</summary>
        public static void ApplyDebuffImmunity(CombatMatchState state, int unitIndex, int durationFrames)
        {
            StatusEffectSystem.RemoveEffectsByType(state, unitIndex, StatusEffectType.StatDebuff);
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.DebuffImmunity, 0, durationFrames);
        }
    }
}
