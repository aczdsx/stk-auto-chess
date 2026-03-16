namespace CookApps.AutoChess
{
    /// <summary>
    /// 시라유키 (217663506): 체력 최저 적 3명에게 순차 텔레포트 + 참격.
    /// 스킬 시작 시 TargetImpossible({1}초간 자연 만료), 완료 후 회피율 버프.
    /// 스펙: {0}=쿨타임, {1}=지정불가시간(초), {2}=데미지배율(%) → PowerPercent,
    ///       {3}=회피버프시간(초), {4}=회피증가율(%)
    /// Params: Param0=untargetableDurationFrames, Param1=dodgeDurationFrames, Param2=dodgePercent
    /// </summary>
    public class SimSkillShirayukiAssassin : SimSkillBase
    {
        private const int MaxTargets = 3;

        private int _untargetableDurationFrames;
        private int _dodgeDurationFrames;
        private int _dodgePercent;

        private int _currentHit;
        private int _totalHits;
        private int _phaseTimer;
        private int _clipEndTimer;
        private bool _hitsComplete;

        private readonly int[] _targetIds = new int[MaxTargets];
        private int _targetCount;

        public override SkillExecutionType ExecutionType => SkillExecutionType.Channeling;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _untargetableDurationFrames = p.Param0;
            _dodgeDurationFrames = p.Param1;
            _dodgePercent = p.Param2;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindTarget(state, ref caster, TargetType);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            _currentHit = 0;
            _totalHits = HitCount > 0 ? HitCount : MaxTargets;
            _hitsComplete = false;
            _clipEndTimer = SkillClipFrames > 0 ? SkillClipFrames : 60;

            // 최저HP 적 수집
            byte enemyTeam = (byte)(1 - caster.TeamIndex);
            _targetCount = SkillAreaHelper.FindLowestHPAllies(state, enemyTeam, _totalHits, _targetIds);
            if (_targetCount <= 0) { _totalHits = 0; return; }

            // 지정불가 — {1}초간 자연 만료 (스킬 종료 시 제거하지 않음)
            int casterIdx = state.FindUnitIndex(caster.CombatId);
            if (casterIdx >= 0 && _untargetableDurationFrames > 0)
            {
                StatusEffectSystem.AddEffect(state, casterIdx,
                    StatusEffectType.TargetImpossible, 0, _untargetableDurationFrames);
            }

            // vfx[0] 트레일 — 캐스터 본인 (View에서 수명 관리)
            state.EventQueue?.PushSkillPhaseVfx(
                caster.CombatId, SkillId, 0, targetId: caster.CombatId);

            // 첫 히트 타이밍
            _phaseTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0] : 10;
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            _clipEndTimer--;

            // 히트 완료 → 애니메이션 끝까지 대기
            if (_hitsComplete)
                return _clipEndTimer > 0;

            _phaseTimer--;
            if (_phaseTimer > 0) return true;

            // 타겟 결정 (순차, 부족하면 마지막 반복)
            int tIdx = _currentHit < _targetCount ? _currentHit : _targetCount - 1;
            int targetId = _targetIds[tIdx];

            // 타겟 사망 → 새 최저HP 적 탐색
            int unitIdx = state.FindUnitIndex(targetId);
            if (unitIdx < 0 || !state.Units[unitIdx].IsAlive)
            {
                targetId = FindAnyLowestHpEnemy(state, (byte)(1 - caster.TeamIndex));
                if (targetId == CombatUnit.InvalidId)
                {
                    _hitsComplete = true;
                    ApplyDodgeBuff(state, ref caster);
                    return _clipEndTimer > 0;
                }
                _targetIds[tIdx] = targetId;
                unitIdx = state.FindUnitIndex(targetId);
            }

            // 텔레포트 → 히트
            TeleportBehindTarget(state, ref caster, ref state.Units[unitIdx]);
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 1, targetId: targetId);
            SkillDamageHelper.DealDamage(state, ref caster, targetId, PowerPercent, DamageType);

            _currentHit++;

            if (_currentHit >= _totalHits)
            {
                _hitsComplete = true;
                ApplyDodgeBuff(state, ref caster);
                return _clipEndTimer > 0;
            }

            // 다음 히트 타이밍
            SetNextHitTimer();
            return true;
        }

        // ── 헬퍼 ──

        private void ApplyDodgeBuff(CombatMatchState state, ref CombatUnit caster)
        {
            if (_dodgeDurationFrames <= 0 || _dodgePercent <= 0) return;
            int idx = state.FindUnitIndex(caster.CombatId);
            if (idx >= 0)
                SkillBuffHelper.ApplyTimedBuff(state, idx, StatModType.DodgeChance, _dodgePercent, _dodgeDurationFrames);
        }

        private void SetNextHitTimer()
        {
            if (SkillHitFrames != null && _currentHit < SkillHitFrames.Length)
            {
                int prev = SkillHitFrames[_currentHit - 1];
                int next = SkillHitFrames[_currentHit];
                _phaseTimer = next > prev ? next - prev : 5;
            }
            else
            {
                _phaseTimer = 5;
            }
        }

        private static int FindAnyLowestHpEnemy(CombatMatchState state, byte enemyTeam)
        {
            int bestId = CombatUnit.InvalidId;
            int bestHp = int.MaxValue;
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var u = ref state.Units[i];
                if (!u.IsAlive || u.TeamIndex != enemyTeam) continue;
                if (u.CurrentHP < bestHp) { bestHp = u.CurrentHP; bestId = u.CombatId; }
            }
            return bestId;
        }

        // ── 텔레포트 ──

        private static void TeleportBehindTarget(CombatMatchState state, ref CombatUnit caster, ref CombatUnit target)
        {
            int dirCol = target.GridCol > caster.GridCol ? 1 : target.GridCol < caster.GridCol ? -1 : 0;
            int dirRow = target.GridRow > caster.GridRow ? 1 : target.GridRow < caster.GridRow ? -1 : 0;

            // 타겟 뒤쪽 먼저 시도
            if (TryTeleport(state, ref caster, target.GridCol + dirCol, target.GridRow + dirRow))
                return;

            // 인접 빈 타일 탐색
            for (int d = 1; d <= 2; d++)
                for (int dc = -d; dc <= d; dc++)
                    for (int dr = -d; dr <= d; dr++)
                    {
                        if (dc == 0 && dr == 0) continue;
                        if (TryTeleport(state, ref caster, target.GridCol + dc, target.GridRow + dr))
                            return;
                    }
        }

        private static bool TryTeleport(CombatMatchState state, ref CombatUnit caster, int col, int row)
        {
            if (!BoardHelper.IsValidCombatPosition(col, row)) return false;
            if (state.GetUnitAtGrid(col, row) != CombatUnit.InvalidId) return false;

            state.ClearGrid(caster.GridCol, caster.GridRow);
            caster.GridCol = (byte)col;
            caster.GridRow = (byte)row;
            state.SetGrid(col, row, caster.CombatId);
            state.EventQueue?.PushUnitMoved(caster.CombatId, (byte)col, (byte)row);
            return true;
        }

        public override void Reset()
        {
            _untargetableDurationFrames = 0;
            _dodgeDurationFrames = 0;
            _dodgePercent = 0;
            _currentHit = 0;
            _totalHits = 0;
            _phaseTimer = 0;
            _clipEndTimer = 0;
            _hitsComplete = false;
            _targetCount = 0;
            for (int i = 0; i < _targetIds.Length; i++)
                _targetIds[i] = CombatUnit.InvalidId;
        }
    }
}
