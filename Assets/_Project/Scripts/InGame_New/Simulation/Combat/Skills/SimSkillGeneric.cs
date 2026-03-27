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
        // ══════════════════════════════
        // 초기화
        // ══════════════════════════════

        public static void InitializeFromSpec(ref SimSkillInstance skill, List<SkillActive> specList, int tickRate)
        {
            skill.WorldTickRate = tickRate;

            if (skill.Recipe == null) return;

            // Recipe에서 ExecutionType, HasProjectile 동기화
            skill.ExecutionType = skill.Recipe.ExecutionType;
            skill.HasProjectile = skill.Recipe.HasProjectile;

            // ParamSlots에 따라 specList에서 수치 추출
            if (skill.Recipe.ParamSlots != null && skill.Recipe.ParamSlots.Length > 0)
            {
                skill.ParamValues = new int[skill.Recipe.ParamSlots.Length];
                for (int i = 0; i < skill.Recipe.ParamSlots.Length; i++)
                {
                    var slot = skill.Recipe.ParamSlots[i];
                    if (slot.SpecIndex == 255)
                    {
                        // 고정값 (ValueRef.Fixed) — specData 무시
                        skill.ParamValues[i] = slot.ValueType == ParamValueType.Frames
                            ? (int)(slot.Fallback * tickRate + 0.5f)
                            : UnityEngine.Mathf.RoundToInt(slot.Fallback);
                    }
                    else
                    {
                        skill.ParamValues[i] = slot.ValueType == ParamValueType.Frames
                            ? SkillSpecHelper.GetFrames(specList, slot.SpecIndex, slot.Fallback, tickRate)
                            : SkillSpecHelper.GetInt(specList, slot.SpecIndex, slot.Fallback);
                    }
                }

                // 첫 번째 슬롯은 관례적으로 PowerPercent
                if (skill.ParamValues.Length > 0 && skill.ParamValues[0] > 0)
                    skill.PowerPercent = skill.ParamValues[0];
            }
        }

        // ══════════════════════════════
        // 타겟 선정
        // ══════════════════════════════

        public static int SelectTarget(ref SimSkillInstance skill, CombatMatchState state, ref CombatUnit caster)
        {
            switch (skill.Recipe.TargetRule)
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
                    int radius = FindAoERadius(ref skill);
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

        public static void Execute(ref SimSkillInstance skill, CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            skill.CachedTargetId = targetCombatId;
            skill.CurrentPower = skill.PowerPercent;
            skill.BounceCount = 0;
            skill.HitIdCount = 0;
            skill.KnockbackHitWall = false;
            skill.ProjectileArrivalTimer = 0;
            skill.CompleteFired = false;
            skill.PostCompleteTimer = 0;
            skill.CurrentHitFrameIndex = 0;
            skill.HitFrameTimer = 0;
            skill.StartDelay = 0;
            skill.TickTimer = 0;
            skill.TickInterval = 0;
            skill.RemainingTicks = 0;
            skill.TickCount = 0;

            // Recipe에서 DecayParamIndex가 있는 액션 찾아서 감쇠율 캐시
            skill.DecayPercent = 0;
            if (skill.Recipe.Actions != null)
            {
                for (int i = 0; i < skill.Recipe.Actions.Length; i++)
                {
                    if (skill.Recipe.Actions[i].DecayParamIndex >= 0)
                    {
                        skill.DecayPercent = skill.ParamValues != null && skill.Recipe.Actions[i].DecayParamIndex < skill.ParamValues.Length
                            ? skill.ParamValues[skill.Recipe.Actions[i].DecayParamIndex] : 0;
                        break;
                    }
                }
            }

            var ctx = MakeContext(ref skill, state, ref caster, targetCombatId, ref rng);

            // OnCast 트리거 액션 실행
            DispatchActions(ref skill, SkillTriggerType.OnCast, 0, ctx);

            // Retarget으로 변경된 타겟 동기화
            skill.CachedTargetId = ctx.TargetCombatId;

            // 채널링 초기화
            if (skill.ExecutionType == SkillExecutionType.Channeling)
                InitChanneling(ref skill);
        }

        // ══════════════════════════════
        // DelayedApply 지원
        // ══════════════════════════════

        public static void ApplySkillEffect(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, ref DeterministicRNG rng)
        {
            var ctx = MakeContext(ref skill, state, ref caster, skill.CachedTargetId, ref rng);
            DispatchActions(ref skill, SkillTriggerType.AtHitFrame, 0, ctx);
            skill.CachedTargetId = ctx.TargetCombatId;
        }

        // ══════════════════════════════
        // Channeling
        // ══════════════════════════════

        public static bool OnChannelTick(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, ref DeterministicRNG rng)
        {
            // DelayedApply는 SkillDispatcher가 처리
            if (skill.ExecutionType == SkillExecutionType.DelayedApply)
                return SkillDispatcher.OnChannelTickDelayedApply(ref skill, state, ref caster, ref rng);

            var ctx = MakeContext(ref skill, state, ref caster, skill.CachedTargetId, ref rng);

            // ── Multi-hitframe 처리 (오데트 2페이즈) ──
            if (skill.HasMultiHitFrames)
            {
                if (skill.HitFrameTimer > 0)
                {
                    skill.HitFrameTimer--;
                    if (skill.HitFrameTimer <= 0)
                    {
                        DispatchActionsForHitFrame(ref skill, skill.CurrentHitFrameIndex, ctx);
                        skill.CachedTargetId = ctx.TargetCombatId;
                        skill.CurrentHitFrameIndex++;

                        if (skill.SkillHitFrames != null && skill.CurrentHitFrameIndex < skill.SkillHitFrames.Length)
                        {
                            skill.HitFrameTimer = skill.SkillHitFrames[skill.CurrentHitFrameIndex]
                                                - skill.SkillHitFrames[skill.CurrentHitFrameIndex - 1];
                        }
                        else
                        {
                            // 모든 hitframe 처리 완료 → OnComplete + 클립 끝까지 대기
                            if (!skill.CompleteFired)
                            {
                                skill.CompleteFired = true;
                                DispatchActions(ref skill, SkillTriggerType.OnComplete, 0, ctx);
                                // 남은 클립 프레임 계산
                                int lastHitFrame = skill.SkillHitFrames[skill.CurrentHitFrameIndex - 1];
                                skill.PostCompleteTimer = skill.SkillClipFrames > lastHitFrame
                                    ? skill.SkillClipFrames - lastHitFrame : 0;
                            }
                        }
                    }
                }

                // 투사체 도착도 체크 (multi-hitframe 스킬이 투사체도 쓸 수 있음)
                if (skill.ProjectileArrivalTimer > 0)
                {
                    skill.ProjectileArrivalTimer--;
                    if (skill.ProjectileArrivalTimer <= 0)
                        HandleProjectileArrival(ref skill, ctx);
                }

                // post-complete 대기 (클립 끝까지)
                if (skill.CompleteFired)
                {
                    skill.PostCompleteTimer--;
                    return skill.PostCompleteTimer > 0;
                }

                return true;
            }

            // ── 시작 딜레이 ──
            if (skill.StartDelay > 0)
            {
                skill.StartDelay--;
                if (skill.StartDelay == 0)
                {
                    DispatchActions(ref skill, SkillTriggerType.AtHitFrame, 0, ctx);
                    skill.CachedTargetId = ctx.TargetCombatId;
                }
                return true;
            }

            // ── 투사체 도착 타이머 (라키유/미노/베인) ──
            if (skill.ProjectileArrivalTimer > 0)
            {
                skill.ProjectileArrivalTimer--;
                if (skill.ProjectileArrivalTimer <= 0)
                    HandleProjectileArrival(ref skill, ctx);
            }

            // ── 틱 간격 ──
            if (skill.TickInterval > 0)
            {
                skill.TickTimer--;
                if (skill.TickTimer <= 0)
                {
                    skill.TickTimer = skill.TickInterval;
                    skill.TickCount++;

                    DispatchActions(ref skill, SkillTriggerType.OnTick, skill.TickCount, ctx);
                    skill.CachedTargetId = ctx.TargetCombatId;
                    skill.RemainingTicks--;
                }

                // 종료 체크
                if (skill.RemainingTicks <= 0)
                {
                    DispatchActions(ref skill, SkillTriggerType.OnComplete, 0, ctx);
                    return false;
                }
            }

            // 투사체 체인 루프 진행 중이면 계속 유지
            if (skill.ProjectileArrivalTimer > 0)
                return true;

            // 틱도 없고 투사체도 없으면 종료 (AtHitFrame만 있는 채널링)
            if (skill.TickInterval <= 0 && skill.ProjectileArrivalTimer <= 0 && !skill.HasMultiHitFrames)
                return false;

            return true;
        }

        // ══════════════════════════════
        // 내부
        // ══════════════════════════════

        private static void InitChanneling(ref SimSkillInstance skill)
        {
            skill.StartDelay = skill.SkillHitFrames != null && skill.SkillHitFrames.Length > 0
                ? skill.SkillHitFrames[0] : 0;
            skill.TickCount = 0;

            // Recipe에서 OnTick 액션을 찾아 틱 설정
            // RepeatCount/DynamicFromClip/RepeatIntervalMs가 있는 액션 우선 (미노: Retarget보다 SpawnProjectile 우선)
            if (skill.Recipe.Actions != null)
            {
                int bestTickIdx = -1;
                for (int i = 0; i < skill.Recipe.Actions.Length; i++)
                {
                    ref var action = ref skill.Recipe.Actions[i];
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
                    ref var action = ref skill.Recipe.Actions[bestTickIdx];
                    int interval;
                    if (action.RepeatIntervalMs > 0)
                        interval = (int)(action.RepeatIntervalMs * skill.WorldTickRate / 1000f + 0.5f);
                    else if (action.RepeatIntervalFrames > 0)
                        interval = action.RepeatIntervalFrames;
                    else
                        interval = 15;

                    if (action.DynamicFromClip)
                    {
                        int channelFrames = skill.SkillClipFrames - skill.StartDelay;
                        skill.TickInterval = interval;
                        skill.RemainingTicks = channelFrames > 0
                            ? channelFrames / skill.TickInterval : 1;
                    }
                    else
                    {
                        skill.TickInterval = interval;
                        skill.RemainingTicks = action.RepeatCount > 0
                            ? action.RepeatCount : 1;
                    }
                }
            }

            skill.TickTimer = skill.TickInterval;

            // Multi-hitframe 감지 (오데트: AtHitFrame + hitFrameIndex > 0)
            skill.HasMultiHitFrames = false;
            if (skill.Recipe.Actions != null)
            {
                for (int i = 0; i < skill.Recipe.Actions.Length; i++)
                {
                    if (skill.Recipe.Actions[i].Trigger == SkillTriggerType.AtHitFrame &&
                        skill.Recipe.Actions[i].HitFrameIndex > 0)
                    {
                        skill.HasMultiHitFrames = true;
                        break;
                    }
                }
            }
            if (skill.HasMultiHitFrames && skill.SkillHitFrames != null && skill.SkillHitFrames.Length > 0)
            {
                skill.CurrentHitFrameIndex = 0;
                skill.HitFrameTimer = skill.SkillHitFrames[0];
                skill.StartDelay = 0; // multi-hitframe이 타이밍 관리 → _startDelay 이중 발동 방지
            }
        }

        /// <summary>투사체 도착 처리 — OnProjectileArrive 디스패치 + 바운스 루프</summary>
        private static void HandleProjectileArrival(ref SimSkillInstance skill, SkillExecuteContext ctx)
        {
            skill.BounceCount++;

            // 히트 추적
            if (skill.HitIdCount < skill.HitIds.Length)
                skill.HitIds[skill.HitIdCount++] = skill.CachedTargetId;

            // 감쇠 적용
            if (skill.DecayPercent > 0)
                skill.CurrentPower = skill.CurrentPower * (100 - skill.DecayPercent) / 100;

            DispatchActions(ref skill, SkillTriggerType.OnProjectileArrive, skill.BounceCount, ctx);
            skill.CachedTargetId = ctx.TargetCombatId;

            // 바운스 루프 종료 체크 (Retarget 실패 또는 최대 바운스 도달)
            if (skill.ProjectileArrivalTimer <= 0 && skill.BounceCount >= skill.TargetCount && skill.TargetCount > 1)
            {
                DispatchActions(ref skill, SkillTriggerType.OnComplete, 0, ctx);
            }
        }

        private static void DispatchActions(ref SimSkillInstance skill, SkillTriggerType trigger, int tickCount, SkillExecuteContext ctx)
        {
            if (skill.Recipe.Actions == null) return;

            for (int i = 0; i < skill.Recipe.Actions.Length; i++)
            {
                ref var action = ref skill.Recipe.Actions[i];
                if (action.Trigger != trigger) continue;
                if (!CheckCondition(action.Condition, tickCount)) continue;

                ExecuteActionWithSpecialHandling(ref skill, ref action, tickCount, ctx);
            }
        }

        /// <summary>hitFrameIndex로 필터링된 AtHitFrame 액션만 디스패치 (오데트 multi-phase)</summary>
        private static void DispatchActionsForHitFrame(ref SimSkillInstance skill, int hitFrameIndex, SkillExecuteContext ctx)
        {
            if (skill.Recipe.Actions == null) return;

            for (int i = 0; i < skill.Recipe.Actions.Length; i++)
            {
                ref var action = ref skill.Recipe.Actions[i];
                if (action.Trigger != SkillTriggerType.AtHitFrame) continue;
                if (action.HitFrameIndex != hitFrameIndex) continue;
                if (!CheckCondition(action.Condition, 0)) continue;

                ExecuteActionWithSpecialHandling(ref skill, ref action, 0, ctx);
            }
        }

        /// <summary>Knockback/SpawnProjectile/Retarget 특수 처리를 포함한 단일 액션 실행</summary>
        private static void ExecuteActionWithSpecialHandling(ref SimSkillInstance skill, ref SkillAction action, int tickCount, SkillExecuteContext ctx)
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

                    int actualMoved = SkillCCHelper.Knockback(ctx.State, ref target, dirCol, dirRow, dist, skill.WorldTickRate);
                    skill.KnockbackHitWall = actualMoved < dist;
                }
                else
                {
                    skill.KnockbackHitWall = false;
                }

                if (skill.KnockbackHitWall)
                    DispatchActions(ref skill, SkillTriggerType.OnKnockbackWall, tickCount, ctx);

                return;
            }

            // ── SpawnProjectile: 도착 타이머 시작 ──
            if (action.Effect == SkillEffectType.SpawnProjectile)
            {
                ActionExecutor.Execute(ref action, ctx);
                int travelFrames = action.RepeatIntervalFrames > 0 ? action.RepeatIntervalFrames : 30;
                skill.ProjectileArrivalTimer = travelFrames + 3; // +3 = DamageDelayFrames
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
                    skill.CachedTargetId = newTarget;
                    ctx.TargetCombatId = newTarget; // 이후 액션에 반영
                }
                else if (skill.BounceCount > 0)
                {
                    // Retarget 실패 시 바운스 루프 종료
                    DispatchActions(ref skill, SkillTriggerType.OnComplete, 0, ctx);
                    skill.ProjectileArrivalTimer = 0;
                }
                return;
            }

            ActionExecutor.Execute(ref action, ctx);

            // 비투사체 Damage 실행 후 히트 추적 (시라유키 등 Retarget excludeHit 지원)
            if (action.Effect == SkillEffectType.Damage &&
                action.TargetFilter == SkillTargetFilter.PrimaryTarget &&
                skill.HitIdCount < skill.HitIds.Length)
            {
                skill.HitIds[skill.HitIdCount++] = ctx.TargetCombatId;
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

        private static SkillExecuteContext MakeContext(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
        {
            return new SkillExecuteContext
            {
                State = state,
                CasterCombatId = caster.CombatId,
                TargetCombatId = targetCombatId,
                DamageType = skill.DamageType,
                SkillSpecId = skill.SkillId,
                CasterTeam = caster.TeamIndex,
                WorldTickRate = skill.WorldTickRate,
                Rng = rng,
                ParamValues = skill.ParamValues,
                ParamSlots = skill.Recipe?.ParamSlots,
                BasePowerPercent = skill.PowerPercent,
                TickCount = skill.TickCount,
                CurrentPower = skill.CurrentPower > 0 ? skill.CurrentPower : skill.PowerPercent,
                BounceCount = skill.BounceCount,
                HitIds = skill.HitIds,
                HitIdCount = skill.HitIdCount,
            };
        }

        private static int FindAoERadius(ref SimSkillInstance skill)
        {
            if (skill.Recipe.Actions == null) return 1;
            for (int i = 0; i < skill.Recipe.Actions.Length; i++)
            {
                if (skill.Recipe.Actions[i].AreaRange > 0)
                    return skill.Recipe.Actions[i].AreaRange;
            }
            return 1;
        }
    }
}
