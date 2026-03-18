namespace CookApps.AutoChess
{
    /// <summary>
    /// 마나 리젠 시스템. 매 프레임 누적 카운터로 균등 분배하여 스무스하게 충전.
    /// </summary>
    public static class ManaSystem
    {
        /// <summary>
        /// 매 프레임 호출. 누적 카운터 방식으로 1초간 ManaRegenPerSec만큼 균등 분배.
        /// 매 프레임 accum += finalRegen, accum >= tickRate이면 1씩 충전.
        /// </summary>
        public static void TickManaRegen(CombatMatchState state, int frameCount, int tickRate)
        {
            if (tickRate <= 0) return;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;
                if (unit.MaxMana <= 0) continue;

                int baseRegen = unit.ManaRegenPerSec;
                if (baseRegen <= 0) continue;

                int finalRegen = baseRegen * (100 + unit.ManaRegenRateBonus) / 100;
                if (finalRegen < 0) finalRegen = 0;

                unit.ManaRegenAccum += finalRegen;
                while (unit.ManaRegenAccum >= tickRate)
                {
                    unit.ManaRegenAccum -= tickRate;
                    DamageSystem.ChargeMana(ref unit, 1);
                }
            }
        }
    }
}
