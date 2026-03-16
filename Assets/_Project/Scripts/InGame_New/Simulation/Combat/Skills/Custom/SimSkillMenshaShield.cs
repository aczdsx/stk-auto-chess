using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 멘샤: 같은 행 아군에게 실드 부여.
    /// IsDelayedSingleApply — SkillHitFrames[0] 타이밍에 실드 적용.
    /// </summary>
    public class SimSkillMenshaShield : SimSkillBase
    {
        private int _shieldDurationFrames;

        public override SkillExecutionType ExecutionType => SkillExecutionType.DelayedApply;

        public override void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            base.Initialize(baseParams);
            // {0}=쿨타임, {1}=실드지속(초), {2}=실드배율(%)→PowerPercent
            PowerPercent = SkillSpecHelper.GetInt(specList, 2, 200f);
            _shieldDurationFrames = SkillSpecHelper.GetFrames(specList, 1, 6f, tickRate);
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
            int col = caster.GridCol;

            // 같은 행 타일 이펙트 (fx_common_area_xxx) — 구 시스템 AddInGameTileFx 대응
            state.EventQueue?.PushSkillAreaEffect(
                caster.SourceEntityId, (byte)col, (byte)row, 7, isRow: true);

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var u = ref state.Units[i];
                if (u.TeamIndex != caster.TeamIndex || !u.IsAlive) continue;
                if (u.GridRow != row) continue;
                SkillBuffHelper.AddShield(state, i, shieldAmount, _shieldDurationFrames);

                // 타겟별 스킬 VFX (skill_vfxs[0]) — 구 시스템 AddInGameVfx 대응
                state.EventQueue?.PushSkillPhaseVfx(
                    caster.CombatId, SkillId, 0, targetId: u.CombatId);
            }
        }

        public override void Reset()
        {
            base.Reset();
            _shieldDurationFrames = 180;
        }
    }
}
