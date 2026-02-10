namespace CookApps.AutoChess
{
    /// <summary>아군 회복 스킬 (최저 HP 아군 대상)</summary>
    public class SimSkillHeal : SimSkillBase
    {
        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return SkillAreaHelper.FindLowestHPAlly(state, caster.TeamIndex);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];

            int healAmount = caster.Attack * PowerPercent / 100;
            SkillDamageHelper.Heal(ref target, healAmount);
        }
    }
}
