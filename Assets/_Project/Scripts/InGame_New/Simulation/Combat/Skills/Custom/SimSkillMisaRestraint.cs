namespace CookApps.AutoChess
{
    /// <summary>
    /// 미사 (217323201): 공격력 최고 적을 관에 봉인 + 스턴.
    /// 채널링 패턴 — SkillHitFrames[0] 타이밍에 CC 적용 + 관 VFX.
    /// 데미지 없음 (PowerPercent = 0).
    /// VFX: [0]=관짝 떨어지는 연출 (타겟 위치), [1]=관짝 유지 연출 (타겟 위치)
    /// 스펙: {0}=쿨타임(초), {1}=봉인지속(초) → CCDurationFrames
    /// </summary>
    public class SimSkillMisaRestraint : SimSkillBase
    {
        private int _targetCombatId;
        private int _phaseTimer;
        private bool _applied;

        public override bool IsChanneling => true;
        public override int GetCastFrames() => 0;

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindTarget(state, ref caster, TargetType);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            _targetCombatId = targetCombatId;
            _applied = false;

            _phaseTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0]
                : 10;
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (_applied) return false;

            _phaseTimer--;
            if (_phaseTimer > 0) return true;

            _applied = true;

            int targetIdx = state.FindUnitIndex(_targetCombatId);
            if (targetIdx < 0 || !state.Units[targetIdx].IsAlive)
                return false;

            // 스턴(봉인) 적용
            SkillCCHelper.ApplyCC(state, ref state.Units[targetIdx], CCType, CCDurationFrames);

            // 전용 아이콘용 SkillMarker 동시 발행
            StatusEffectSystem.AddEffect(state, targetIdx,
                StatusEffectType.SkillMarker, (int)SkillMarkerType.MisaRestraint, CCDurationFrames);

            // vfx[0]: 관짝 떨어지는 연출 (타겟 위치)
            state.EventQueue?.PushSkillPhaseVfx(
                caster.CombatId, SkillId, 0, targetId: _targetCombatId);

            // vfx[1]: 관짝 유지 연출 (타겟 위치)
            state.EventQueue?.PushSkillPhaseVfx(
                caster.CombatId, SkillId, 1, targetId: _targetCombatId);

            return false;
        }

        public override void Reset()
        {
            _targetCombatId = CombatUnit.InvalidId;
            _phaseTimer = 0;
            _applied = false;
        }
    }
}