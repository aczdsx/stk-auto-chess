namespace CookApps.AutoChess
{
    /// <summary>
    /// 미사 (217323201): 공격력 최고 적을 관에 봉인 + 스턴.
    /// IsDelayedSingleApply — SkillHitFrames[0] 타이밍에 CC 적용 + 관 VFX.
    /// </summary>
    public class SimSkillMisaRestraint : SimSkillBase
    {
        protected override bool IsDelayedSingleApply => true;

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindTarget(state, ref caster, TargetType);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng) { }

        protected override void ApplySkillEffect(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int targetIdx = state.FindUnitIndex(targetCombatId);
            if (targetIdx < 0 || !state.Units[targetIdx].IsAlive)
                return;

            // 스턴(봉인) 적용
            SkillCCHelper.ApplyCC(state, ref state.Units[targetIdx], CCType, CCDurationFrames);

            // 전용 아이콘용 SkillMarker 동시 발행
            StatusEffectSystem.AddEffect(state, targetIdx,
                StatusEffectType.SkillMarker, (int)SkillMarkerType.MisaRestraint, CCDurationFrames);

            // vfx[0]: 관짝 떨어지는 연출 (타겟 위치)
            state.EventQueue?.PushSkillPhaseVfx(
                caster.CombatId, SkillId, 0, targetId: targetCombatId);

            // vfx[1]: 관짝 유지 연출 (타겟 위치)
            state.EventQueue?.PushSkillPhaseVfx(
                caster.CombatId, SkillId, 1, targetId: targetCombatId);
        }
    }
}
