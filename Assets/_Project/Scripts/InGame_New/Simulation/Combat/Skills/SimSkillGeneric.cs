using System.Collections.Generic;

using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// Recipe 기반 범용 스킬 실행기.
    /// SkillRecipe의 Action 배열을 타이밍에 따라 디스패치.
    /// 조건부 체이닝(OnKnockbackWall, OnProjectileArrive), multi-hitframe, 바운스 추적 지원.
    /// </summary>
    public class SimSkillGeneric : SimSkillBase
    {
        private SkillRecipe _recipe;

        // ── 런타임 상태 (채널링용) ──
        private int _startDelay;
        private int _tickTimer;
        private int _tickInterval;
        private int _remainingTicks;
        private int _tickCount;
        private int _worldTickRate;

        // ── Spec에서 추출한 밸런스 수치 ──
        private int[] _paramValues;

        // ── 캐시 ──
        private int _cachedTargetId;

        // ── 체이닝 상태 ──
        private bool _knockbackHitWall;
        private int _projectileArrivalTimer;
        private int _currentPower;
        private int _bounceCount;
        private int _decayPercent;
        private readonly int[] _hitIds = new int[8];
        private int _hitIdCount;

        // ── 복수 HitFrame (오데트 2페이즈) ──
        private int _currentHitFrameIndex;
        private int _hitFrameTimer;
        private bool _hasMultiHitFrames;

        public override SkillExecutionType ExecutionType => _recipe.ExecutionType;
        public override bool HasProjectile => _recipe.HasProjectile;

        // ══════════════════════════════
        // 초기화
        // ══════════════════════════════

        public void SetRecipe(SkillRecipe recipe)
        {
            _recipe = recipe;
        }

        public override void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            base.Initialize(baseParams);
            _worldTickRate = tickRate;

            if (_recipe == null) return;

            // ParamSlots에 따라 specList에서 수치 추출
            if (_recipe.ParamSlots != null && _recipe.ParamSlots.Length > 0)
            {
                _paramValues = new int[_recipe.ParamSlots.Length];
                for (int i = 0; i < _recipe.ParamSlots.Length; i++)
                {
                    var slot = _recipe.ParamSlots[i];
                    _paramValues[i] = slot.ValueType == ParamValueType.Frames
                        ? SkillSpecHelper.GetFrames(specList, slot.SpecIndex, slot.Fallback, tickRate)
                        : SkillSpecHelper.GetInt(specList, slot.SpecIndex, slot.Fallback);
                }

                // 첫 번째 슬롯은 관례적으로 PowerPercent
                if (_paramValues.Length > 0 && _paramValues[0] > 0)
                    PowerPercent = _paramValues[0];
            }

        }

        // ══════════════════════════════
        // 타겟 선정
        // ══════════════════════════════

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            switch (_recipe.TargetRule)
            {
                case SkillTargetType.NearestEnemy:
                    return TargetingSystem.FindNearestEnemy(state, ref caster);
                case SkillTargetType.FarthestEnemy:
                    return TargetingSystem.FindTarget(state, ref caster, SkillTargetType.FarthestEnemy);
                case SkillTargetType.LowestHPAlly:
                    return SkillAreaHelper.FindLowestHPAlly(state, caster.TeamIndex);
                case SkillTargetType.HighestAttackEnemy:
                    return TargetingSystem.FindTarget(state, ref caster, SkillTargetType.HighestAttackEnemy);
                case SkillTargetType.LowestHPEnemy:
                    return TargetingSystem.FindTarget(state, ref caster, SkillTargetType.LowestHPEnemy);
                case SkillTargetType.BestAoETarget:
                    int radius = FindAoERadius();
                    return SkillAreaHelper.FindBestAoETarget(state, ref caster, radius);
                case SkillTargetType.Self:
                    return caster.CombatId;
                default:
                    return TargetingSystem.FindNearestEnemy(state, ref caster);
            }
        }

        // ══════════════════════════════
        // Execute
        // ══════════════════════════════

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            _cachedTargetId = targetCombatId;
            _currentPower = PowerPercent;
            _bounceCount = 0;
            _hitIdCount = 0;
            _knockbackHitWall = false;
            _projectileArrivalTimer = 0;

            // Recipe에서 DecayParamIndex가 있는 액션 찾아서 감쇠율 캐시
            _decayPercent = 0;
            if (_recipe.Actions != null)
            {
                for (int i = 0; i < _recipe.Actions.Length; i++)
                {
                    if (_recipe.Actions[i].DecayParamIndex >= 0)
                    {
                        _decayPercent = _paramValues != null && _recipe.Actions[i].DecayParamIndex < _paramValues.Length
                            ? _paramValues[_recipe.Actions[i].DecayParamIndex] : 0;
                        break;
                    }
                }
            }

            var ctx = MakeContext(state, ref caster, targetCombatId, ref rng);

            // OnCast 트리거 액션 실행
            DispatchActions(SkillTriggerType.OnCast, 0, ctx);

            // Retarget으로 변경된 타겟 동기화
            _cachedTargetId = ctx.TargetCombatId;

            // 채널링 초기화
            if (_recipe.ExecutionType == SkillExecutionType.Channeling)
                InitChanneling();
        }

        // ══════════════════════════════
        // DelayedApply 지원 (base의 OnChannelTick에서 자동 호출)
        // ══════════════════════════════

        protected override void ApplySkillEffect(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            var ctx = MakeContext(state, ref caster, _cachedTargetId, ref rng);
            DispatchActions(SkillTriggerType.AtHitFrame, 0, ctx);
            _cachedTargetId = ctx.TargetCombatId;
        }

        // ══════════════════════════════
        // Channeling
        // ══════════════════════════════

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            // DelayedApply는 base가 처리
            if (_recipe.ExecutionType == SkillExecutionType.DelayedApply)
                return base.OnChannelTick(state, ref caster, ref rng);

            var ctx = MakeContext(state, ref caster, _cachedTargetId, ref rng);

            // ── Multi-hitframe 처리 (오데트 2페이즈) ──
            if (_hasMultiHitFrames)
            {
                if (_hitFrameTimer > 0)
                {
                    _hitFrameTimer--;
                    if (_hitFrameTimer <= 0)
                    {
                        DispatchActionsForHitFrame(_currentHitFrameIndex, ctx);
                        _cachedTargetId = ctx.TargetCombatId;
                        _currentHitFrameIndex++;

                        if (SkillHitFrames != null && _currentHitFrameIndex < SkillHitFrames.Length)
                        {
                            _hitFrameTimer = SkillHitFrames[_currentHitFrameIndex]
                                           - SkillHitFrames[_currentHitFrameIndex - 1];
                        }
                        else
                        {
                            // 모든 hitframe 처리 완료
                            DispatchActions(SkillTriggerType.OnComplete, 0, ctx);
                            return false;
                        }
                    }
                }

                // 투사체 도착도 체크 (multi-hitframe 스킬이 투사체도 쓸 수 있음)
                if (_projectileArrivalTimer > 0)
                {
                    _projectileArrivalTimer--;
                    if (_projectileArrivalTimer <= 0)
                        HandleProjectileArrival(ctx);
                }

                return true;
            }

            // ── 시작 딜레이 ──
            if (_startDelay > 0)
            {
                _startDelay--;
                if (_startDelay == 0)
                {
                    DispatchActions(SkillTriggerType.AtHitFrame, 0, ctx);
                    _cachedTargetId = ctx.TargetCombatId;
                }
                return true;
            }

            // ── 투사체 도착 타이머 (라키유/미노/베인) ──
            if (_projectileArrivalTimer > 0)
            {
                _projectileArrivalTimer--;
                if (_projectileArrivalTimer <= 0)
                    HandleProjectileArrival(ctx);
            }

            // ── 틱 간격 ──
            if (_tickInterval > 0)
            {
                _tickTimer--;
                if (_tickTimer <= 0)
                {
                    _tickTimer = _tickInterval;
                    _tickCount++;

                    DispatchActions(SkillTriggerType.OnTick, _tickCount, ctx);
                    _cachedTargetId = ctx.TargetCombatId;
                    _remainingTicks--;
                }

                // 종료 체크
                if (_remainingTicks <= 0)
                {
                    DispatchActions(SkillTriggerType.OnComplete, 0, ctx);
                    return false;
                }
            }

            // 투사체 체인 루프 진행 중이면 계속 유지
            if (_projectileArrivalTimer > 0)
                return true;

            // 틱도 없고 투사체도 없으면 종료 (AtHitFrame만 있는 채널링)
            if (_tickInterval <= 0 && _projectileArrivalTimer <= 0 && !_hasMultiHitFrames)
                return false;

            return true;
        }

        // ══════════════════════════════
        // Reset
        // ══════════════════════════════

        public override void Reset()
        {
            base.Reset();
            _startDelay = 0;
            _tickTimer = 0;
            _tickInterval = 0;
            _remainingTicks = 0;
            _tickCount = 0;
            _cachedTargetId = CombatUnit.InvalidId;

            _knockbackHitWall = false;
            _projectileArrivalTimer = 0;
            _currentPower = 0;
            _bounceCount = 0;
            _decayPercent = 0;
            _hitIdCount = 0;
            _currentHitFrameIndex = 0;
            _hitFrameTimer = 0;
            _hasMultiHitFrames = false;
            for (int i = 0; i < _hitIds.Length; i++) _hitIds[i] = CombatUnit.InvalidId;
        }

        // ══════════════════════════════
        // 내부
        // ══════════════════════════════

        private void InitChanneling()
        {
            _startDelay = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0] : 0;
            _tickCount = 0;

            // Recipe에서 OnTick 액션을 찾아 틱 설정
            // RepeatCount/DynamicFromClip/RepeatIntervalMs가 있는 액션 우선 (미노: Retarget보다 SpawnProjectile 우선)
            if (_recipe.Actions != null)
            {
                int bestTickIdx = -1;
                for (int i = 0; i < _recipe.Actions.Length; i++)
                {
                    ref var action = ref _recipe.Actions[i];
                    if (action.Trigger != SkillTriggerType.OnTick) continue;
                    if (bestTickIdx < 0) bestTickIdx = i; // 첫 번째 백업
                    if (action.RepeatCount > 0 || action.DynamicFromClip || action.RepeatIntervalMs > 0)
                    {
                        bestTickIdx = i;
                        break;
                    }
                }

                if (bestTickIdx >= 0)
                {
                    ref var action = ref _recipe.Actions[bestTickIdx];
                    int interval;
                    if (action.RepeatIntervalMs > 0)
                        interval = (int)(action.RepeatIntervalMs * _worldTickRate / 1000f + 0.5f);
                    else if (action.RepeatIntervalFrames > 0)
                        interval = action.RepeatIntervalFrames;
                    else
                        interval = 15;

                    if (action.DynamicFromClip)
                    {
                        int channelFrames = SkillClipFrames - _startDelay;
                        _tickInterval = interval;
                        _remainingTicks = channelFrames > 0
                            ? channelFrames / _tickInterval : 1;
                    }
                    else
                    {
                        _tickInterval = interval;
                        _remainingTicks = action.RepeatCount > 0
                            ? action.RepeatCount : 1;
                    }
                }
            }

            _tickTimer = _tickInterval;

            // Multi-hitframe 감지 (오데트: AtHitFrame + hitFrameIndex > 0)
            _hasMultiHitFrames = false;
            if (_recipe.Actions != null)
            {
                for (int i = 0; i < _recipe.Actions.Length; i++)
                {
                    if (_recipe.Actions[i].Trigger == SkillTriggerType.AtHitFrame &&
                        _recipe.Actions[i].HitFrameIndex > 0)
                    {
                        _hasMultiHitFrames = true;
                        break;
                    }
                }
            }
            if (_hasMultiHitFrames && SkillHitFrames != null && SkillHitFrames.Length > 0)
            {
                _currentHitFrameIndex = 0;
                _hitFrameTimer = SkillHitFrames[0];
                _startDelay = 0; // multi-hitframe이 타이밍 관리 → _startDelay 이중 발동 방지
            }
        }

        /// <summary>투사체 도착 처리 — OnProjectileArrive 디스패치 + 바운스 루프</summary>
        private void HandleProjectileArrival(SkillExecuteContext ctx)
        {
            _bounceCount++;

            // 히트 추적
            if (_hitIdCount < _hitIds.Length)
                _hitIds[_hitIdCount++] = _cachedTargetId;

            // 감쇠 적용
            if (_decayPercent > 0)
                _currentPower = _currentPower * (100 - _decayPercent) / 100;

            DispatchActions(SkillTriggerType.OnProjectileArrive, _bounceCount, ctx);
            _cachedTargetId = ctx.TargetCombatId;

            // 바운스 루프 종료 체크 (Retarget 실패 또는 최대 바운스 도달)
            if (_projectileArrivalTimer <= 0 && _bounceCount >= TargetCount && TargetCount > 1)
            {
                DispatchActions(SkillTriggerType.OnComplete, 0, ctx);
            }
        }

        private void DispatchActions(SkillTriggerType trigger, int tickCount, SkillExecuteContext ctx)
        {
            if (_recipe.Actions == null) return;

            for (int i = 0; i < _recipe.Actions.Length; i++)
            {
                ref var action = ref _recipe.Actions[i];
                if (action.Trigger != trigger) continue;
                if (!CheckCondition(action.Condition, tickCount)) continue;

                // ── Knockback 특별 처리: 결과를 직접 저장 + OnKnockbackWall 디스패치 ──
                if (action.Effect == SkillEffectType.Knockback)
                {
                    int dist = action.KnockbackDistance > 0
                        ? action.KnockbackDistance
                        : ctx.GetParamValue(action.SecondaryParamIndex);
                    if (dist <= 0) dist = 2;

                    int targetIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                    if (targetIdx >= 0)
                    {
                        ref var target = ref ctx.State.Units[targetIdx];
                        ref var caster = ref ctx.GetCaster();
                        int dirCol = target.GridCol - caster.GridCol;
                        int dirRow = target.GridRow - caster.GridRow;
                        if (dirCol == 0 && dirRow == 0)
                            dirCol = ctx.CasterTeam == 0 ? 1 : -1;
                        else
                        {
                            if (System.Math.Abs(dirCol) >= System.Math.Abs(dirRow))
                                dirRow = 0;
                            else
                                dirCol = 0;
                            dirCol = dirCol > 0 ? 1 : dirCol < 0 ? -1 : 0;
                            dirRow = dirRow > 0 ? 1 : dirRow < 0 ? -1 : 0;
                        }

                        int actualMoved = SkillCCHelper.Knockback(ctx.State, ref target, dirCol, dirRow, dist, _worldTickRate);
                        _knockbackHitWall = actualMoved < dist;
                    }
                    else
                    {
                        _knockbackHitWall = false;
                    }

                    if (_knockbackHitWall)
                        DispatchActions(SkillTriggerType.OnKnockbackWall, tickCount, ctx);

                    continue; // ActionExecutor.Execute 건너뜀
                }

                // ── SpawnProjectile: 도착 타이머 시작 ──
                if (action.Effect == SkillEffectType.SpawnProjectile)
                {
                    ActionExecutor.Execute(ref action, ctx);
                    int travelFrames = action.RepeatIntervalFrames > 0 ? action.RepeatIntervalFrames : 30;
                    _projectileArrivalTimer = travelFrames + 3; // +3 = DamageDelayFrames
                    continue;
                }

                // ── Retarget: _cachedTargetId 갱신 + ctx 재구성 ──
                if (action.Effect == SkillEffectType.Retarget)
                {
                    // struct 복사 문제 우회: Retarget 결과를 직접 추출
                    var retargetCtx = ctx; // 복사본에서 실행
                    ActionExecutor.Execute(ref action, retargetCtx);
                    int newTarget = retargetCtx.TargetCombatId;

                    if (newTarget != CombatUnit.InvalidId)
                    {
                        _cachedTargetId = newTarget;
                        ctx.TargetCombatId = newTarget; // 이후 액션에 반영
                    }
                    else if (_bounceCount > 0)
                    {
                        // Retarget 실패 시 바운스 루프 종료
                        DispatchActions(SkillTriggerType.OnComplete, 0, ctx);
                        _projectileArrivalTimer = 0;
                    }
                    continue;
                }

                ActionExecutor.Execute(ref action, ctx);

                // 비투사체 Damage 실행 후 히트 추적 (시라유키 등 Retarget excludeHit 지원)
                if (action.Effect == SkillEffectType.Damage &&
                    action.TargetFilter == SkillTargetFilter.PrimaryTarget &&
                    _hitIdCount < _hitIds.Length)
                {
                    _hitIds[_hitIdCount++] = ctx.TargetCombatId;
                }
            }
        }

        /// <summary>hitFrameIndex로 필터링된 AtHitFrame 액션만 디스패치 (오데트 multi-phase)</summary>
        private void DispatchActionsForHitFrame(int hitFrameIndex, SkillExecuteContext ctx)
        {
            if (_recipe.Actions == null) return;

            for (int i = 0; i < _recipe.Actions.Length; i++)
            {
                ref var action = ref _recipe.Actions[i];
                if (action.Trigger != SkillTriggerType.AtHitFrame) continue;
                if (action.HitFrameIndex != hitFrameIndex) continue;
                if (!CheckCondition(action.Condition, 0)) continue;

                // Knockback/SpawnProjectile/Retarget 특별 처리도 여기서 동일하게 적용
                if (action.Effect == SkillEffectType.Knockback)
                {
                    int dist = action.KnockbackDistance > 0
                        ? action.KnockbackDistance
                        : ctx.GetParamValue(action.SecondaryParamIndex);
                    if (dist <= 0) dist = 2;
                    int targetIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                    if (targetIdx >= 0)
                    {
                        ref var target = ref ctx.State.Units[targetIdx];
                        ref var caster = ref ctx.GetCaster();
                        int dirCol = target.GridCol - caster.GridCol;
                        int dirRow = target.GridRow - caster.GridRow;
                        if (dirCol == 0 && dirRow == 0) dirCol = ctx.CasterTeam == 0 ? 1 : -1;
                        else
                        {
                            if (System.Math.Abs(dirCol) >= System.Math.Abs(dirRow)) dirRow = 0;
                            else dirCol = 0;
                            dirCol = dirCol > 0 ? 1 : dirCol < 0 ? -1 : 0;
                            dirRow = dirRow > 0 ? 1 : dirRow < 0 ? -1 : 0;
                        }
                        int actualMoved = SkillCCHelper.Knockback(ctx.State, ref target, dirCol, dirRow, dist, _worldTickRate);
                        _knockbackHitWall = actualMoved < dist;
                    }
                    else _knockbackHitWall = false;
                    if (_knockbackHitWall)
                        DispatchActions(SkillTriggerType.OnKnockbackWall, 0, ctx);
                    continue;
                }

                if (action.Effect == SkillEffectType.SpawnProjectile)
                {
                    ActionExecutor.Execute(ref action, ctx);
                    int travelFrames = action.RepeatIntervalFrames > 0 ? action.RepeatIntervalFrames : 30;
                    _projectileArrivalTimer = travelFrames + 3;
                    continue;
                }

                if (action.Effect == SkillEffectType.Retarget)
                {
                    var retargetCtx = ctx;
                    ActionExecutor.Execute(ref action, retargetCtx);
                    int newTarget = retargetCtx.TargetCombatId;
                    if (newTarget != CombatUnit.InvalidId)
                    {
                        _cachedTargetId = newTarget;
                        ctx.TargetCombatId = newTarget;
                    }
                    continue;
                }

                ActionExecutor.Execute(ref action, ctx);
            }
        }

        private static bool CheckCondition(SkillActionCondition condition, int tickCount)
        {
            switch (condition)
            {
                case SkillActionCondition.Always:
                    return true;
                case SkillActionCondition.EveryNth2:
                    return tickCount % 2 == 0;
                case SkillActionCondition.EveryNth3:
                    return tickCount % 3 == 0;
                case SkillActionCondition.LastHitOnly:
                    return false;
                default:
                    return true;
            }
        }

        private SkillExecuteContext MakeContext(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            return new SkillExecuteContext
            {
                State = state,
                CasterCombatId = caster.CombatId,
                TargetCombatId = targetCombatId,
                DamageType = DamageType,
                SkillSpecId = SkillId,
                CasterTeam = caster.TeamIndex,
                WorldTickRate = _worldTickRate,
                Rng = rng,
                ParamValues = _paramValues,
                BasePowerPercent = PowerPercent,
                TickCount = _tickCount,
                CurrentPower = _currentPower > 0 ? _currentPower : PowerPercent,
                BounceCount = _bounceCount,
                HitIds = _hitIds,
                HitIdCount = _hitIdCount,
            };
        }

        private int FindAoERadius()
        {
            if (_recipe.Actions == null) return 1;
            for (int i = 0; i < _recipe.Actions.Length; i++)
            {
                if (_recipe.Actions[i].AreaRange > 0)
                    return _recipe.Actions[i].AreaRange;
            }
            return 1;
        }
    }
}
