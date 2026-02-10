namespace CookApps.AutoChess
{
    /// <summary>스턴 + 데미지 스킬</summary>
    public class SimSkillStun : SimSkillBase
    {
        private int _stunFrames;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _stunFrames = p.Param0 > 0 ? p.Param0 : 30; // 기본 1초 (30fps)
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

            // 데미지 적용
            SkillDamageHelper.DealDamage(state, ref caster, targetCombatId, PowerPercent, DamageType);

            // 스턴 적용 (데미지로 사망하지 않은 경우)
            if (target.IsAlive)
            {
                SkillCCHelper.ApplyCC(ref target, CrowdControlType.Stun, _stunFrames);
            }
        }

        public override void Reset()
        {
            _stunFrames = 30;
        }
    }
}
