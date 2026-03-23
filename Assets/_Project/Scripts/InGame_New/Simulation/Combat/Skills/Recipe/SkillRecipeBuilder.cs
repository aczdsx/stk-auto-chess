using System.Collections.Generic;

namespace CookApps.AutoChess
{
    /// <summary>
    /// SkillRecipe를 간결하게 선언하기 위한 Builder.
    /// new SkillAction { ... } 반복을 제거하고 체이닝 API 제공.
    ///
    /// 사용 예:
    ///   Recipe(id, DelayedApply, FarthestEnemy)
    ///       .Param(1, Int, 200f)
    ///       .OnCast(Vfx(0, AtGridPos))
    ///       .AtHit(Damage())
    ///       .AtHit(Vfx(1, AtCasterWithDir))
    ///       .Register();
    /// </summary>
    public struct SkillRecipeBuilder
    {
        private readonly Dictionary<int, SkillRecipe> _target;
        private readonly int _skillId;
        private SkillExecutionType _execType;
        private SkillTargetType _targetRule;
        private bool _hasProjectile;
        private readonly List<ParamSlot> _params;
        private readonly List<SkillAction> _actions;

        public SkillRecipeBuilder(Dictionary<int, SkillRecipe> target, int skillId,
            SkillExecutionType execType, SkillTargetType targetRule)
        {
            _target = target;
            _skillId = skillId;
            _execType = execType;
            _targetRule = targetRule;
            _hasProjectile = false;
            _params = new List<ParamSlot>(4);
            _actions = new List<SkillAction>(6);
        }

        // ── 기본 설정 ──

        public SkillRecipeBuilder Projectile()
        {
            _hasProjectile = true;
            return this;
        }

        /// <summary>specList 파라미터 슬롯 추가</summary>
        public SkillRecipeBuilder Param(byte specIndex, ParamValueType type, float fallback)
        {
            _params.Add(new ParamSlot(specIndex, type, fallback));
            return this;
        }

        // ── 액션 추가 (트리거별) ──

        public SkillRecipeBuilder OnCast(SkillAction action)
        {
            action.Trigger = SkillTriggerType.OnCast;
            _actions.Add(action);
            return this;
        }

        public SkillRecipeBuilder AtHit(SkillAction action, byte hitFrameIndex = 0)
        {
            action.Trigger = SkillTriggerType.AtHitFrame;
            action.HitFrameIndex = hitFrameIndex;
            _actions.Add(action);
            return this;
        }

        public SkillRecipeBuilder OnTick(SkillAction action)
        {
            action.Trigger = SkillTriggerType.OnTick;
            _actions.Add(action);
            return this;
        }

        public SkillRecipeBuilder OnComplete(SkillAction action)
        {
            action.Trigger = SkillTriggerType.OnComplete;
            _actions.Add(action);
            return this;
        }

        // ── 빌드 + 등록 ──

        public void Register()
        {
            _target[_skillId] = Build();
        }

        public SkillRecipe Build()
        {
            return new SkillRecipe
            {
                ExecutionType = _execType,
                TargetRule = _targetRule,
                HasProjectile = _hasProjectile,
                ParamSlots = _params.Count > 0 ? _params.ToArray() : null,
                Actions = _actions.Count > 0 ? _actions.ToArray() : null,
            };
        }

        // ══════════════════════════════
        // 액션 팩토리 (static) — 체이닝에서 사용
        // ══════════════════════════════

