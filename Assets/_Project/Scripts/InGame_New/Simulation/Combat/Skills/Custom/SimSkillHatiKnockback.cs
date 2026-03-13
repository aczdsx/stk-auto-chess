namespace CookApps.AutoChess
{
    /// <summary>
    /// 하티 (217433303): 가장 먼 적 단일 타격 + 넉백 2타일.
    /// 채널링 스킬 — Execute 즉시 → SkillHitFrames[0] 타이밍에 효과 적용.
    /// - Execute: vfx[0] 캐스터, vfx[1] 타겟 (차징 이펙트)
    /// - SkillHitFrames[0]: vfx[2] 캐스터→타겟 방향 rotation, 데미지, 넉백
    /// </summary>
    public class SimSkillHatiKnockback : SimSkillBase
    {
        private int _knockbackDistance;
        private int _worldTickRate;

        private int _cachedTargetId;
        private int _phaseTimer;
        private bool _fired;

        public override bool IsChanneling => true;
        public override int GetCastFrames() => 0;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _knockbackDistance = p.CCDurationFrames > 0 ? p.CCDurationFrames : 2;
            _worldTickRate = p.WorldTickRate;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindTarget(state, ref caster, TargetType);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            _cachedTargetId = targetCombatId;
            _fired = false;
            _phaseTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0]
                : 15;

            // vfx[0]: 캐스터 차징 이펙트
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0);

            // vfx[1]: 타겟 이펙트
            if (targetCombatId > 0)
                state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 1,
                    targetId: targetCombatId);
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (_fired) return false;

            _phaseTimer--;
            if (_phaseTimer > 0) return true;

            _fired = true;
            ApplyHit(state, ref caster);
            return false;
        }

        private void ApplyHit(CombatMatchState state, ref CombatUnit caster)
        {
            int idx = state.FindUnitIndex(_cachedTargetId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];

            // caster → target 방향
            int dirCol = target.GridCol - caster.GridCol;
            int dirRow = target.GridRow - caster.GridRow;
            if (System.Math.Abs(dirCol) >= System.Math.Abs(dirRow))
                dirRow = 0;
            else
                dirCol = 0;
            dirCol = dirCol > 0 ? 1 : dirCol < 0 ? -1 : 0;
            dirRow = dirRow > 0 ? 1 : dirRow < 0 ? -1 : 0;
            if (dirCol == 0 && dirRow == 0)
                dirCol = caster.TeamIndex == 0 ? 1 : -1;

            // vfx[2]: 공격 이펙트 (캐스터→타겟 방향 rotation)
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 2,
                dirCol: (sbyte)dirCol, dirRow: (sbyte)dirRow);

            // 데미지
            SkillDamageHelper.DealDamage(state, ref caster, _cachedTargetId, PowerPercent, DamageType);

            // 사망 체크
            idx = state.FindUnitIndex(_cachedTargetId);
            if (idx < 0 || !state.Units[idx].IsAlive) return;
            target = ref state.Units[idx];

            // 넉백 2타일
            SkillCCHelper.Knockback(state, ref target, dirCol, dirRow, _knockbackDistance, _worldTickRate);
        }

        public override void Reset()
        {
            _knockbackDistance = 2;
            _fired = false;
            _phaseTimer = 0;
            _cachedTargetId = 0;
        }
    }
}
