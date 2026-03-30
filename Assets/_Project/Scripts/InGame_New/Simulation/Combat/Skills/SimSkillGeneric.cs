using System.Collections.Generic;

using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// Recipe 기반 범용 스킬 실행기 (static class).
    /// SkillRecipe의 Action 배열을 타이밍에 따라 디스패치.
    /// 조건부 체이닝(OnKnockbackWall, OnProjectileArrive), multi-hitframe, 바운스 추적 지원.
    /// </summary>
    public static class GenericSkillLogic
    {
        // HitIds scratch buffer (SkillExecuteContext에 전달용, GC-free)
        private static readonly int[] HitIdScratch = new int[SkillState.MaxHitIds];

        // ══════════════════════════════
        // 초기화
        // ══════════════════════════════

        public static void InitializeFromSpec(ref SkillConfig config, ref SkillState state, List<SkillActive> specList, int tickRate)
        {
            config.WorldTickRate = tickRate;

            if (config.Recipe == null) return;

            // Recipe에서 ExecutionType, HasProjectile 동기화
            config.ExecutionType = config.Recipe.ExecutionType;
            config.HasProjectile = config.Recipe.HasProjectile;

            // ParamSlots에 따라 specList에서 수치 추출
            if (config.Recipe.ParamSlots != null && config.Recipe.ParamSlots.Length > 0)
            {
                config.ParamValues = new int[config.Recipe.ParamSlots.Length];
                for (int i = 0; i < config.Recipe.ParamSlots.Length; i++)
                {
                    var slot = config.Recipe.ParamSlots[i];
                    if (slot.SpecIndex == 255)
                    {
                        // 고정값 (ValueRef.Fixed) — specData 무시
                        config.ParamValues[i] = slot.ValueType == ParamValueType.Frames
                            ? (int)(slot.Fallback * tickRate + 0.5f)
                            : UnityEngine.Mathf.RoundToInt(slot.Fallback);
                    }
                    else
                    {
                        config.ParamValues[i] = slot.ValueType == ParamValueType.Frames
                            ? SkillSpecHelper.GetFrames(specList, slot.SpecIndex, slot.Fallback, tickRate)
                            : SkillSpecHelper.GetInt(specList, slot.SpecIndex, slot.Fallback);
                    }
                }

                // 첫 번째 슬롯은 관례적으로 PowerPercent
                if (config.ParamValues.Length > 0 && config.ParamValues[0] > 0)
                    config.PowerPercent = config.ParamValues[0];
            }
        }

        // ══════════════════════════════
        // 타겟 선정
        // ══════════════════════════════

        public static int SelectTarget(ref SkillConfig config, CombatMatchState state, ref CombatUnit caster)
        {
            switch (config.Recipe.TargetRule)
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
                    int radius = FindAoERadius(ref config);
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

        public static void Execute(ref SkillConfig config, ref SkillState state, CombatMatchState matchState,
            ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
        {
            state.CachedTargetId = targetCombatId;
            state.SavedGridCol = caster.GridCol;
            state.SavedGridRow = caster.GridRow;
            state.CurrentPower = config.PowerPercent;
            state.BounceCount = 0;
            state.HitIdCount = 0;
            state.KnockbackHitWall = 0;
            state.ProjectileArrivalTimer = 0;
            state.CompleteFired = 0;
            state.PostCompleteTimer = 0;
            state.CurrentHitFrameIndex = 0;
            state.HitFrameTimer = 0;
            state.StartDelay = 0;
            state.TickTimer = 0;
            state.TickInterval = 0;
            state.RemainingTicks = 0;
            state.TickCount = 0;

            // Recipe에서 DecayParamIndex가 있는 액션 찾아서 감쇠율 캐시
            state.DecayPercent = 0;
            if (config.Recipe.Actions != null)
            {
                for (int i = 0; i < config.Recipe.Actions.Length; i++)
                {
                    if (config.Recipe.Actions[i].DecayParamIndex >= 0)
                    {
                        state.DecayPercent = config.ParamValues != null && config.Recipe.Actions[i].DecayParamIndex < config.ParamValues.Length
                            ? config.ParamValues[config.Recipe.Actions[i].DecayParamIndex] : 0;
                        break;
                    }
                }
            }

            var ctx = MakeContext(ref config, ref state, matchState, ref caster, targetCombatId, ref rng);

            // OnCast 트리거 액션 실행
            DispatchActions(ref config, ref state, SkillTriggerType.OnCast, 0, ctx);

            // Retarget으로 변경된 타겟 동기화
            state.CachedTargetId = ctx.TargetCombatId;

            // 채널링 초기화
            if (config.ExecutionType == SkillExecutionType.Channeling)
                InitChanneling(ref config, ref state);
        }

        // ══════════════════════════════
        // DelayedApply 지원
        // ══════════════════════════════

        public static void ApplySkillEffect(ref SkillConfig config, ref SkillState state, CombatMatchState matchState,
            ref CombatUnit caster, ref DeterministicRNG rng)
        {
            var ctx = MakeContext(ref config, ref state, matchState, ref caster, state.CachedTargetId, ref rng);
            DispatchActions(ref config, ref state, SkillTriggerType.AtHitFrame, 0, ctx);
            state.CachedTargetId = ctx.TargetCombatId;
        }

        // ══════════════════════════════
        // Channeling
        // ══════════════════════════════

        public static bool OnChannelTick(ref SkillConfig config, ref SkillState state, CombatMatchState matchState,
            ref CombatUnit caster, ref DeterministicRNG rng)
        {
            // DelayedApply는 SkillDispatcher가 처리
            if (config.ExecutionType == SkillExecutionType.DelayedApply)
                return SkillDispatcher.OnChannelTickDelayedApply(ref config, ref state, matchState, ref caster, ref rng);

            var ctx = MakeContext(ref config, ref state, matchState, ref caster, state.CachedTargetId, ref rng);

            // ── 대쉬 페이즈 (multi-hitframe보다 우선) ──
            if (caster.DashPhase != DashPhase.None)
            {
                if (!caster.IsMoving)
                {
                    bool stillMoving = DashSystem.OnMoveComplete(matchState, ref caster, ref state);
                    if (!stillMoving)
                    {
                        // 페이즈 완료 → 다음 hitframe 디스패치
                        int nextHitFrame = state.Custom.Dash.DashHitFrameIndex + 1;
                        DispatchActionsForHitFrame(ref config, ref state, nextHitFrame, ctx);
                    }
                }
                return DashSystem.IsActive(ref caster);
            }

            // ── Multi-hitframe 처리 (오데트 2페이즈) ──
            if (state.HasMultiHitFrames != 0)
            {
                if (state.HitFrameTimer > 0)
                {
                    state.HitFrameTimer--;
                    if (state.HitFrameTimer <= 0)
                    {
                        DispatchActionsForHitFrame(ref config, ref state, state.CurrentHitFrameIndex, ctx);
                        state.CachedTargetId = ctx.TargetCombatId;
                        state.CurrentHitFrameIndex++;

                        if (config.SkillHitFrames != null && state.CurrentHitFrameIndex < config.SkillHitFrames.Length)
                        {
                            state.HitFrameTimer = config.SkillHitFrames[state.CurrentHitFrameIndex]
                                                - config.SkillHitFrames[state.CurrentHitFrameIndex - 1];
                        }
                        else
                        {
                            // 모든 hitframe 처리 완료 → OnComplete + 클립 끝까지 대기
                            if (state.CompleteFired == 0)
                            {
                                state.CompleteFired = 1;
                                DispatchActions(ref config, ref state, SkillTriggerType.OnComplete, 0, ctx);
                                // 남은 클립 프레임 계산
                                int lastHitFrame = config.SkillHitFrames[state.CurrentHitFrameIndex - 1];
                                state.PostCompleteTimer = config.SkillClipFrames > lastHitFrame
                                    ? config.SkillClipFrames - lastHitFrame : 0;
                            }
                        }
                    }
                }

                // 투사체 도착도 체크 (multi-hitframe 스킬이 투사체도 쓸 수 있음)
                if (state.ProjectileArrivalTimer > 0)
                {
                    state.ProjectileArrivalTimer--;
                    if (state.ProjectileArrivalTimer <= 0)
                        HandleProjectileArrival(ref config, ref state, ctx);
                }

                // post-complete 대기 (클립 끝까지)
                if (state.CompleteFired != 0)
                {
                    state.PostCompleteTimer--;
                    return state.PostCompleteTimer > 0;
                }

                return true;
            }

            // ── 시작 딜레이 ──
            if (state.StartDelay > 0)
            {
                state.StartDelay--;
                if (state.StartDelay == 0)
                {
                    DispatchActions(ref config, ref state, SkillTriggerType.AtHitFrame, 0, ctx);
                    state.CachedTargetId = ctx.TargetCombatId;
                }
                return true;
            }

            // ── 투사체 도착 타이머 (라키유/미노/베인) ──
            if (state.ProjectileArrivalTimer > 0)
            {
                state.ProjectileArrivalTimer--;
                if (state.ProjectileArrivalTimer <= 0)
                    HandleProjectileArrival(ref config, ref state, ctx);
            }

            // ── 틱 간격 ──
            if (state.TickInterval > 0)
            {
                state.TickTimer--;
                if (state.TickTimer <= 0)
                {
                    state.TickTimer = state.TickInterval;
                    state.TickCount++;

                    DispatchActions(ref config, ref state, SkillTriggerType.OnTick, state.TickCount, ctx);
                    state.CachedTargetId = ctx.TargetCombatId;
                    state.RemainingTicks--;
                }

                // 종료 체크
                if (state.RemainingTicks <= 0)
                {
                    DispatchActions(ref config, ref state, SkillTriggerType.OnComplete, 0, ctx);
                    return false;
                }
            }

            // 투사체 체인 루프 진행 중이면 계속 유지
            if (state.ProjectileArrivalTimer > 0)
                return true;



            // 틱도 없고 투사체도 없으면 종료 (AtHitFrame만 있는 채널링)
            if (state.TickInterval <= 0 && state.ProjectileArrivalTimer <= 0 && state.HasMultiHitFrames == 0)
            {

                return false;
            }

            return true;
        }

        // ══════════════════════════════
        // 내부
        // ══════════════════════════════

        private static void InitChanneling(ref SkillConfig config, ref SkillState state)
        {
            state.StartDelay = config.SkillHitFrames != null && config.SkillHitFrames.Length > 0
                ? config.SkillHitFrames[0] : 0;
            state.TickCount = 0;

            // Recipe에서 OnTick 액션을 찾아 틱 설정
            if (config.Recipe.Actions != null)
            {
                int bestTickIdx = -1;
                for (int i = 0; i < config.Recipe.Actions.Length; i++)
                {
                    ref var action = ref config.Recipe.Actions[i];
                    if (action.Trigger != SkillTriggerType.OnTick) continue;
                    if (bestTickIdx < 0) bestTickIdx = i;
                    if (action.RepeatCount > 0 || action.DynamicFromClip || action.RepeatIntervalMs > 0)
                    {
                        bestTickIdx = i;
                        break;
                    }
                }

                if (bestTickIdx >= 0)
                {
                    ref var action = ref config.Recipe.Actions[bestTickIdx];
                    int interval;
                    if (action.RepeatIntervalMs > 0)
                        interval = (int)(action.RepeatIntervalMs * config.WorldTickRate / 1000f + 0.5f);
                    else if (action.RepeatIntervalFrames > 0)
                        interval = action.RepeatIntervalFrames;
                    else
                        interval = 15;

                    if (action.DynamicFromClip)
                    {
                        int channelFrames = config.SkillClipFrames - state.StartDelay;
                        state.TickInterval = interval;
                        state.RemainingTicks = channelFrames > 0
                            ? channelFrames / state.TickInterval : 1;
                    }
                    else
                    {
                        state.TickInterval = interval;
                        state.RemainingTicks = action.RepeatCount > 0
                            ? action.RepeatCount : 1;
                    }
                }
            }

            state.TickTimer = state.TickInterval;

            // Multi-hitframe 감지 (오데트: AtHitFrame + hitFrameIndex > 0)
            state.HasMultiHitFrames = 0;
            if (config.Recipe.Actions != null)
            {
                for (int i = 0; i < config.Recipe.Actions.Length; i++)
                {
                    if (config.Recipe.Actions[i].Trigger == SkillTriggerType.AtHitFrame &&
                        config.Recipe.Actions[i].HitFrameIndex > 0)
                    {
                        state.HasMultiHitFrames = 1;
                        break;
                    }
                }
            }
            if (state.HasMultiHitFrames != 0 && config.SkillHitFrames != null && config.SkillHitFrames.Length > 0)
            {
                state.CurrentHitFrameIndex = 0;
                state.HitFrameTimer = config.SkillHitFrames[0];
                state.StartDelay = 0;
            }
        }

        /// <summary>투사체 도착 처리 — OnProjectileArrive 디스패치 + 바운스 루프</summary>
        private static void HandleProjectileArrival(ref SkillConfig config, ref SkillState state, SkillExecuteContext ctx)
        {
            state.BounceCount++;

            // 히트 추적
            if (state.HitIdCount < SkillState.MaxHitIds)
                state.SetHitId(state.HitIdCount++, state.CachedTargetId);

            // 감쇠 적용
            if (state.DecayPercent > 0)
                state.CurrentPower = state.CurrentPower * (100 - state.DecayPercent) / 100;

            DispatchActions(ref config, ref state, SkillTriggerType.OnProjectileArrive, state.BounceCount, ctx);
            state.CachedTargetId = ctx.TargetCombatId;

            // 바운스 루프 종료 체크 (Retarget 실패 또는 최대 바운스 도달)
            if (state.ProjectileArrivalTimer <= 0 && state.BounceCount >= config.TargetCount && config.TargetCount > 1)
            {
                DispatchActions(ref config, ref state, SkillTriggerType.OnComplete, 0, ctx);
            }
        }

        private static void DispatchActions(ref SkillConfig config, ref SkillState state, SkillTriggerType trigger, int tickCount, SkillExecuteContext ctx)
        {
            if (config.Recipe.Actions == null) return;

            for (int i = 0; i < config.Recipe.Actions.Length; i++)
            {
                ref var action = ref config.Recipe.Actions[i];
                if (action.Trigger != trigger) continue;
                if (!CheckCondition(action.Condition, tickCount)) continue;

                ExecuteActionWithSpecialHandling(ref config, ref state, ref action, tickCount, ctx);
            }
        }

        /// <summary>hitFrameIndex로 필터링된 AtHitFrame 액션만 디스패치 (오데트 multi-phase)</summary>
        private static void DispatchActionsForHitFrame(ref SkillConfig config, ref SkillState state, int hitFrameIndex, SkillExecuteContext ctx)
        {
            if (config.Recipe.Actions == null) return;

            for (int i = 0; i < config.Recipe.Actions.Length; i++)
            {
                ref var action = ref config.Recipe.Actions[i];
                if (action.Trigger != SkillTriggerType.AtHitFrame) continue;
                if (action.HitFrameIndex != hitFrameIndex) continue;
                if (!CheckCondition(action.Condition, 0)) continue;

                ExecuteActionWithSpecialHandling(ref config, ref state, ref action, 0, ctx);
            }
        }

        /// <summary>Knockback/SpawnProjectile/Retarget 특수 처리를 포함한 단일 액션 실행</summary>
        private static void ExecuteActionWithSpecialHandling(ref SkillConfig config, ref SkillState state, ref SkillAction action, int tickCount, SkillExecuteContext ctx)
        {
            // ── Knockback: 결과를 직접 저장 + OnKnockbackWall 디스패치 ──
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

                    int actualMoved = SkillCCHelper.Knockback(ctx.State, ref target, dirCol, dirRow, dist, config.WorldTickRate);
                    state.KnockbackHitWall = (byte)(actualMoved < dist ? 1 : 0);
                }
                else
                {
                    state.KnockbackHitWall = 0;
                }

                if (state.KnockbackHitWall != 0)
                    DispatchActions(ref config, ref state, SkillTriggerType.OnKnockbackWall, tickCount, ctx);

                return;
            }

            // ── SpawnProjectile: 도착 타이머 시작 ──
            if (action.Effect == SkillEffectType.SpawnProjectile)
            {
                ActionExecutor.Execute(ref action, ctx);
                int travelFrames = action.RepeatIntervalFrames > 0 ? action.RepeatIntervalFrames : 30;
                state.ProjectileArrivalTimer = travelFrames + 3; // +3 = DamageDelayFrames
                return;
            }

            // ── DashForward: DashSystem에 위임 ──
            if (action.Effect == SkillEffectType.DashForward)
            {
                ActionExecutor.Execute(ref action, ctx); // VFX 처리
                ref var caster = ref ctx.GetCaster();
                DashSystem.StartPhase(ctx.State, ref caster, ref state, ref action, ctx, config.WorldTickRate);
                return;
            }

            // ── Retarget: CachedTargetId 갱신 + ctx 재구성 ──
            if (action.Effect == SkillEffectType.Retarget)
            {
                // struct 복사 문제 우회: Retarget 결과를 직접 추출
                var retargetCtx = ctx; // 복사본에서 실행
                ActionExecutor.Execute(ref action, retargetCtx);
                int newTarget = retargetCtx.TargetCombatId;

                if (newTarget != CombatUnit.InvalidId)
                {
                    state.CachedTargetId = newTarget;
                    ctx.TargetCombatId = newTarget; // 이후 액션에 반영
                }
                else if (state.BounceCount > 0)
                {
                    // Retarget 실패 시 바운스 루프 종료
                    DispatchActions(ref config, ref state, SkillTriggerType.OnComplete, 0, ctx);
                    state.ProjectileArrivalTimer = 0;
                }
                return;
            }

            ActionExecutor.Execute(ref action, ctx);

            // 비투사체 Damage 실행 후 히트 추적 (시라유키 등 Retarget excludeHit 지원)
            if (action.Effect == SkillEffectType.Damage &&
                action.TargetFilter == SkillTargetFilter.PrimaryTarget &&
                state.HitIdCount < SkillState.MaxHitIds)
            {
                state.SetHitId(state.HitIdCount++, ctx.TargetCombatId);
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

        private static SkillExecuteContext MakeContext(ref SkillConfig config, ref SkillState state,
            CombatMatchState matchState, ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
        {
            // HitIds inline 필드 → scratch buffer로 복사 (ActionExecutor 호환)
            for (int i = 0; i < state.HitIdCount && i < SkillState.MaxHitIds; i++)
                HitIdScratch[i] = state.GetHitId(i);

            return new SkillExecuteContext
            {
                State = matchState,
                CasterCombatId = caster.CombatId,
                TargetCombatId = targetCombatId,
                DamageType = config.DamageType,
                SkillSpecId = config.SkillId,
                CasterTeam = caster.TeamIndex,
                WorldTickRate = config.WorldTickRate,
                Rng = rng,
                ParamValues = config.ParamValues,
                ParamSlots = config.Recipe?.ParamSlots,
                BasePowerPercent = config.PowerPercent,
                TickCount = state.TickCount,
                CurrentPower = state.CurrentPower > 0 ? state.CurrentPower : config.PowerPercent,
                BounceCount = state.BounceCount,
                HitIds = HitIdScratch,
                HitIdCount = state.HitIdCount,
                SavedGridCol = state.SavedGridCol,
                SavedGridRow = state.SavedGridRow,
            };
        }

        private static int FindAoERadius(ref SkillConfig config)
        {
            if (config.Recipe.Actions == null) return 1;
            for (int i = 0; i < config.Recipe.Actions.Length; i++)
            {
                if (config.Recipe.Actions[i].AreaRange > 0)
                    return config.Recipe.Actions[i].AreaRange;
            }
            return 1;
        }
    }
}
