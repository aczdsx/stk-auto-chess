namespace CookApps.AutoChess
{
    /// <summary>단일 타겟 다단히트 데미지</summary>
    public class SimSkillMultiHit : SimSkillBase
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
        }
    }
}