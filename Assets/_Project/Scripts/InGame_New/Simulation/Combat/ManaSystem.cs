namespace CookApps.AutoChess
{
    /// <summary>
    /// 마나 리젠 시스템. 초당 1회 시간 기반 마나 충전.
    /// </summary>
    public static class ManaSystem
    {
        /// <summary>
        /// 매 프레임 호출. 초당 1회(frameCount % tickRate == 0) 살아있는 유닛에 시간 리젠 적용.
        /// </summary>
        public static void TickManaRegen(CombatMatchState state, int frameCount, int tickRate)
        {
            if (tickRate <= 0) return;
            if (frameCount % tickRate != 0) return;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;
                if (unit.MaxMana <= 0) continue;

                int baseRegen = unit.ManaRegenPerSec;
                if (baseRegen <= 0) continue;

                int finalRegen = baseRegen * (100 + unit.ManaRegenRateBonus) / 100;
                if (finalRegen < 0) finalRegen = 0;

                DamageSystem.ChargeMana(ref unit, finalRegen);
            }
        }
    }
}
