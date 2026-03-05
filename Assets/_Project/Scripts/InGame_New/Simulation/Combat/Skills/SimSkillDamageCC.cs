namespace CookApps.AutoChess
{
    /// <summary>단일 타겟 데미지 + CC 스킬</summary>
    public class SimSkillDamageCC : SimSkillBase
    {
        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            SkillDamageHelper.DealDamage(state, ref caster, targetCombatId, PowerPercent, DamageType);

            if (CCType == CrowdControlType.None || CCDurationFrames <= 0) return;

            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;

            if (CCType == CrowdControlType.Knockback)
            {
                int dirCol = caster.TeamIndex == 0 ? 1 : -1;
                SkillCCHelper.Knockback(state, ref state.Units[idx], dirCol, 0, CCDurationFrames);
            }
            else
            {
                SkillCCHelper.ApplyCC(ref state.Units[idx], CCType, CCDurationFrames);
            }
        }
    }
}