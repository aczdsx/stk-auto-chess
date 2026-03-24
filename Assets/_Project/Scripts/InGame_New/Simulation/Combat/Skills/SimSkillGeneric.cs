using System.Collections.Generic;

using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// Recipe 기반 범용 스킬 실행기.
    /// SkillRecipe의 Action 배열을 타이밍에 따라 디스패치.
    /// 아키타입 클래스(SingleDamage, AoEDamage 등)와 대부분의 커스텀 클래스를 대체.
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
        private int _ccDurationFramesOverride;

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

            // SkillParams의 Param0~3, HitCount 등을 Recipe Actions에 오버라이드
            // 아키타입 스킬에서 SkillSpecAdapter가 설정한 값을 Recipe에 반영
            ApplyParamOverrides(baseParams);
        }

        /// <summary>
        /// SkillParams의 Param0~3 값을 Recipe Actions의 AreaRange/RepeatCount 등에 반영.
        /// 아키타입 스킬의 SkillSpecAdapter 기본값을 Recipe가 사용할 수 있게 함.
        /// </summary>
        private void ApplyParamOverrides(SkillParams p)
        {
            if (_recipe?.Actions == null) return;

            for (int i = 0; i < _recipe.Actions.Length; i++)
            {
                ref var action = ref _recipe.Actions[i];

                // AreaRange: Param0이 설정되어 있으면 오버라이드
                if (action.AreaRange > 0 && p.Param0 > 0)
                    action.AreaRange = (byte)p.Param0;

                // MultiHit RepeatCount: HitCount가 설정되어 있으면 오버라이드
                if (action.Effect == SkillEffectType.MultiHit && p.HitCount > 0)
                    action.RepeatCount = (byte)p.HitCount;

                // LowestHpAllies AreaRange: TargetCount가 설정되어 있으면 오버라이드
                if (action.TargetFilter == SkillTargetFilter.LowestHpAllies && p.TargetCount > 0)
                    action.AreaRange = (byte)p.TargetCount;

                // CC: SkillParams에서 CCType/Duration이 설정되어 있으면 오버라이드
                if (action.Effect == SkillEffectType.ApplyCC)
                {
                    if (p.CCType != CrowdControlType.None)
                        action.CCType = p.CCType;
                    if (p.CCDurationFrames > 0)
                        action.SecondaryParamIndex = -2; // 특수값: CCDurationFrames 직접 사용
                }
            }

            // CCDurationFrames를 직접 참조할 수 있도록 저장
            _ccDurationFramesOverride = p.CCDurationFrames;
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

            var ctx = MakeContext(state, ref caster, targetCombatId, ref rng);

            // OnCast 트리거 액션 실행
            DispatchActions(SkillTriggerType.OnCast, 0, ctx);

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

            // 시작 딜레이
            if (_startDelay > 0)
            {
                _startDelay--;
                if (_startDelay == 0)
                    DispatchActions(SkillTriggerType.AtHitFrame, 0, ctx);
                return true;
            }

            // 틱 간격
            _tickTimer--;
            if (_tickTimer <= 0)
            {
                _tickTimer = _tickInterval;
                _tickCount++;

                DispatchActions(SkillTriggerType.OnTick, _tickCount, ctx);
                _remainingTicks--;
            }

            // 종료 체크
            if (_remainingTicks <= 0)
            {
                DispatchActions(SkillTriggerType.OnComplete, 0, ctx);
                return false;
            }

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
            for (int i = 0; i < _recipe.Actions.Length; i++)
            {
                ref var action = ref _recipe.Actions[i];
                if (action.Trigger != SkillTriggerType.OnTick) continue;

                // 틱 간격: Ms가 있으면 tickRate 변환, 없으면 Frames 직접 사용
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
                break; // 첫 번째 OnTick 액션에서 틱 설정
            }

            _tickTimer = _tickInterval;
        }

        private void DispatchActions(SkillTriggerType trigger, int tickCount, SkillExecuteContext ctx)
        {
            if (_recipe.Actions == null) return;

            for (int i = 0; i < _recipe.Actions.Length; i++)
            {
                ref var action = ref _recipe.Actions[i];
                if (action.Trigger != trigger) continue;

                // 조건 체크
                if (!CheckCondition(action.Condition, tickCount)) continue;

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
                    return false; // 외부에서 별도 처리
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
                CCDurationOverride = _ccDurationFramesOverride,
                TickCount = _tickCount,
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
