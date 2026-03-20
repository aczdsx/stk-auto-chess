using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 0챕터 보스 탱커 (250108001): 전방 직선 순차 타격 + 넉백.
    /// 캐스터 전방 직선으로 타일을 하나씩 순차 타격하며,
    /// 각 타일의 적에게 데미지 + 1칸 넉백을 적용.
    /// </summary>
    public class SimSkillBossTankLine : SimSkillBase
    {
        private const int DefaultLineLength = 10;
        private const int KnockbackDistance = 1;

        private int _worldTickRate;
        private int _lineLength;

        // 채널링 상태
        private int _dirCol;
        private int _dirRow;
        private int _currentStep;      // 현재 처리 중인 타일 인덱스 (0-based)
        private int _tickTimer;         // 다음 타일까지 남은 프레임
        private int _tickInterval;      // 타일 간 간격 (프레임)
        private bool _started;          // SkillHitFrames[0] 대기 완료 여부
        private int _startDelay;
        private int _clipEndTimer;

        public override SkillExecutionType ExecutionType => SkillExecutionType.Channeling;

        public override void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            base.Initialize(baseParams);
            // {0}=쿨타임, {1}=데미지배율(%)
            PowerPercent = SkillSpecHelper.GetInt(specList, 1, 200f);
            _worldTickRate = tickRate;
            _lineLength = DefaultLineLength;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            // 타겟 방향 결정
            int targetIdx = state.FindUnitIndex(targetCombatId);
            if (targetIdx >= 0)
            {
                ref var target = ref state.Units[targetIdx];
                int dc = target.GridCol - caster.GridCol;
                int dr = target.GridRow - caster.GridRow;
                if (System.Math.Abs(dr) >= System.Math.Abs(dc))
                {
                    _dirRow = dr >= 0 ? 1 : -1;
                    _dirCol = 0;
                }
                else
                {
                    _dirCol = dc >= 0 ? 1 : -1;
                    _dirRow = 0;
                }
            }
            else
            {
                _dirRow = caster.TeamIndex == 0 ? 1 : -1;
                _dirCol = 0;
            }

            _startDelay = SkillHitFrames != null && SkillHitFrames.Length > 0 ? SkillHitFrames[0] : 15;

            // 타일 간 간격: 0.2초
            _tickInterval = SkillSpecHelper.SecondsToFrames(0.2f, _worldTickRate);
            if (_tickInterval < 1) _tickInterval = 1;

            _currentStep = 0;
            _tickTimer = 0;
            _started = false;
            _clipEndTimer = SkillClipFrames > 0
                ? SkillClipFrames
                : _startDelay + _tickInterval * _lineLength;
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            _clipEndTimer--;

            // SkillHitFrames[0] 대기
            if (!_started)
            {
                _startDelay--;
                if (_startDelay > 0) return true;
                _started = true;

                // 첫 타일 즉시 실행
                DoLineHit(state, ref caster, ref rng);
                _currentStep++;
                _tickTimer = _tickInterval;
                return true;
            }

            if (_currentStep < _lineLength)
            {
                _tickTimer--;
                if (_tickTimer <= 0)
                {
                    _tickTimer = _tickInterval;
                    DoLineHit(state, ref caster, ref rng);
                    _currentStep++;
                }
            }

            return _clipEndTimer > 0 && _currentStep < _lineLength;
        }

        private void DoLineHit(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            int dist = _currentStep + 1;
            int tCol = caster.GridCol + _dirCol * dist;
            int tRow = caster.GridRow + _dirRow * dist;

            if (!BoardHelper.IsValidCombatPosition(tCol, tRow)) return;

            // 타일 VFX
            state.EventQueue?.PushSkillAreaEffect(
                caster.CombatId, (byte)tCol, (byte)tRow, 0);

            // 스킬 VFX (vfx[0]): 해당 그리드 좌표에 생성
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0,
                dirCol: (sbyte)_dirCol, dirRow: (sbyte)_dirRow,
                col: (byte)tCol, row: (byte)tRow, useGridPos: true);

            // 카메라 쉐이크 (0.4초, magnitude 0.15)
            state.EventQueue?.PushCameraShake(400, 15);

            // 해당 타일에 적이 있으면 데미지 + 넉백
            int combatId = state.GetUnitAtGrid(tCol, tRow);
            if (combatId == CombatUnit.InvalidId) return;

            int idx = state.FindUnitIndex(combatId);
            if (idx < 0) return;

            ref var target = ref state.Units[idx];
            if (!target.IsAlive || target.TeamIndex == caster.TeamIndex) return;

            // 데미지
            SkillDamageHelper.DealDamage(state, ref caster, combatId, PowerPercent, DamageType);

            // 사망 체크 후 넉백
            idx = state.FindUnitIndex(combatId);
            if (idx < 0 || !state.Units[idx].IsAlive) return;
            target = ref state.Units[idx];

            SkillCCHelper.Knockback(state, ref target, _dirCol, _dirRow, KnockbackDistance, _worldTickRate);
        }

        public override void Reset()
        {
            base.Reset();
            _currentStep = 0;
            _tickTimer = 0;
            _tickInterval = 0;
            _dirCol = 0;
            _dirRow = 0;
            _started = false;
            _startDelay = 0;
            _clipEndTimer = 0;
        }
    }
}
