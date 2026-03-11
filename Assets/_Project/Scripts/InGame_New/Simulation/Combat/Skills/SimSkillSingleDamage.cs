namespace CookApps.AutoChess
{
    /// <summary>단일 타겟 배율 데미지 스킬</summary>
    public class SimSkillSingleDamage : SimSkillBase
    {
        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindTarget(state, ref caster, TargetType);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            SkillDamageHelper.DealDamage(state, ref caster, targetCombatId, PowerPercent, DamageType);
        }
    }
}
