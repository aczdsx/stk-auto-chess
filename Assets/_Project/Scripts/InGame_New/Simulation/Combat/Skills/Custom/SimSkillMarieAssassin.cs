namespace CookApps.AutoChess
{
    /// <summary>
    /// 마리에 (217563405): 공격력 최고 적 뒤로 순간이동 + 다단히트.
    /// SkillHitFrames 각 타이밍에 히트 발생, 히트 수는 스펙 기반.
    /// 첫 히트에서 순간이동 + vfx[0], 이후 히트는 데미지만.
    /// 스펙: {0}=쿨타임, {1}=히트수, {2}=데미지배율(%) → PowerPercent,
    ///       {3}=디버프지속(초), {4}=디버프율(%)
    /// Params: Param0=debuffDurationFrames, Param1=debuffPercent
    /// </summary>
    public class SimSkillMarieAssassin : SimSkillBase
    {
        private int _debuffDurationFrames;
        private int _debuffPercent;

        private int _currentHit;
        private int _totalHits;
        private int _phaseTimer;
        private bool _teleported;
        private int _targetCombatId;

        public override SkillExecutionType ExecutionType => SkillExecutionType.Channeling;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _debuffDurationFrames = p.Param0;
            _debuffPercent = p.Param1;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindTarget(state, ref caster, TargetType);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            _targetCombatId = targetCombatId;
            _currentHit = 0;
            _totalHits = HitCount;
            _teleported = false;

            // 첫 히트 타이밍 대기 (SkillHitFrames[0])
            _phaseTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0]
                : 10;
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (_currentHit >= _totalHits) return false;

            _phaseTimer--;
            if (_phaseTimer > 0) return true;

            // 타겟 생존 확인
            int targetIdx = state.FindUnitIndex(_targetCombatId);
            if (targetIdx < 0 || !state.Units[targetIdx].IsAlive)
                return false;

            // 첫 히트: 순간이동
            if (!_teleported)
            {
                _teleported = true;
                TeleportBehindTarget(state, ref caster, ref state.Units[targetIdx]);

                // vfx[0] 타겟 위치에 스폰
                state.EventQueue?.PushSkillPhaseVfx(
                    caster.CombatId, SkillId, 0, targetId: _targetCombatId);
            }

            // 데미지
            SkillDamageHelper.DealDamage(state, ref caster, _targetCombatId, PowerPercent, DamageType);

            _currentHit++;

            if (_currentHit >= _totalHits)
            {
                // 마지막 히트 후 디버프 적용 (공격력/방어력 감소)
                if (_debuffDurationFrames > 0 && _debuffPercent > 0)
                {
                    targetIdx = state.FindUnitIndex(_targetCombatId);
                    if (targetIdx >= 0 && state.Units[targetIdx].IsAlive)
                    {
                        SkillBuffHelper.ApplyTimedDebuff(state, targetIdx,
                            StatModType.Attack, _debuffPercent, _debuffDurationFrames);
                        SkillBuffHelper.ApplyTimedDebuff(state, targetIdx,
                            StatModType.Def, _debuffPercent, _debuffDurationFrames);

                        // 전용 아이콘용 SkillMarker 동시 발행
                        StatusEffectSystem.AddEffect(state, targetIdx,
                            StatusEffectType.SkillMarker, (int)SkillMarkerType.MarieAracne, _debuffDurationFrames);
                    }
                }
                return false;
            }

            // 다음 히트 타이밍 설정
            if (SkillHitFrames != null && _currentHit < SkillHitFrames.Length)
            {
                int prevFrame = SkillHitFrames[_currentHit - 1];
                int nextFrame = SkillHitFrames[_currentHit];
                _phaseTimer = nextFrame > prevFrame ? nextFrame - prevFrame : 5;
            }
            else
            {
                // SkillHitFrames 부족 시 균등 분배
                _phaseTimer = 5;
            }

            return true;
        }

        private static void TeleportBehindTarget(CombatMatchState state, ref CombatUnit caster, ref CombatUnit target)
        {
            // 타겟 기준 캐스터 반대편 타일로 이동
            int dirCol = target.GridCol - caster.GridCol;
            int dirRow = target.GridRow - caster.GridRow;

            // 방향 정규화 (±1)
            if (dirCol != 0) dirCol = dirCol > 0 ? 1 : -1;
            if (dirRow != 0) dirRow = dirRow > 0 ? 1 : -1;

            // 타겟 뒤쪽 타일 (타겟 기준 캐스터 반대편)
            int behindCol = target.GridCol + dirCol;
            int behindRow = target.GridRow + dirRow;

            if (TryTeleport(state, ref caster, behindCol, behindRow))
                return;

            // 뒤쪽이 막혀있으면 타겟 인접 빈 타일 탐색
            for (int d = 1; d <= 2; d++)
            {
                for (int dc = -d; dc <= d; dc++)
                {
                    for (int dr = -d; dr <= d; dr++)
                    {
                        if (dc == 0 && dr == 0) continue;
                        if (TryTeleport(state, ref caster, target.GridCol + dc, target.GridRow + dr))
                            return;
                    }
                }
            }
        }

        private static bool TryTeleport(CombatMatchState state, ref CombatUnit caster, int col, int row)
        {
            if (!BoardHelper.IsValidCombatPosition(col, row)) return false;
            if (state.GetUnitAtGrid(col, row) != CombatUnit.InvalidId) return false;

            // 기존 위치 그리드 해제
            state.ClearGrid(caster.GridCol, caster.GridRow);
            // 새 위치로 이동
            caster.GridCol = (byte)col;
            caster.GridRow = (byte)row;
            state.SetGrid(col, row, caster.CombatId);
            // 이동 이벤트 (View에서 순간이동 처리)
            state.EventQueue?.PushUnitMoved(caster.CombatId, (byte)col, (byte)row);
            return true;
        }

        public override void Reset()
        {
            _debuffDurationFrames = 0;
            _debuffPercent = 0;
            _currentHit = 0;
            _totalHits = 0;
            _phaseTimer = 0;
            _teleported = false;
            _targetCombatId = CombatUnit.InvalidId;
        }
    }
}
