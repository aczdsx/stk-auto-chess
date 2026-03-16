namespace CookApps.AutoChess
{
    /// <summary>
    /// 아드리아 (217523403): 3단계 확장 패턴 AoE + 방어력 비례 데미지 + 스턴.
    /// Phase 0: +(범위1), Phase 1: X(범위1), Phase 2: +(범위2)
    /// 각 Phase는 SkillHitFrames 타이밍에 발동, 이미 맞은 적은 중복 히트 안 함.
    /// 스펙: {0}=쿨타임, {1}=데미지배율(%), {2}=방어력계수, {3}=스턴시간(초)
    /// Params: Param0=defScaleValue, Param1=stunDurationFrames
    /// </summary>
    public class SimSkillAdriaExpand : SimSkillBase
    {
        private int _defScaleValue;
        private int _stunDurationFrames;

        private int _currentPhase;
        private int _phaseTimer;
        private bool _done;

        // 이미 맞은 적 추적 (비트마스크, 최대 64유닛)
        private long _hitMask;

        private const int PhaseCount = 3;
        private const int FallbackDelay = 8; // SkillHitFrames 없을 때 페이즈 간격

        public override SkillExecutionType ExecutionType => SkillExecutionType.Channeling;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _defScaleValue = p.Param0 > 0 ? p.Param0 : 100;
            _stunDurationFrames = p.Param1 > 0 ? p.Param1 : 60;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return caster.CombatId; // 자기 중심
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            _currentPhase = 0;
            _done = false;
            _hitMask = 0;

            // 첫 Phase 타이밍 대기 (SkillHitFrames[0])
            _phaseTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0]
                : FallbackDelay;
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (_done) return false;

            _phaseTimer--;
            if (_phaseTimer > 0) return true;

            // 현재 Phase 실행
            DoPhase(state, ref caster, _currentPhase);

            _currentPhase++;
            if (_currentPhase >= PhaseCount)
            {
                _done = true;
                return false;
            }

            // 다음 Phase 타이밍 설정
            if (SkillHitFrames != null && _currentPhase < SkillHitFrames.Length)
            {
                // SkillHitFrames[n]은 절대 프레임 → 이전 프레임과의 차이가 대기 시간
                int prevFrame = SkillHitFrames[_currentPhase - 1];
                int nextFrame = SkillHitFrames[_currentPhase];
                _phaseTimer = nextFrame > prevFrame ? nextFrame - prevFrame : FallbackDelay;
            }
            else
            {
                _phaseTimer = FallbackDelay;
            }

            return true;
        }

        private void DoPhase(CombatMatchState state, ref CombatUnit caster, int phase)
        {
            int col = caster.GridCol;
            int row = caster.GridRow;
            int attack = caster.Attack;
            int def = caster.Def;
            var dmgType = DamageType;
            byte team = caster.TeamIndex;
            int power = PowerPercent;
            int defScale = _defScaleValue;
            int stunFrames = _stunDurationFrames;
            int casterIdx = state.FindUnitIndex(caster.CombatId);

            // Phase별 패턴으로 적 순회
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;
                if (unit.TeamIndex == team) continue;

                if (!IsInPattern(col, row, unit.GridCol, unit.GridRow, phase))
                    continue;

                // 중복 히트 방지
                int bitIndex = i % 64;
                long bit = 1L << bitIndex;
                if ((_hitMask & bit) != 0) continue;
                _hitMask |= bit;

                // 데미지: attack * damageRate% * (1 + def / defValue)
                int raw = attack * power / 100 * (defScale + def) / defScale;
                int dmg = DamageSystem.CalculateDamage(raw, dmgType, ref state.Units[casterIdx], ref unit);
                DamageSystem.ApplyDamage(state, ref unit, dmg);
                DamageSystem.ChargeMana(ref unit, unit.ManaGainOnHit);

                // 스턴
                if (stunFrames > 0)
                    SkillCCHelper.ApplyCC(state, ref unit, CrowdControlType.Stun, stunFrames);
            }

            // 패턴 내 모든 타일에 vfx[0] 스폰 (타일이펙트 없음)
            EmitPatternVfx(state, ref caster, col, row, phase);
        }

        /// <summary>패턴에 해당하는 모든 그리드 좌표에 vfx[0] 발행</summary>
        private void EmitPatternVfx(CombatMatchState state, ref CombatUnit caster, int cx, int cy, int phase)
        {
            int range = GetPhaseRadius(phase);
            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = -range; dy <= range; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (!IsInPattern(cx, cy, cx + dx, cy + dy, phase)) continue;

                    int tx = cx + dx;
                    int ty = cy + dy;
                    if (!BoardHelper.IsValidCombatPosition(tx, ty)) continue;

                    state.EventQueue?.PushSkillPhaseVfx(
                        caster.CombatId, SkillId, 0,
                        col: (byte)tx, row: (byte)ty, useGridPos: true);
                }
            }
        }

        /// <summary>Phase별 패턴 판정. Phase 0: +(1), Phase 1: X(1), Phase 2: +(2)</summary>
        private static bool IsInPattern(int cx, int cy, int tx, int ty, int phase)
        {
            int dx = tx - cx;
            int dy = ty - cy;
            int absDx = dx < 0 ? -dx : dx;
            int absDy = dy < 0 ? -dy : dy;

            switch (phase)
            {
                case 0: // + 패턴 (맨해튼 거리 1, 축 정렬만)
                    return (absDx + absDy <= 1) && (absDx + absDy > 0);

                case 1: // X 패턴 (대각선 거리 1)
                    return absDx == 1 && absDy == 1;

                case 2: // + 패턴 (맨해튼 거리 2, 축 정렬만 — 이미 히트된 적 제외는 hitMask에서)
                    return (absDx + absDy <= 2) && (absDx + absDy > 0)
                           && (absDx == 0 || absDy == 0);

                default:
                    return false;
            }
        }

        private static int GetPhaseRadius(int phase)
        {
            switch (phase)
            {
                case 0: return 1;
                case 1: return 1;
                case 2: return 2;
                default: return 1;
            }
        }

        public override void Reset()
        {
            _defScaleValue = 100;
            _stunDurationFrames = 60;
            _currentPhase = 0;
            _phaseTimer = 0;
            _done = false;
            _hitMask = 0;
        }
    }
}
