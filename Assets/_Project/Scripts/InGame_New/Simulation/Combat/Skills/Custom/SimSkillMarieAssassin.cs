namespace CookApps.AutoChess
{
    /// <summary>마리에: 타겟에 순간이동 + 다단히트 + 조건부 CC</summary>
    public class SimSkillMarieAssassin : SimSkillBase
    {
        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            for (int i = 0; i < HitCount; i++)
            {
                int idx = state.FindUnitIndex(targetCombatId);
                if (idx < 0 || !state.Units[idx].IsAlive) break;
                SkillDamageHelper.DealDamage(state, ref caster, targetCombatId, PowerPercent, DamageType);
            }

            if (CCType != CrowdControlType.None && CCDurationFrames > 0)
            {
                int idx = state.FindUnitIndex(targetCombatId);
                if (idx >= 0 && state.Units[idx].IsAlive)
                {
                    SkillCCHelper.ApplyCC(ref state.Units[idx], CCType, CCDurationFrames);
                }
            }
        }
    }
}
