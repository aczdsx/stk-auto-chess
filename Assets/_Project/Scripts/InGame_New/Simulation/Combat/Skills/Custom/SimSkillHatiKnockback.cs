using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 하티 (217433303): 가장 먼 적 단일 타격 + 넉백 2타일.
    /// IsDelayedSingleApply — Execute 즉시 차징 VFX → SkillHitFrames[0] 타이밍에 데미지+넉백.
    /// </summary>
    public class SimSkillHatiKnockback : SimSkillBase
    {
        private int _knockbackDistance;
        private int _worldTickRate;

        public override SkillExecutionType ExecutionType => SkillExecutionType.DelayedApply;

        public override void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            base.Initialize(baseParams);
            // {0}=쿨타임, {1}=데미지배율(%)→PowerPercent, {2}=넉백거리(타일)
            PowerPercent = SkillSpecHelper.GetInt(specList, 1, 200f);
            TargetType = SkillTargetType.FarthestEnemy;
            int knockbackDist = SkillSpecHelper.GetInt(specList, 2, 2f);
            _knockbackDistance = knockbackDist > 0 ? knockbackDist : 2;
            _worldTickRate = tickRate;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindTarget(state, ref caster, TargetType);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            // vfx[0]: 캐스터 차징 이펙트
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0);

            // vfx[1]: 타겟 이펙트
            if (targetCombatId > 0)
                state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 1,
                    targetId: targetCombatId);
        }

        protected override void ApplySkillEffect(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int idx = state.FindUnitIndex(targetCombatId);
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
            SkillDamageHelper.DealDamage(state, ref caster, targetCombatId, PowerPercent, DamageType);

            // 사망 체크
            idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0 || !state.Units[idx].IsAlive) return;
            target = ref state.Units[idx];

            // 넉백 2타일
            SkillCCHelper.Knockback(state, ref target, dirCol, dirRow, _knockbackDistance, _worldTickRate);
        }

        public override void Reset()
        {
            base.Reset();
            _knockbackDistance = 2;
        }
    }
}
