namespace CookApps.AutoChess
{
    /// <summary>데미지 계산 + 적용 헬퍼</summary>
    public static class SkillDamageHelper
    {
        /// <summary>단일 대상 데미지 (배율 기반)</summary>
        public static void DealDamage(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, int powerPercent, DamageType type)
        {
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];
            if (!target.IsAlive) return;

            int raw = caster.Attack * powerPercent / 100;
            int dmg = DamageSystem.CalculateDamage(raw, type, ref caster, ref target);
            DamageSystem.ApplyDamage(state, ref target, dmg);
            DamageSystem.ChargeMana(ref target, target.ManaGainOnHit);
        }

        /// <summary>고정 데미지</summary>
        public static void DealFlatDamage(CombatMatchState state, int targetCombatId,
            int flatDamage, DamageType type)
        {
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];
            if (!target.IsAlive) return;

            int dmg = DamageSystem.CalculateDamage(flatDamage, type, ref target);
            DamageSystem.ApplyDamage(state, ref target, dmg);
            DamageSystem.ChargeMana(ref target, target.ManaGainOnHit);
        }

        /// <summary>HP 회복 (HealReduction 상태효과 반영)</summary>
        public static void Heal(CombatMatchState state, ref CombatUnit target, int amount)
        {
            if (!target.IsAlive) return;

            // HealReduction 상태효과 적용
            int targetIdx = state.FindUnitIndex(target.CombatId);
            if (targetIdx >= 0)
            {
                int reduction = StatusEffectSystem.GetHealReduction(state, targetIdx);
                if (reduction > 0)
                    amount = amount * (100 - reduction) / 100;
            }
            if (amount <= 0) return;

            target.CurrentHP += amount;
            if (target.CurrentHP > target.MaxHP)
                target.CurrentHP = target.MaxHP;

            if (CombatLogger.Enabled) CombatLogger.LogHeal(target.CombatId, amount, target.CurrentHP, target.MaxHP);

            state.EventQueue?.PushUnitHealed(target.CombatId, amount);
        }
    }
}
