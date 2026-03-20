using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 단일 대상 투사체 스킬 — 가장 가까운 적에게 Homing 투사체 발사, 도착 시 데미지.
    /// 레거시 스킬 중 "투사체 → 히트" 패턴을 범용으로 포팅.
    /// 예: 230202004 (1챕터 일반 저격수)
    ///
    /// VFX 매핑:
    ///   skill_vfxs[0] = 히트 이펙트 (도착 시)
    ///   skill_vfxs[1] = 투사체 이펙트 (비행 중)
    /// </summary>
    public class SimSkillSingleProjectile : SimSkillBase
    {
        private const float DefaultTravelSec = 0.5f;

        private int _travelFrames;

        public override SkillExecutionType ExecutionType => SkillExecutionType.DelayedApply;
        public override bool HasProjectile => true;

        public override void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            base.Initialize(baseParams);
            // {0}=쿨타임, {1}=데미지배율(%)
            PowerPercent = SkillSpecHelper.GetInt(specList, 1, 200f);
            _travelFrames = SkillSpecHelper.SecondsToFrames(DefaultTravelSec, tickRate);
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            // 시전 애니메이션만 — 실제 효과는 ApplySkillEffect에서
        }

        protected override void ApplySkillEffect(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];
            if (!target.IsAlive) return;

            int raw = caster.Attack * PowerPercent / 100;
            bool isCrit = false; // ProjectileSystem 내부에서 크리티컬 판정

            // vfx[1]=투사체, vfx[0]=도착 히트
            ProjectileSystem.CreateHomingProjectile(
                state, caster.CombatId, targetCombatId,
                raw, isCrit, DamageType, _travelFrames,
                skillSpecId: SkillId, skillVfxIndex: 1, arrivalVfxIndex: 0);
        }

        public override void Reset()
        {
            base.Reset();
            _travelFrames = 0;
        }
    }
}
