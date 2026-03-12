namespace CookApps.AutoChess
{
    /// <summary>
    /// 필리아 (215532401): 가장 먼 적 단일 강타.
    /// 채널링 스킬 — Execute 즉시 호출 → SkillHitFrames[0] 타이밍에 VFX + 데미지 적용.
    /// 원본 VFX 구성:
    ///   vfx[0]: 시전 즉시 타겟 위치에 생성
    ///   vfx[1]: Execute 타이밍에 시전자 위치에서 방향 회전 포함 생성 (발사 이펙트)
    ///   vfx[2]: Execute 타이밍에 타겟에 부착 (히트 이펙트)
    /// </summary>
    public class SimSkillPiliaStrike : SimSkillBase
    {
        private int _cachedTargetId;
        private int _phaseTimer;
        private bool _fired;

        public override bool IsChanneling => true;
        public override int GetCastFrames() => 0;

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

            // vfx[0]: 시전 즉시 타겟 그리드 위치에 생성 (rotation 없이)
            int tIdx = state.FindUnitIndex(targetCombatId);
            if (tIdx >= 0)
            {
                state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0,
                    col: state.Units[tIdx].GridCol, row: state.Units[tIdx].GridRow, useGridPos: true);
            }
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (_fired) return false;

            _phaseTimer--;
            if (_phaseTimer > 0) return true;

            _fired = true;

            // 시전자 → 타겟 방향 계산
            int targetIdx = state.FindUnitIndex(_cachedTargetId);
            sbyte dirCol = 0, dirRow = 0;
            if (targetIdx >= 0)
            {
                dirCol = (sbyte)(state.Units[targetIdx].GridCol - caster.GridCol);
                dirRow = (sbyte)(state.Units[targetIdx].GridRow - caster.GridRow);
                // 정규화 (-1, 0, 1)
                if (dirCol > 1) dirCol = 1; else if (dirCol < -1) dirCol = -1;
                if (dirRow > 1) dirRow = 1; else if (dirRow < -1) dirRow = -1;
            }

            // vfx[1]: 발사 이펙트 — 시전자 위치 + 방향 회전
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 1, dirCol: dirCol, dirRow: dirRow);

            // vfx[2]: 히트 이펙트 — 타겟 위치에 부착
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 2, targetId: _cachedTargetId);

            SkillDamageHelper.DealDamage(state, ref caster, _cachedTargetId, PowerPercent, DamageType);
            return false;
        }

        public override void Reset()
        {
            _cachedTargetId = -1;
            _fired = false;
        }
    }
}
