using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 유니 (215252102): 최저 HP 아군 3명 힐 + 디버프 제거.
    /// IsDelayedSingleApply — SkillHitFrames[0] 타이밍에 힐 적용.
    /// </summary>
    public class SimSkillYuniHeal : SimSkillBase
    {
        private readonly int[] _targetBuffer = new int[16];
        private int _debuffRemoveCount;

        public override SkillExecutionType ExecutionType => SkillExecutionType.DelayedApply;

        public override void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            base.Initialize(baseParams);
            // {0}=쿨타임, {1}=힐배율(%)→PowerPercent, {2}=디버프 제거 수
            PowerPercent = SkillSpecHelper.GetInt(specList, 1, 200f);
            TargetCount = 3;
            _debuffRemoveCount = SkillSpecHelper.GetInt(specList, 2, 2f);
            if (_debuffRemoveCount <= 0) _debuffRemoveCount = 1;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return SkillAreaHelper.FindLowestHPAlly(state, caster.TeamIndex);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng) { }

        protected override void ApplySkillEffect(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int healAmount = caster.Attack * PowerPercent / 100;
            int count = SkillAreaHelper.FindLowestHPAllies(
                state, caster.TeamIndex, TargetCount, _targetBuffer);

            // 본인 VFX (vfx[0])
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0);

            for (int i = 0; i < count; i++)
            {
                int idx = state.FindUnitIndex(_targetBuffer[i]);
                if (idx < 0) continue;

                SkillDamageHelper.Heal(state, ref state.Units[idx], healAmount);
                StatusEffectSystem.RemoveDebuffs(state, idx, _debuffRemoveCount);

                // 타겟별 VFX (vfx[0])
                state.EventQueue?.PushSkillPhaseVfx(
                    caster.CombatId, SkillId, 0, targetId: state.Units[idx].CombatId);
            }
        }

        public override void Reset()
        {
            base.Reset();
            _debuffRemoveCount = 1;
        }
    }
}
