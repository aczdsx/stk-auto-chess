namespace CookApps.AutoChess
{
    /// <summary>
    /// 유니 (215252102): 최저 HP 아군 3명 힐 + 디버프 제거.
    /// 채널링 스킬 — Execute 즉시 호출 → SkillHitFrames[0] 타이밍에 힐 적용.
    /// </summary>
    public class SimSkillYuniHeal : SimSkillBase
    {
        private readonly int[] _targetBuffer = new int[16];
        private int _debuffRemoveCount;

        // 채널링 상태
        private int _cachedCasterCombatId;
        private int _cachedHealAmount;
        private int _phaseTimer;
        private bool _fired;

        public override bool IsChanneling => true;

        // Execute 즉시 호출 → OnChannelTick에서 SkillHitFrames[0] 타이밍에 효과 적용
        public override int GetCastFrames() => 0;

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
            _cachedCasterCombatId = caster.CombatId;
            _cachedHealAmount = caster.Attack * PowerPercent / 100;
            _fired = false;
            _phaseTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0]
                : 15; // fallback 0.5초@30fps
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (_fired) return false;

            _phaseTimer--;
            if (_phaseTimer > 0) return true;

            // SkillHitFrames[0] 타이밍 도달 → 힐 + VFX 적용
            _fired = true;
            ApplyHeal(state, ref caster);
            return false;
        }

        private void ApplyHeal(CombatMatchState state, ref CombatUnit caster)
        {
            int count = SkillAreaHelper.FindLowestHPAllies(
                state, caster.TeamIndex, TargetCount, _targetBuffer);

            // 본인 VFX (vfx[0])
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0);

            for (int i = 0; i < count; i++)
            {
                int idx = state.FindUnitIndex(_targetBuffer[i]);
                if (idx < 0) continue;

                SkillDamageHelper.Heal(state, ref state.Units[idx], _cachedHealAmount);
                StatusEffectSystem.RemoveDebuffs(state, idx, _debuffRemoveCount);

                // 타겟별 VFX (vfx[0])
                state.EventQueue?.PushSkillPhaseVfx(
                    caster.CombatId, SkillId, 0, targetId: state.Units[idx].CombatId);
            }
        }

        public override void Reset()
        {
            _debuffRemoveCount = 1;
            _fired = false;
        }
    }
}
