using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 라키유 (217353203): 베지어 투사체 → 착탄 → 3×3 범위 디버프 (데미지 없음).
    /// 스펙: {0}=쿨타임(초), {1}=디버프지속(초), {2}=회복감소(%), {3}=방어감소(%)
    /// Params: Param0=debuffDurationFrames, Param1=healReductionPercent, Param2=defReductionPercent
    /// vfx[0]=약병 베지어 투사체(캐스터→타겟), vfx[1]=착탄 이펙트(3×3 각 타일)
    /// </summary>
    public class SimSkillRakiyuDebuff : SimSkillBase
    {
        public override SkillExecutionType ExecutionType => SkillExecutionType.Channeling;

        private const float TravelTimeSec = 0.5f;
        private const int AreaRange = 1; // 체비셰프 거리 1 = 3×3

        private int _debuffDurationFrames;
        private int _healReductionPercent;
        private int _defReductionPercent;
        private int _travelFrames;

        private int _targetId;
        private int _targetCol;
        private int _targetRow;

        private int _phase; // 0=대기, 1=투사체 비행중, 2=완료
        private int _startDelay;
        private int _arrivalTimer;
        private int _clipEndTimer;

        public override void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            base.Initialize(baseParams);
            // {0}=쿨타임, {1}=디버프지속(초), {2}=회복감소(%), {3}=방어감소(%)
            PowerPercent = 0;
            _debuffDurationFrames = SkillSpecHelper.GetFrames(specList, 1, 3f, tickRate);
            _healReductionPercent = SkillSpecHelper.GetInt(specList, 2, 50f);
            _defReductionPercent = SkillSpecHelper.GetInt(specList, 3, 30f);
            _travelFrames = SkillSpecHelper.SecondsToFrames(TravelTimeSec, tickRate);
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            _targetId = targetCombatId;
            _phase = 0;
            _clipEndTimer = SkillClipFrames > 0 ? SkillClipFrames : 120;
            _startDelay = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0] : 1;

            // 타겟 위치 캐싱 (사망 대비)
            int targetIdx = state.FindUnitIndex(targetCombatId);
            if (targetIdx >= 0)
            {
                _targetCol = state.Units[targetIdx].GridCol;
                _targetRow = state.Units[targetIdx].GridRow;
            }
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            _clipEndTimer--;

            switch (_phase)
            {
                case 0: // SkillHitFrames[0] 대기 → 투사체 발사
                    _startDelay--;
                    if (_startDelay <= 0)
                    {
                        LaunchProjectile(state, ref caster);
                        _phase = 1;
                    }
                    return true;

                case 1: // 투사체 비행중 → 도착 시 디버프 적용
                    _arrivalTimer--;
                    if (_arrivalTimer <= 0)
                    {
                        OnProjectileArrival(state, ref caster);
                        _phase = 2;
                    }
                    return _clipEndTimer > 0;

                default: // 채널링 유지 (clipEnd까지)
                    return _clipEndTimer > 0;
            }
        }

        private void LaunchProjectile(CombatMatchState state, ref CombatUnit caster)
        {
            // 타겟 생존 시 현재 위치로 갱신
            int targetIdx = state.FindUnitIndex(_targetId);
            if (targetIdx >= 0 && state.Units[targetIdx].IsAlive)
            {
                _targetCol = state.Units[targetIdx].GridCol;
                _targetRow = state.Units[targetIdx].GridRow;
            }

            // 베지어 투사체 발사 (damage=0, 뷰에서 VFX 처리)
            ProjectileSystem.CreateHomingProjectile(
                state, caster.CombatId, _targetId,
                damage: 0, isCrit: false, DamageType, _travelFrames,
                skillSpecId: SkillId, skillVfxIndex: 0, useBezier: true, arrivalVfxIndex: 1);

            _arrivalTimer = _travelFrames;
        }

        private void OnProjectileArrival(CombatMatchState state, ref CombatUnit caster)
        {
            // 타겟 생존 시 현재 위치 기준, 사망 시 캐싱된 위치 기준
            int targetIdx = state.FindUnitIndex(_targetId);
            if (targetIdx >= 0 && state.Units[targetIdx].IsAlive)
            {
                _targetCol = state.Units[targetIdx].GridCol;
                _targetRow = state.Units[targetIdx].GridRow;
            }

            int col = _targetCol;
            int row = _targetRow;
            int debuffDur = _debuffDurationFrames;
            int defRedPct = _defReductionPercent;
            int healRedPct = _healReductionPercent;

            // 타일 이펙트 (3×3 box)
            state.EventQueue?.PushSkillAreaEffect(
                caster.SourceEntityId, (byte)col, (byte)row, AreaRange, isBox: true);

            // 3×3 범위 내 적에게 디버프 적용
            SkillAreaHelper.ForEachEnemyInRadius(state, caster.TeamIndex, col, row, AreaRange,
                (ref CombatUnit enemy, int i) =>
                {
                    int enemyIdx = state.FindUnitIndex(enemy.CombatId);
                    if (enemyIdx < 0) return;

                    // AdReduce 디버프
                    SkillBuffHelper.ApplyTimedDebuff(state, enemyIdx,
                        StatModType.AdReduce, defRedPct, debuffDur);

                    // ApReduce 디버프
                    SkillBuffHelper.ApplyTimedDebuff(state, enemyIdx,
                        StatModType.ApReduce, defRedPct, debuffDur);

                    // 회복 감소
                    if (healRedPct > 0)
                        StatusEffectSystem.AddEffect(state, enemyIdx,
                            StatusEffectType.HealReduction, healRedPct, debuffDur);
                });
        }

        public override void Reset()
        {
            _targetId = CombatUnit.InvalidId;
            _targetCol = 0;
            _targetRow = 0;
            _phase = 0;
            _startDelay = 0;
            _arrivalTimer = 0;
            _clipEndTimer = 0;
        }
    }
}
