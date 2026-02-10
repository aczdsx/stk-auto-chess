namespace CookApps.AutoChess
{
    /// <summary>적 디버프 스킬 (대상 스탯 감소)</summary>
    public class SimSkillDebuff : SimSkillBase
    {
        private StatModType _statType;
        private int _debuffValue;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _statType = (StatModType)p.Param0;
            _debuffValue = p.Param1 > 0 ? p.Param1 : PowerPercent;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];

            // 음수로 적용하여 스탯 감소
            SkillBuffHelper.ModifyStat(ref target, _statType, -_debuffValue);
        }

        public override void Reset()
        {
            _statType = default;
            _debuffValue = 0;
        }
    }
}
