using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 스킬 팩토리 + Recipe 레지스트리 + 스펙 어댑터 통합.
    /// SkillId → SkillConfig 생성, Recipe 정의/조회, 스펙 변환을 모두 담당.
    ///
    /// partial class로 분리:
    /// - SkillFactory.cs — Core (딕셔너리, Create, Initialize, SkillRecipeBuilder)
    /// - SkillFactory.SpecAdapter.cs — 스펙 변환 (BuildParams, ExtractSkillHitTimes)
    /// - SkillFactory.Character.cs — 플레이어 스킬 Recipe 정의
    /// - SkillFactory.Monster.cs — 몬스터 스킬 Recipe 정의 + Preset
    /// </summary>
    public static partial class SkillFactory
    {
        // ── 타입 레지스트리 ──
        private static readonly Dictionary<int, SkillImplType> _typeRegistry = new();
        private static readonly Dictionary<int, SkillParams> _paramsCache = new();
        private static readonly Dictionary<int, List<SkillActive>> _specListCache = new();
        private static bool _initialized;

        // ── Recipe 레지스트리 ──
        private static readonly Dictionary<int, SkillRecipe> _recipes = new();

        static SkillFactory()
        {
            RegisterPlayerRecipes();
            RegisterMonsterRecipes();
        }

        // ══════════════════════════════
        // Public API
        // ══════════════════════════════

        public static void RegisterType(int skillId, SkillImplType type)
        {
            _typeRegistry[skillId] = type;
        }

        public static SkillConfig Create(int skillId)
        {
            var config = new SkillConfig();
            config.Type = _typeRegistry.TryGetValue(skillId, out var type) ? type : SkillImplType.Generic;
            if (config.Type == SkillImplType.Generic && _recipes.TryGetValue(skillId, out var recipe))
                config.Recipe = recipe;
            return config;
        }

        /// <summary>캐시된 SkillParams 조회</summary>
        public static bool TryGetParams(int skillId, out SkillParams p)
        {
            return _paramsCache.TryGetValue(skillId, out p);
        }

        /// <summary>캐시된 SkillActive 스펙 리스트 조회 (커스텀 스킬 자체 파싱용)</summary>
        public static bool TryGetSpecList(int skillId, out List<SkillActive> specList)
        {
            return _specListCache.TryGetValue(skillId, out specList);
        }

        // ══════════════════════════════
        // Recipe 조회 (내부용)
        // ══════════════════════════════

        internal static bool TryGetRecipe(int skillGroupId, out SkillRecipe recipe)
            => _recipes.TryGetValue(skillGroupId, out recipe);

        // ══════════════════════════════
        // 초기화
        // ══════════════════════════════

        /// <summary>SkillActive 스펙 테이블 기반 자동 등록</summary>
        public static void Initialize(int tickRate)
        {
            if (_initialized) return;
            _initialized = true;

            // 커스텀 스킬 먼저 등록 (스펙 기반 등록에서 덮어쓰지 않도록)
            RegisterCustomSkills();

            var specManager = SpecDataManager.Instance;
            var allSkills = specManager?.SkillActive?.All;
            if (allSkills == null) return;

            for (int i = 0; i < allSkills.Count; i++)
            {
                var spec = allSkills[i];

                // PASSIVE, NONE 타입은 스킵
                if (spec.skill_type != SkillType.NORMAL &&
                    spec.skill_type != SkillType.WEAPON &&
                    spec.skill_type != SkillType.ACTIVE)
                    continue;

                int id = spec.skill_group_id;

                // 같은 skill_group_id로 이미 등록된 경우 스킵 (성급별 중복 방지)
                if (_paramsCache.ContainsKey(id)) continue;

                var specList = specManager.GetSkillDataList(id);
                var skillParams = BuildParams(spec, specList, tickRate);

                _paramsCache[id] = skillParams;
                if (specList != null)
                    _specListCache[id] = specList;

                // 커스텀 스킬이 이미 등록되어 있으면 스킵
                if (_typeRegistry.ContainsKey(id)) continue;

                // 개별 Recipe가 있으면 Generic으로 등록
                if (TryGetRecipe(id, out _))
                    _typeRegistry[id] = SkillImplType.Generic;
            }

            // Recipe는 있지만 미등록된 스킬 fallback 등록
            foreach (var kv in _recipes)
            {
                if (_typeRegistry.ContainsKey(kv.Key)) continue;

                if (!_paramsCache.ContainsKey(kv.Key))
                {
                    var recipe = kv.Value;
                    _paramsCache[kv.Key] = new SkillParams
                    {
                        SkillId = kv.Key,
                        PowerPercent = 200,
                        DamageType = DamageType.Physical,
                        WorldTickRate = tickRate,
                        TargetCount = 1,
                        HitCount = 1,
                        TargetType = recipe.TargetRule,
                        FaceTarget = recipe.TargetRule != SkillTargetType.Self
                            && recipe.TargetRule != SkillTargetType.LowestHPAlly,
                    };
                }

                _typeRegistry[kv.Key] = SkillImplType.Generic;
            }
        }

        private static void RegisterCustomSkills()
        {
            RegisterType(217653505, SkillImplType.Enki);     // 엔키: 보드 스윕 투사체
            RegisterType(217333202, SkillImplType.April);    // 에이프릴: 확장 콘 + 거리별 배율
            RegisterType(217523403, SkillImplType.Adria);    // 아드리아: 3단계 확장 + 비트마스크
            RegisterType(217263103, SkillImplType.Rukida);   // 루키다: 마커 카운트 기반 동적 버프
        }

        /// <summary>팩토리 등록 해제 (테스트/재시작용)</summary>
        public static void Clear()
        {
            _typeRegistry.Clear();
            _paramsCache.Clear();
            _specListCache.Clear();
            _initialized = false;
            // _recipes는 static constructor에서 1회 초기화되는 불변 데이터 — Clear 대상 아님
        }

        // ══════════════════════════════
        // Recipe Builder 헬퍼
        // ══════════════════════════════

        /// <summary>스킬 Recipe Builder 시작. Register()로 _recipes에 자동 등록.</summary>
        private static SkillRecipeBuilder Skill(int skillId, SkillExecutionType exec, SkillTargetType target)
            => new SkillRecipeBuilder(_recipes, skillId, exec, target);

        // ── Preset 함수 (파라미터 없는 고정 패턴) ──

        static SkillRecipeBuilder PresetDamageStun(SkillRecipeBuilder b)
            => b.On(SkillEvent.Cast).Do(SkillRecipeBuilder.Damage()).Do(SkillRecipeBuilder.CC(CrowdControlType.Stun));

        static SkillRecipeBuilder PresetSingleDamage(SkillRecipeBuilder b)
            => b.On(SkillEvent.Cast).Do(SkillRecipeBuilder.Damage());

        static SkillRecipeBuilder PresetConeDamage(SkillRecipeBuilder b)
            => b.On(SkillEvent.Cast).Do(SkillRecipeBuilder.Damage(filter: SkillTargetFilter.EnemiesInArea, area: SkillAreaShape.Line, range: 2));

        static SkillRecipeBuilder PresetMultiHit(SkillRecipeBuilder b)
            => b.On(SkillEvent.Cast).Do(SkillRecipeBuilder.MultiHit());

        static SkillRecipeBuilder PresetMultiTargetHeal(SkillRecipeBuilder b)
            => b.On(SkillEvent.Cast).Do(SkillRecipeBuilder.Heal(filter: SkillTargetFilter.LowestHpAllies, range: 3));

        // ══════════════════════════════
        // ValueRef — 값 참조 (빌드 시점 전용)
        // ══════════════════════════════

        /// <summary>값 참조 — specData 또는 고정값. 빌드 시점에만 사용, 런타임에는 ParamIndex로 변환됨.</summary>
        public readonly struct ValueRef
        {
            public readonly byte SpecIndex;        // specData 인덱스 (255 = 고정값, 254 = 미지정)
            public readonly ParamValueType Type;
            public readonly float Value;           // fallback 또는 고정값
            public readonly bool IsDefault;        // true = PowerPercent 사용

            private ValueRef(byte specIndex, ParamValueType type, float value, bool isDefault)
            {
                SpecIndex = specIndex; Type = type; Value = value; IsDefault = isDefault;
            }

            /// <summary>specData에서 값 읽기 (fallback: specData 없을 때 기본값)</summary>
            public static ValueRef Spec(byte specIndex, float fallback)
                => new ValueRef(specIndex, ParamValueType.Int, fallback, false);
            public static ValueRef Spec(byte specIndex, ParamValueType type, float fallback)
                => new ValueRef(specIndex, type, fallback, false);

            /// <summary>고정값 (specData 무시)</summary>
            public static ValueRef Fixed(float value)
                => new ValueRef(255, ParamValueType.Int, value, false);
            public static ValueRef FixedFrames(float seconds)
                => new ValueRef(255, ParamValueType.Frames, seconds, false);

            /// <summary>공격력 기반 비율 — 런타임에 attack * value / 100 절대값으로 변환</summary>
            public static ValueRef AtkPercent(byte specIndex, float fallback)
                => new ValueRef(specIndex, ParamValueType.AtkPercent, fallback, false);
            public static ValueRef AtkPercentFixed(float percent)
                => new ValueRef(255, ParamValueType.AtkPercent, percent, false);

            /// <summary>PowerPercent 참조 (기본 데미지 배율)</summary>
            public static readonly ValueRef Power = new ValueRef(254, ParamValueType.Int, 0, true);

            /// <summary>미지정 (ParamIndex = -1)</summary>
            internal static readonly ValueRef None = new ValueRef(254, ParamValueType.Int, 0, true);

            internal bool IsNone => SpecIndex == 254;
        }

        /// <summary>빌드 전 액션 + 값 참조. Do()에서 ParamIndex로 자동 변환됨.</summary>
        public readonly struct ActionTemplate
        {
            public readonly SkillAction Action;
            public readonly ValueRef PrimaryValue;
            public readonly ValueRef SecondaryValue;
            public readonly ValueRef DecayValue;

            public ActionTemplate(SkillAction action,
                ValueRef primary = default, ValueRef secondary = default, ValueRef decay = default)
            {
                Action = action;
                PrimaryValue = primary.SpecIndex == 0 && !primary.IsDefault && primary.Value == 0 ? ValueRef.None : primary;
                SecondaryValue = secondary.SpecIndex == 0 && !secondary.IsDefault && secondary.Value == 0 ? ValueRef.None : secondary;
                DecayValue = decay.SpecIndex == 0 && !decay.IsDefault && decay.Value == 0 ? ValueRef.None : decay;
            }
        }

        // ══════════════════════════════
        // Recipe Builder (inner struct)
        // ══════════════════════════════

        /// <summary>
        /// SkillRecipe를 간결하게 선언하기 위한 Builder.
        /// 액션 팩토리가 ActionTemplate(값 참조 포함)을 반환, Do()에서 ParamSlots 자동 할당.
        /// </summary>
        internal struct SkillRecipeBuilder
        {
            private readonly Dictionary<int, SkillRecipe> _target;
            private readonly int _skillId;
            private SkillExecutionType _execType;
            private SkillTargetType _targetRule;
            private bool _hasProjectile;
            private bool _suppressAutoSound;
            private readonly List<ParamSlot> _params;
            private readonly List<SkillAction> _actions;
            private TraitTag _explicitTags;
            private SkillTriggerType _currentTrigger;
            private byte _currentHitFrameIndex;

            public SkillRecipeBuilder(Dictionary<int, SkillRecipe> target, int skillId,
                SkillExecutionType execType, SkillTargetType targetRule)
            {
                _target = target;
                _skillId = skillId;
                _execType = execType;
                _targetRule = targetRule;
                _hasProjectile = false;
                _suppressAutoSound = false;
                _explicitTags = TraitTag.None;
                _currentTrigger = SkillTriggerType.OnCast;
                _currentHitFrameIndex = 0;
                _params = new List<ParamSlot>(4);
                _actions = new List<SkillAction>(6);
            }

            // ── 기본 설정 ──

            public SkillRecipeBuilder Projectile()
            {
                _hasProjectile = true;
                return this;
            }

            /// <summary>Cast 시 SkillSoundResolver 자동 재생을 억제하고 Sound() 액션으로 직접 제어</summary>
            public SkillRecipeBuilder SuppressAutoSound()
            {
                _suppressAutoSound = true;
                return this;
            }

            public SkillRecipeBuilder WithTags(TraitTag tags)
            {
                _explicitTags |= tags;
                return this;
            }

            public SkillRecipeBuilder Apply(System.Func<SkillRecipeBuilder, SkillRecipeBuilder> preset)
                => preset(this);

            // ── 이벤트 트리거 ──

            public SkillRecipeBuilder On(SkillEvent evt)
            {
                switch (evt)
                {
                    case SkillEvent.Cast:             _currentTrigger = SkillTriggerType.OnCast; _currentHitFrameIndex = 0; break;
                    case SkillEvent.Execute1:          _currentTrigger = SkillTriggerType.AtHitFrame; _currentHitFrameIndex = 0; break;
                    case SkillEvent.Execute2:          _currentTrigger = SkillTriggerType.AtHitFrame; _currentHitFrameIndex = 1; break;
                    case SkillEvent.Execute3:          _currentTrigger = SkillTriggerType.AtHitFrame; _currentHitFrameIndex = 2; break;
                    case SkillEvent.Execute4:          _currentTrigger = SkillTriggerType.AtHitFrame; _currentHitFrameIndex = 3; break;
                    case SkillEvent.Tick:              _currentTrigger = SkillTriggerType.OnTick; _currentHitFrameIndex = 0; break;
                    case SkillEvent.Complete:          _currentTrigger = SkillTriggerType.OnComplete; _currentHitFrameIndex = 0; break;
                    case SkillEvent.KnockbackWall:    _currentTrigger = SkillTriggerType.OnKnockbackWall; _currentHitFrameIndex = 0; break;
                    case SkillEvent.ProjectileArrive:  _currentTrigger = SkillTriggerType.OnProjectileArrive; _currentHitFrameIndex = 0; break;
                }
                return this;
            }

            // ── 액션 추가 ──

            /// <summary>ValueRef가 있는 액션 (값 참조 → ParamSlot 자동 할당)</summary>
            public SkillRecipeBuilder Do(ActionTemplate template)
            {
                var action = template.Action;
                action.Trigger = _currentTrigger;
                if (_currentTrigger == SkillTriggerType.AtHitFrame)
                    action.HitFrameIndex = _currentHitFrameIndex;

                action.ParamIndex = AllocateSlot(template.PrimaryValue);
                action.SecondaryParamIndex = AllocateSlot(template.SecondaryValue);
                if (!template.DecayValue.IsNone)
                    action.DecayParamIndex = AllocateSlot(template.DecayValue);

                _actions.Add(action);
                return this;
            }

            /// <summary>값 참조가 없는 액션 (VFX, Teleport, Retarget 등)</summary>
            public SkillRecipeBuilder Do(SkillAction action)
            {
                action.Trigger = _currentTrigger;
                if (_currentTrigger == SkillTriggerType.AtHitFrame)
                    action.HitFrameIndex = _currentHitFrameIndex;
                _actions.Add(action);
                return this;
            }

            // ── 하위 호환 ──

            public SkillRecipeBuilder OnCast(SkillAction action) => On(SkillEvent.Cast).Do(action);
            public SkillRecipeBuilder OnCast(ActionTemplate t) => On(SkillEvent.Cast).Do(t);
            public SkillRecipeBuilder AtHit(SkillAction action) => On(SkillEvent.Execute1).Do(action);
            public SkillRecipeBuilder OnTick(SkillAction action) => On(SkillEvent.Tick).Do(action);
            public SkillRecipeBuilder OnTick(ActionTemplate t) => On(SkillEvent.Tick).Do(t);
            public SkillRecipeBuilder OnComplete(SkillAction action) => On(SkillEvent.Complete).Do(action);
            public SkillRecipeBuilder OnComplete(ActionTemplate t) => On(SkillEvent.Complete).Do(t);

            // ── 슬롯 할당 ──

            private sbyte AllocateSlot(ValueRef vref)
            {
                if (vref.IsNone) return -1;

                // 같은 Spec 참조가 이미 있으면 재사용
                for (int i = 0; i < _params.Count; i++)
                {
                    var existing = _params[i];
                    if (existing.SpecIndex == vref.SpecIndex && existing.ValueType == vref.Type
                        && System.Math.Abs(existing.Fallback - vref.Value) < 0.001f)
                        return (sbyte)i;
                }

                _params.Add(new ParamSlot(vref.SpecIndex, vref.Type, vref.Value));
                return (sbyte)(_params.Count - 1);
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
                    SuppressAutoSound = _suppressAutoSound,
                    Tags = _explicitTags | InferTags(),
                    ParamSlots = _params.Count > 0 ? _params.ToArray() : null,
                    Actions = _actions.Count > 0 ? _actions.ToArray() : null,
                };
            }

            private TraitTag InferTags()
            {
                TraitTag inferred = TraitTag.None;
                for (int i = 0; i < _actions.Count; i++)
                {
                    switch (_actions[i].Effect)
                    {
                        case SkillEffectType.Damage: inferred |= TraitTag.Damage; break;
                        case SkillEffectType.Heal: inferred |= TraitTag.Heal; break;
                        case SkillEffectType.ApplyCC: inferred |= TraitTag.CC; break;
                        case SkillEffectType.Shield: inferred |= TraitTag.Shield; break;
                        case SkillEffectType.Knockback: inferred |= TraitTag.Knockback; break;
                        case SkillEffectType.SpawnProjectile:
                        case SkillEffectType.SpawnLinearProjectile: inferred |= TraitTag.Projectile; break;
                        case SkillEffectType.MultiHit: inferred |= TraitTag.MultiHit; break;
                        case SkillEffectType.ApplyBuff: inferred |= TraitTag.Buff; break;
                        case SkillEffectType.ApplyDebuff: inferred |= TraitTag.Debuff; break;
                        case SkillEffectType.RemoveDebuffs: inferred |= TraitTag.RemoveDebuffs; break;
                    }
                    if (_actions[i].AreaShape != SkillAreaShape.None) inferred |= TraitTag.AoE;
                }
                if (_execType == SkillExecutionType.Channeling) inferred |= TraitTag.Channeling;
                return inferred;
            }

            // ══════════════════════════════
            // 액션 팩토리 — ActionTemplate 반환 (값 참조 포함)
            // ══════════════════════════════

            public static ActionTemplate Damage(ValueRef power = default,
                SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget,
                SkillAreaShape area = SkillAreaShape.None, byte range = 0,
                bool excludePrimary = false, byte rectDepth = 0)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.Damage, TargetFilter = filter, AreaShape = area, AreaRange = range, ExcludePrimary = excludePrimary, RectDepth = rectDepth },
                    primary: power.IsNone ? ValueRef.Power : power);

            public static ActionTemplate Heal(ValueRef power = default,
                SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget,
                SkillAreaShape area = SkillAreaShape.None, byte range = 0)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.Heal, TargetFilter = filter, AreaShape = area, AreaRange = range },
                    primary: power.IsNone ? ValueRef.Power : power);

            public static ActionTemplate CC(CrowdControlType ccType, ValueRef duration = default)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.ApplyCC, TargetFilter = SkillTargetFilter.PrimaryTarget, CCType = ccType },
                    secondary: duration);

            public static ActionTemplate AreaCC(CrowdControlType ccType, SkillAreaShape area, byte range, ValueRef duration = default)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.ApplyCC, TargetFilter = SkillTargetFilter.EnemiesInArea, AreaShape = area, AreaRange = range, CCType = ccType },
                    secondary: duration);

            public static ActionTemplate Buff(StatModType stat, ValueRef value, ValueRef duration, SkillTargetFilter filter = SkillTargetFilter.Self, bool scaleByHitCount = false)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.ApplyBuff, TargetFilter = filter, BuffStat = stat, ScaleByHitCount = scaleByHitCount },
                    primary: value, secondary: duration);

            public static ActionTemplate Debuff(StatusEffectType statusEffect, ValueRef value, ValueRef duration,
                SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget, SkillAreaShape area = SkillAreaShape.None, byte range = 0)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.ApplyDebuff, TargetFilter = filter, AreaShape = area, AreaRange = range, StatusEffect = statusEffect },
                    primary: value, secondary: duration);

            public static ActionTemplate Debuff(StatModType stat, ValueRef value, ValueRef duration,
                SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget, SkillAreaShape area = SkillAreaShape.None, byte range = 0)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.ApplyDebuff, TargetFilter = filter, AreaShape = area, AreaRange = range, BuffStat = stat },
                    primary: value, secondary: duration);

            public static ActionTemplate Shield(ValueRef percent, ValueRef duration, SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.Shield, TargetFilter = filter },
                    primary: percent, secondary: duration);

            public static ActionTemplate MultiHit(ValueRef power = default, byte hitCount = 3)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.MultiHit, TargetFilter = SkillTargetFilter.PrimaryTarget, RepeatCount = hitCount },
                    primary: power.IsNone ? ValueRef.Power : power);

            public static ActionTemplate SpawnProjectile(ValueRef power = default, sbyte vfxIndex = -1, short travelFrames = 30,
                bool useBezier = false, sbyte arrivalVfxIndex = -1)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.SpawnProjectile, TargetFilter = SkillTargetFilter.PrimaryTarget,
                        VfxIndex = vfxIndex, RepeatIntervalFrames = travelFrames, UseBezier = useBezier, ArrivalVfxIndex = arrivalVfxIndex },
                    primary: power.IsNone ? ValueRef.Power : power);

            public static ActionTemplate SpawnLinearProjectile(ValueRef power = default, byte length = 4, short moveInterval = 3, byte width = 1)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.SpawnLinearProjectile, TargetFilter = SkillTargetFilter.PrimaryTarget,
                        AreaRange = length, RepeatIntervalFrames = moveInterval, RepeatCount = width },
                    primary: power.IsNone ? ValueRef.Power : power);

            public static ActionTemplate ModifyStat(ValueRef value = default, SkillTargetFilter filter = SkillTargetFilter.Self)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.ModifyStat, TargetFilter = filter },
                    primary: value);

            public static ActionTemplate DamageKnockback(SkillAreaShape area, byte range, ValueRef power = default)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.DamageKnockbackInArea, TargetFilter = SkillTargetFilter.EnemiesInArea, AreaShape = area, AreaRange = range },
                    primary: power.IsNone ? ValueRef.Power : power);

            public static ActionTemplate SequentialLine(ValueRef power, byte lineLength, short intervalMs, byte repeatCount)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.SequentialLineDamage, TargetFilter = SkillTargetFilter.EnemiesInArea,
                        AreaRange = lineLength, RepeatCount = repeatCount, RepeatIntervalMs = intervalMs },
                    primary: power);

            public static ActionTemplate DamageWithDecay(ValueRef power, ValueRef decayPercent)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.Damage },
                    primary: power.IsNone ? ValueRef.Power : power, decay: decayPercent);

            public static ActionTemplate ApplyStatusEffect(StatusEffectType statusType, SkillTargetFilter filter,
                ValueRef duration = default, ValueRef value = default)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.ApplyStatusEffect, TargetFilter = filter, StatusEffect = statusType },
                    primary: value, secondary: duration);

            // ══════════════════════════════
            // 액션 팩토리 — SkillAction 반환 (값 참조 없음)
            // ══════════════════════════════

            public static SkillAction Vfx(sbyte vfxIndex, SkillVfxPlacement at, short vfxDirOffset = 0)
                => new SkillAction { Effect = SkillEffectType.None, VfxIndex = vfxIndex, VfxAt = at, VfxDirOffset = vfxDirOffset };

            /// <summary>추적된 VFX 제거 (FIFO). 해당 vfxIndex로 스폰된 가장 오래된 VFX를 숨김.</summary>
            public static SkillAction RemoveVfx(sbyte vfxIndex)
                => new SkillAction { Effect = SkillEffectType.RemoveVfx, VfxIndex = vfxIndex };

            /// <summary>사운드 재생</summary>
            public static SkillAction Sound(SoundFX sfx)
                => new SkillAction { Effect = SkillEffectType.PlaySound, SoundId = (int)sfx };

            public static SkillAction AreaVfx(SkillVfxPlacement at, byte range, sbyte vfxIndex = -1,
                SkillActionCondition condition = SkillActionCondition.Always, bool isBox = false)
                => new SkillAction { Effect = SkillEffectType.None, VfxIndex = vfxIndex, VfxAt = at, AreaRange = range, Condition = condition, IsBoxArea = isBox };

            public static SkillAction TileEffect(SkillAreaShape shape = SkillAreaShape.Circle, byte range = 1,
                SkillTargetFilter at = SkillTargetFilter.Self, bool isBox = false, byte rectDepth = 0)
                => new SkillAction { Effect = SkillEffectType.TileEffect, AreaShape = shape, AreaRange = range, TargetFilter = at, IsBoxArea = isBox, RectDepth = rectDepth };

            public static SkillAction Teleport(byte distance = 0)
                => new SkillAction { Effect = SkillEffectType.Teleport, TeleportDistance = distance };

            public static SkillAction TeleportReturn()
                => new SkillAction { Effect = SkillEffectType.TeleportReturn };

            public static SkillAction Dash()
                => new SkillAction { Effect = SkillEffectType.Dash };

            public static SkillAction DashReturn()
                => new SkillAction { Effect = SkillEffectType.DashReturn };

            /// <summary>대쉬 단일 페이즈. Rush/Overshoot/Return을 각 Execute에서 독립 호출.</summary>
            /// <param name="vfxIndex">DashSystem이 페이즈 시작 시 스폰할 VFX 인덱스 (-1이면 없음)</param>
            /// <param name="vfxDirOffset">VFX 방향 오프셋 (0.1단위, 18=1.8f)</param>
            public static ActionTemplate DashForward(DashPhase phase,
                byte distance = 0,
                short durationMs = 0,
                MoveEaseType ease = MoveEaseType.None,
                ValueRef power = default,
                CrowdControlType cc = default,
                ValueRef ccDuration = default,
                sbyte vfxIndex = -1,
                short vfxDirOffset = 0)
                => new ActionTemplate(
                    new SkillAction
                    {
                        Effect = SkillEffectType.DashForward,
                        DashPhaseType = phase,
                        AreaRange = distance,
                        DashDurationMs = durationMs,
                        DashEaseType = ease,
                        CCType = cc,
                        VfxIndex = vfxIndex,
                        VfxDirOffset = vfxDirOffset,
                    },
                    primary: power, secondary: ccDuration);

            public static SkillAction Retarget(SkillTargetFilter filter, bool excludeHit = false)
                => new SkillAction { Effect = SkillEffectType.Retarget, TargetFilter = filter, ExcludeHit = excludeHit };

            public static SkillAction Knockback(byte fixedDistance = 0)
                => new SkillAction { Effect = SkillEffectType.Knockback, TargetFilter = SkillTargetFilter.PrimaryTarget, KnockbackDistance = fixedDistance };

            public static ActionTemplate Knockback(ValueRef distance)
                => new ActionTemplate(
                    new SkillAction { Effect = SkillEffectType.Knockback, TargetFilter = SkillTargetFilter.PrimaryTarget },
                    secondary: distance);

            public static SkillAction RemoveDebuffs(SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget, SkillAreaShape area = SkillAreaShape.None, byte range = 0)
                => new SkillAction { Effect = SkillEffectType.RemoveDebuffs, TargetFilter = filter, AreaShape = area, AreaRange = range };

            public static SkillAction AddMarker(SkillMarkerType marker)
                => new SkillAction { Effect = SkillEffectType.AddMarker, TargetFilter = SkillTargetFilter.PrimaryTarget, MarkerType = (byte)marker };

            public static SkillAction WithRepeat(SkillAction action, byte count = 0, short intervalFrames = 0, short intervalMs = 0, bool dynamicFromClip = false)
            {
                action.RepeatCount = count;
                action.RepeatIntervalFrames = intervalFrames;
                action.RepeatIntervalMs = intervalMs;
                action.DynamicFromClip = dynamicFromClip;
                return action;
            }

            public static ActionTemplate WithRepeat(ActionTemplate template, byte count = 0, short intervalFrames = 0, short intervalMs = 0, bool dynamicFromClip = false)
            {
                var action = template.Action;
                action.RepeatCount = count;
                action.RepeatIntervalFrames = intervalFrames;
                action.RepeatIntervalMs = intervalMs;
                action.DynamicFromClip = dynamicFromClip;
                return new ActionTemplate(action, template.PrimaryValue, template.SecondaryValue, template.DecayValue);
            }
        }
    }
}