        /// <summary>데미지 (단일 타겟 또는 범위)</summary>
        public static SkillAction Damage(sbyte paramIndex = -1,
            SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget,
            SkillAreaShape area = SkillAreaShape.None, byte range = 0)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.Damage,
                TargetFilter = filter,
                AreaShape = area,
                AreaRange = range,
                ParamIndex = paramIndex,
            };
        }

        /// <summary>힐</summary>
        public static SkillAction Heal(sbyte paramIndex = -1,
            SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget,
            SkillAreaShape area = SkillAreaShape.None, byte range = 0)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.Heal,
                TargetFilter = filter,
                AreaShape = area,
                AreaRange = range,
                ParamIndex = paramIndex,
            };
        }

        /// <summary>CC 적용</summary>
        public static SkillAction CC(CrowdControlType ccType, sbyte durationParamIndex = -2)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.ApplyCC,
                TargetFilter = SkillTargetFilter.PrimaryTarget,
                CCType = ccType,
                SecondaryParamIndex = durationParamIndex,
            };
        }

        /// <summary>범위 CC</summary>
        public static SkillAction AreaCC(CrowdControlType ccType,
            SkillAreaShape area, byte range, sbyte durationParamIndex = -2)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.ApplyCC,
                TargetFilter = SkillTargetFilter.EnemiesInArea,
                AreaShape = area,
                AreaRange = range,
                CCType = ccType,
                SecondaryParamIndex = durationParamIndex,
            };
        }

        /// <summary>넉백</summary>
        public static SkillAction Knockback(sbyte distParamIndex = -1)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.Knockback,
                TargetFilter = SkillTargetFilter.PrimaryTarget,
                SecondaryParamIndex = distParamIndex,
            };
        }

        /// <summary>버프 (지속시간 기반)</summary>
        public static SkillAction Buff(StatModType stat, sbyte valueParamIndex,
            sbyte durationParamIndex, SkillTargetFilter filter = SkillTargetFilter.Self)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.ApplyBuff,
                TargetFilter = filter,
                BuffStat = stat,
                ParamIndex = valueParamIndex,
                SecondaryParamIndex = durationParamIndex,
            };
        }

        /// <summary>디버프 (StatusEffect 기반)</summary>
        public static SkillAction Debuff(StatusEffectType statusEffect,
            sbyte valueParamIndex, sbyte durationParamIndex,
            SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget,
            SkillAreaShape area = SkillAreaShape.None, byte range = 0)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.ApplyDebuff,
                TargetFilter = filter,
                AreaShape = area,
                AreaRange = range,
                StatusEffect = statusEffect,
                ParamIndex = valueParamIndex,
                SecondaryParamIndex = durationParamIndex,
            };
        }

        /// <summary>실드</summary>
        public static SkillAction Shield(sbyte percentParamIndex, sbyte durationParamIndex,
            SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.Shield,
                TargetFilter = filter,
                ParamIndex = percentParamIndex,
                SecondaryParamIndex = durationParamIndex,
            };
        }

        /// <summary>디버프 제거</summary>
        public static SkillAction RemoveDebuffs(SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget,
            SkillAreaShape area = SkillAreaShape.None, byte range = 0)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.RemoveDebuffs,
                TargetFilter = filter,
                AreaShape = area,
                AreaRange = range,
            };
        }

        /// <summary>마커 추가</summary>
        public static SkillAction AddMarker(SkillMarkerType marker)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.AddMarker,
                TargetFilter = SkillTargetFilter.PrimaryTarget,
                MarkerType = (byte)marker,
            };
        }

        /// <summary>다단히트</summary>
        public static SkillAction MultiHit(sbyte paramIndex = -1, byte hitCount = 3)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.MultiHit,
                TargetFilter = SkillTargetFilter.PrimaryTarget,
                ParamIndex = paramIndex,
                RepeatCount = hitCount,
            };
        }

        /// <summary>Homing 투사체 스폰</summary>
        public static SkillAction SpawnProjectile(sbyte paramIndex = -1,
            sbyte vfxIndex = -1, short travelFrames = 30)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.SpawnProjectile,
                TargetFilter = SkillTargetFilter.PrimaryTarget,
                ParamIndex = paramIndex,
                VfxIndex = vfxIndex,
                RepeatIntervalFrames = travelFrames,
            };
        }

        /// <summary>Linear 투사체 스폰</summary>
        public static SkillAction SpawnLinearProjectile(sbyte paramIndex = -1,
            byte length = 4, short moveInterval = 3, byte width = 1)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.SpawnLinearProjectile,
                TargetFilter = SkillTargetFilter.PrimaryTarget,
                ParamIndex = paramIndex,
                AreaRange = length,
                RepeatIntervalFrames = moveInterval,
                RepeatCount = width,
            };
        }

        /// <summary>즉시 영구 스탯 변경</summary>
        public static SkillAction ModifyStat(SkillTargetFilter filter = SkillTargetFilter.Self,
            sbyte paramIndex = -1)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.ModifyStat,
                TargetFilter = filter,
                ParamIndex = paramIndex,
            };
        }

        /// <summary>범위 데미지 + 중심→바깥 넉백 (메이)</summary>
        public static SkillAction DamageKnockback(SkillAreaShape area, byte range,
            sbyte paramIndex = -1)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.DamageKnockbackInArea,
                TargetFilter = SkillTargetFilter.EnemiesInArea,
                AreaShape = area,
                AreaRange = range,
                ParamIndex = paramIndex,
            };
        }

        /// <summary>순차 직선 타격 (보스탱커)</summary>
        public static SkillAction SequentialLine(sbyte paramIndex, byte lineLength,
            short intervalMs, byte repeatCount)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.SequentialLineDamage,
                TargetFilter = SkillTargetFilter.EnemiesInArea,
                AreaRange = lineLength,
                ParamIndex = paramIndex,
                RepeatCount = repeatCount,
                RepeatIntervalMs = intervalMs,
            };
        }

        /// <summary>VFX만 스폰 (효과 없음)</summary>
        public static SkillAction Vfx(sbyte vfxIndex, SkillVfxPlacement at)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.None,
                VfxIndex = vfxIndex,
                VfxAt = at,
            };
        }

        /// <summary>범위 VFX (AreaEffect 또는 PerTileInDiamond)</summary>
        public static SkillAction AreaVfx(SkillVfxPlacement at, byte range,
            sbyte vfxIndex = -1, SkillActionCondition condition = SkillActionCondition.Always)
        {
            return new SkillAction
            {
                Effect = SkillEffectType.None,
                VfxIndex = vfxIndex,
                VfxAt = at,
                AreaRange = range,
                Condition = condition,
            };
        }

        /// <summary>OnTick용: 반복 설정을 액션에 적용</summary>
        public static SkillAction WithRepeat(SkillAction action, byte count = 0,
            short intervalFrames = 0, short intervalMs = 0, bool dynamicFromClip = false)
        {
            action.RepeatCount = count;
            action.RepeatIntervalFrames = intervalFrames;
            action.RepeatIntervalMs = intervalMs;
            action.DynamicFromClip = dynamicFromClip;
            return action;
        }
    }
}
