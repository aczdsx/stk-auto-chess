namespace CookApps.AutoChess
{
    /// <summary>아군 버프 스킬 (자신에게 스탯 증가)</summary>
    public class SimSkillBuff : SimSkillBase
    {
        private StatModType _statType;
        private int _buffValue;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _statType = (StatModType)p.Param0;
            _buffValue = p.Param1 > 0 ? p.Param1 : PowerPercent;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return caster.CombatId;
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];

            SkillBuffHelper.ModifyStat(ref target, _statType, _buffValue);
        }

        public override void Reset()
        {
            _statType = default;
            _buffValue = 0;
        }
    }
}
