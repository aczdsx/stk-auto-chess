using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 스킬 팩토리 + Recipe 레지스트리 + 스펙 어댑터 통합.
    /// SkillId → SimSkillBase 인스턴스 생성, Recipe 정의/조회, 스펙 변환을 모두 담당.
    ///
    /// partial class로 분리:
    /// - SkillFactory.cs — Core (딕셔너리, Create, Initialize, SkillRecipeBuilder)
    /// - SkillFactory.SpecAdapter.cs — 스펙 변환 (BuildParams, ExtractSkillHitTimes)
    /// - SkillFactory.Character.cs — 플레이어 스킬 Recipe 정의
    /// - SkillFactory.Monster.cs — 몬스터 스킬 Recipe 정의 + Preset
    /// </summary>
    public static partial class SkillFactory
    {
        // ── 인스턴스 생성 ──
        private static readonly Dictionary<int, System.Func<SimSkillBase>> _registry = new();
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

        public static void Register(int skillId, System.Func<SimSkillBase> creator)
        {
            _registry[skillId] = creator;
        }

        public static SimSkillBase Create(int skillId)
        {
            if (_registry.TryGetValue(skillId, out var creator))
                return creator();
            return null;
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

        private static bool TryGetRecipe(int skillGroupId, out SkillRecipe recipe)
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
                if (_registry.ContainsKey(id)) continue;

                // 개별 Recipe로 등록
                if (TryGetRecipe(id, out var recipe))
                {
                    var captured = recipe;
                    Register(id, () =>
                    {
                        var skill = new SimSkillGeneric();
                        skill.SetRecipe(captured);
                        return skill;
                    });
                }
            }
        }

        private static void RegisterCustomSkills()
        {
            // ── 커스텀 실행 로직이 필요한 스킬 (전용 클래스 유지) ──
            // Execute/OnChannelTick 로직이 복잡하여 SimSkillGeneric으로 대체 불가.
            Register(217433302, () => new SimSkillMinoProjectile());  // 미노: 순차 미사일 + 개별 도착 타이머
            Register(217363204, () => new SimSkillVeinBounce());      // 베인: 바운스 투사체 + 히트 추적
            Register(217413301, () => new SimSkillTetoraKnockback()); // 테토라: 넉백 + 벽 충돌 + 착지 AoE
            Register(217563405, () => new SimSkillMarieAssassin());   // 마리에: 텔레포트 + 순차 히트
            Register(217653505, () => new SimSkillEnkiWaveHeal());    // 엔키: 보드 스윕 투사체
            Register(217333202, () => new SimSkillAprilBarrage());    // 에이프릴: 확장 콘 + 거리별 배율
            Register(217613501, () => new SimSkillOdetteStrike());    // 오데트: 2단계 텔레포트 + 다른 범위
            Register(217523403, () => new SimSkillAdriaExpand());     // 아드리아: 3단계 확장 + 비트마스크
            Register(217663506, () => new SimSkillShirayukiAssassin()); // 시라유키: 순차 텔레포트 암살
            Register(217263103, () => new SimSkillRukidaFoxfire());   // 루키다: 마커 카운트 기반 동적 버프
            Register(217353203, () => new SimSkillRakiyuDebuff());    // 라키유: 투사체 도착 후 범위 디버프
        }

        /// <summary>팩토리 등록 해제 (테스트/재시작용)</summary>
        public static void Clear()
        {
            _registry.Clear();
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

        // ══════════════════════════════
        // Recipe Builder (inner struct)
        // ══════════════════════════════

        /// <summary>
        /// SkillRecipe를 간결하게 선언하기 위한 Builder.
        /// new SkillAction { ... } 반복을 제거하고 체이닝 API 제공.
        /// </summary>
        internal struct SkillRecipeBuilder
        {
            private readonly Dictionary<int, SkillRecipe> _target;
            private readonly int _skillId;
            private SkillExecutionType _execType;
            private SkillTargetType _targetRule;
            private bool _hasProjectile;
            private readonly List<ParamSlot> _params;
            private readonly List<SkillAction> _actions;
            private TraitTag _explicitTags;

            public SkillRecipeBuilder(Dictionary<int, SkillRecipe> target, int skillId,
                SkillExecutionType execType, SkillTargetType targetRule)
            {
                _target = target;
                _skillId = skillId;
                _execType = execType;
                _targetRule = targetRule;
                _hasProjectile = false;
                _explicitTags = TraitTag.None;
                _params = new List<ParamSlot>(4);
                _actions = new List<SkillAction>(6);
            }

            // ── 기본 설정 ──

            public SkillRecipeBuilder Projectile()
            {
                _hasProjectile = true;
                return this;
            }

            /// <summary>기능 태그 수동 부여 (자동 추론 + OR 합산)</summary>
            public SkillRecipeBuilder WithTags(TraitTag tags)
            {
                _explicitTags |= tags;
                return this;
            }

            /// <summary>Preset 함수 적용 (파라미터 없는 고정 패턴)</summary>
            public SkillRecipeBuilder Apply(System.Func<SkillRecipeBuilder, SkillRecipeBuilder> preset)
                => preset(this);

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

            public SkillRecipeBuilder OnKnockbackWall(SkillAction action)
            {
                action.Trigger = SkillTriggerType.OnKnockbackWall;
                _actions.Add(action);
                return this;
            }

            public SkillRecipeBuilder OnProjectileArrive(SkillAction action)
            {
                action.Trigger = SkillTriggerType.OnProjectileArrive;
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
            // 액션 팩토리 (static) — 체이닝에서 사용
            // ══════════════════════════════

            public static SkillAction Damage(sbyte paramIndex = -1,
                SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget,
                SkillAreaShape area = SkillAreaShape.None, byte range = 0,
                bool excludePrimary = false, byte rectDepth = 0)
                => new SkillAction { Effect = SkillEffectType.Damage, TargetFilter = filter, AreaShape = area, AreaRange = range, ParamIndex = paramIndex, ExcludePrimary = excludePrimary, RectDepth = rectDepth };

            public static SkillAction Heal(sbyte paramIndex = -1,
                SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget,
                SkillAreaShape area = SkillAreaShape.None, byte range = 0)
                => new SkillAction { Effect = SkillEffectType.Heal, TargetFilter = filter, AreaShape = area, AreaRange = range, ParamIndex = paramIndex };

            public static SkillAction CC(CrowdControlType ccType, sbyte durationParamIndex = -2)
                => new SkillAction { Effect = SkillEffectType.ApplyCC, TargetFilter = SkillTargetFilter.PrimaryTarget, CCType = ccType, SecondaryParamIndex = durationParamIndex };

            public static SkillAction AreaCC(CrowdControlType ccType, SkillAreaShape area, byte range, sbyte durationParamIndex = -2)
                => new SkillAction { Effect = SkillEffectType.ApplyCC, TargetFilter = SkillTargetFilter.EnemiesInArea, AreaShape = area, AreaRange = range, CCType = ccType, SecondaryParamIndex = durationParamIndex };

            public static SkillAction Knockback(sbyte distParamIndex = -1, byte fixedDistance = 0)
                => new SkillAction { Effect = SkillEffectType.Knockback, TargetFilter = SkillTargetFilter.PrimaryTarget, SecondaryParamIndex = distParamIndex, KnockbackDistance = fixedDistance };

            public static SkillAction Buff(StatModType stat, sbyte valueParamIndex, sbyte durationParamIndex, SkillTargetFilter filter = SkillTargetFilter.Self)
                => new SkillAction { Effect = SkillEffectType.ApplyBuff, TargetFilter = filter, BuffStat = stat, ParamIndex = valueParamIndex, SecondaryParamIndex = durationParamIndex };

            public static SkillAction Debuff(StatusEffectType statusEffect, sbyte valueParamIndex, sbyte durationParamIndex,
                SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget, SkillAreaShape area = SkillAreaShape.None, byte range = 0)
                => new SkillAction { Effect = SkillEffectType.ApplyDebuff, TargetFilter = filter, AreaShape = area, AreaRange = range, StatusEffect = statusEffect, ParamIndex = valueParamIndex, SecondaryParamIndex = durationParamIndex };

            public static SkillAction Shield(sbyte percentParamIndex, sbyte durationParamIndex, SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget)
                => new SkillAction { Effect = SkillEffectType.Shield, TargetFilter = filter, ParamIndex = percentParamIndex, SecondaryParamIndex = durationParamIndex };

            public static SkillAction RemoveDebuffs(SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget, SkillAreaShape area = SkillAreaShape.None, byte range = 0)
                => new SkillAction { Effect = SkillEffectType.RemoveDebuffs, TargetFilter = filter, AreaShape = area, AreaRange = range };

            public static SkillAction AddMarker(SkillMarkerType marker)
                => new SkillAction { Effect = SkillEffectType.AddMarker, TargetFilter = SkillTargetFilter.PrimaryTarget, MarkerType = (byte)marker };

            public static SkillAction MultiHit(sbyte paramIndex = -1, byte hitCount = 3)
                => new SkillAction { Effect = SkillEffectType.MultiHit, TargetFilter = SkillTargetFilter.PrimaryTarget, ParamIndex = paramIndex, RepeatCount = hitCount };

            public static SkillAction SpawnProjectile(sbyte paramIndex = -1, sbyte vfxIndex = -1, short travelFrames = 30)
                => new SkillAction { Effect = SkillEffectType.SpawnProjectile, TargetFilter = SkillTargetFilter.PrimaryTarget, ParamIndex = paramIndex, VfxIndex = vfxIndex, RepeatIntervalFrames = travelFrames };

            public static SkillAction SpawnLinearProjectile(sbyte paramIndex = -1, byte length = 4, short moveInterval = 3, byte width = 1)
                => new SkillAction { Effect = SkillEffectType.SpawnLinearProjectile, TargetFilter = SkillTargetFilter.PrimaryTarget, ParamIndex = paramIndex, AreaRange = length, RepeatIntervalFrames = moveInterval, RepeatCount = width };

            public static SkillAction ModifyStat(SkillTargetFilter filter = SkillTargetFilter.Self, sbyte paramIndex = -1)
                => new SkillAction { Effect = SkillEffectType.ModifyStat, TargetFilter = filter, ParamIndex = paramIndex };

            public static SkillAction DamageKnockback(SkillAreaShape area, byte range, sbyte paramIndex = -1)
                => new SkillAction { Effect = SkillEffectType.DamageKnockbackInArea, TargetFilter = SkillTargetFilter.EnemiesInArea, AreaShape = area, AreaRange = range, ParamIndex = paramIndex };

            public static SkillAction SequentialLine(sbyte paramIndex, byte lineLength, short intervalMs, byte repeatCount)
                => new SkillAction { Effect = SkillEffectType.SequentialLineDamage, TargetFilter = SkillTargetFilter.EnemiesInArea, AreaRange = lineLength, ParamIndex = paramIndex, RepeatCount = repeatCount, RepeatIntervalMs = intervalMs };

            public static SkillAction Vfx(sbyte vfxIndex, SkillVfxPlacement at)
                => new SkillAction { Effect = SkillEffectType.None, VfxIndex = vfxIndex, VfxAt = at };

            public static SkillAction AreaVfx(SkillVfxPlacement at, byte range, sbyte vfxIndex = -1, SkillActionCondition condition = SkillActionCondition.Always)
                => new SkillAction { Effect = SkillEffectType.None, VfxIndex = vfxIndex, VfxAt = at, AreaRange = range, Condition = condition };

            public static SkillAction WithRepeat(SkillAction action, byte count = 0, short intervalFrames = 0, short intervalMs = 0, bool dynamicFromClip = false)
            {
                action.RepeatCount = count;
                action.RepeatIntervalFrames = intervalFrames;
                action.RepeatIntervalMs = intervalMs;
                action.DynamicFromClip = dynamicFromClip;
                return action;
            }

            // ── 체이닝 팩토리 ──

            public static SkillAction Teleport(byte distance = 0)
                => new SkillAction { Effect = SkillEffectType.Teleport, TeleportDistance = distance };

            public static SkillAction Retarget(SkillTargetFilter filter, bool excludeHit = false)
                => new SkillAction { Effect = SkillEffectType.Retarget, TargetFilter = filter, ExcludeHit = excludeHit };

            public static SkillAction ApplyStatusEffect(StatusEffectType statusType, SkillTargetFilter filter,
                sbyte durationParamIndex = -1, sbyte valueParamIndex = -1)
                => new SkillAction { Effect = SkillEffectType.ApplyStatusEffect, TargetFilter = filter,
                    StatusEffect = statusType, SecondaryParamIndex = durationParamIndex, ParamIndex = valueParamIndex };

            public static SkillAction DamageWithDecay(sbyte paramIndex = -1, sbyte decayParamIndex = -1)
                => new SkillAction { Effect = SkillEffectType.Damage, ParamIndex = paramIndex, DecayParamIndex = decayParamIndex };

            public static SkillAction BuffScaled(StatModType stat, sbyte valueIdx, sbyte durIdx, bool scaleByHitCount = false)
                => new SkillAction { Effect = SkillEffectType.ApplyBuff, BuffStat = stat, TargetFilter = SkillTargetFilter.Self,
                    ParamIndex = valueIdx, SecondaryParamIndex = durIdx, ScaleByHitCount = scaleByHitCount };
        }
    }
}
