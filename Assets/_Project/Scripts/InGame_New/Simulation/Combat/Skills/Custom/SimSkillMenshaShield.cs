namespace CookApps.AutoChess
{
    /// <summary>
    /// 멘샤: 같은 행 아군에게 실드 부여.
    /// IsDelayedSingleApply — SkillHitFrames[0] 타이밍에 실드 적용.
    /// </summary>
    public class SimSkillMenshaShield : SimSkillBase
    {
        private int _shieldDurationFrames;

        protected override bool IsDelayedSingleApply => true;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _shieldDurationFrames = p.Param0 > 0 ? p.Param0 : 180;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return caster.CombatId;
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng) { }

        protected override void ApplySkillEffect(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int shieldAmount = caster.Attack * PowerPercent / 100;
            int row = caster.GridRow;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var u = ref state.Units[i];
                if (u.TeamIndex != caster.TeamIndex || !u.IsAlive) continue;
                if (u.GridRow != row) continue;
                SkillBuffHelper.AddShield(state, i, shieldAmount, _shieldDurationFrames);
            }
        }

        public override void Reset()
        {
            base.Reset();
            _shieldDurationFrames = 180;
        }
    }
}
