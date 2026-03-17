using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 필리아 (215532401): 가장 먼 적 단일 강타.
    /// IsDelayedSingleApply — Execute 즉시 vfx[0] 타겟 위치 스폰 → SkillHitFrames[0]에 VFX + 데미지.
    /// </summary>
    public class SimSkillPiliaStrike : SimSkillBase
    {
        public override SkillExecutionType ExecutionType => SkillExecutionType.DelayedApply;

        public override void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            base.Initialize(baseParams);
            // {0}=쿨타임, {1}=데미지배율(%)→PowerPercent
            PowerPercent = SkillSpecHelper.GetInt(specList, 1, 200f);
            TargetType = SkillTargetType.FarthestEnemy;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindTarget(state, ref caster, TargetType);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            // vfx[0]: 시전 즉시 타겟 그리드 위치에 생성 (rotation 없이)
            int tIdx = state.FindUnitIndex(targetCombatId);
            if (tIdx >= 0)
            {
                state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0,
                    col: state.Units[tIdx].GridCol, row: state.Units[tIdx].GridRow, useGridPos: true);
            }
        }

        protected override void ApplySkillEffect(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            // 시전자 → 타겟 방향 계산
            int targetIdx = state.FindUnitIndex(targetCombatId);
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
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 2, targetId: targetCombatId);

            SkillDamageHelper.DealDamage(state, ref caster, targetCombatId, PowerPercent, DamageType);
        }
    }
}
