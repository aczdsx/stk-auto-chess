namespace CookApps.AutoChess
{
    /// <summary>유니: 최저 HP 아군 3명 힐 + 디버프 제거</summary>
    public class SimSkillYuniHeal : SimSkillBase
    {
        private readonly int[] _targetBuffer = new int[16];
        private int _debuffRemoveCount;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _debuffRemoveCount = p.Param0 > 0 ? p.Param0 : 1;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return SkillAreaHelper.FindLowestHPAlly(state, caster.TeamIndex);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int count = SkillAreaHelper.FindLowestHPAllies(
                state, caster.TeamIndex, TargetCount, _targetBuffer);
            int healAmount = caster.Attack * PowerPercent / 100;

            for (int i = 0; i < count; i++)
            {
                int idx = state.FindUnitIndex(_targetBuffer[i]);
                if (idx < 0) continue;
                SkillDamageHelper.Heal(state, ref state.Units[idx], healAmount);
                StatusEffectSystem.RemoveDebuffs(state, idx, _debuffRemoveCount);
            }
        }

        public override void Reset()
        {
            _debuffRemoveCount = 1;
        }
    }
}
