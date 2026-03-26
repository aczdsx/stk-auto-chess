using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 스킬 팩토리 + Recipe 레지스트리 통합.
    /// SkillId → SimSkillBase 인스턴스 생성, Recipe 정의 및 조회를 모두 담당.
    ///
    /// partial class로 분리:
    /// - SkillFactory.cs — Core (딕셔너리, Create, Initialize, Builder)
    /// - SkillFactory.Archetypes.cs — 아키타입 Recipe 정의
    /// - SkillFactory.Character.cs — 플레이어 스킬 Recipe 정의
    /// - SkillFactory.Monster.cs — 몬스터 스킬 Recipe 정의
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
        private static readonly Dictionary<SimSkillArchetype, SkillRecipe> _archetypeRecipes = new();

        static SkillFactory()
        {
            RegisterArchetypeRecipes();
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

        private static bool TryGetByArchetype(SimSkillArchetype archetype, out SkillRecipe recipe)
            => _archetypeRecipes.TryGetValue(archetype, out recipe);

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
                var archetype = SkillSpecAdapter.ClassifySkill(spec);

                // 같은 skill_group_id로 이미 등록된 경우 스킵 (성급별 중복 방지)
                if (_paramsCache.ContainsKey(id)) continue;

                var specList = specManager.GetSkillDataList(id);
                var skillParams = SkillSpecAdapter.BuildParams(spec, specList, tickRate);

                // Recipe의 TargetRule로 TargetType/FaceTarget 동기화 (스킬ID Recipe > 아키타입 Recipe)
                SkillTargetType resolvedTarget = skillParams.TargetType;
                if (TryGetRecipe(id, out var idRecipe))
                    resolvedTarget = idRecipe.TargetRule;
                else if (TryGetByArchetype(archetype, out var archRecipe))
                    resolvedTarget = archRecipe.TargetRule;
                skillParams.TargetType = resolvedTarget;
                skillParams.FaceTarget = resolvedTarget != SkillTargetType.Self
                    && resolvedTarget != SkillTargetType.LowestHPAlly;

                _paramsCache[id] = skillParams;
                if (specList != null)
                    _specListCache[id] = specList;

                // 커스텀 스킬이 이미 등록되어 있으면 스킵
                if (_registry.ContainsKey(id)) continue;

                // Custom 아키타입인데 아직 미등록이면 스킵
                if (archetype == SimSkillArchetype.Custom) continue;

                // Recipe가 있으면 SimSkillGeneric으로, 없으면 기존 아키타입 클래스로
                if (TryGetByArchetype(archetype, out var archetypeRecipe))
                {
                    var capturedRecipe = archetypeRecipe;
                    Register(id, () =>
                    {
                        var skill = new SimSkillGeneric();
                        skill.SetRecipe(capturedRecipe);
                        return skill;
                    });
                }
                else
                {
                    Register(id, () => SkillSpecAdapter.CreateFromArchetype(archetype));
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

            // ── Recipe 기반 스킬 (SimSkillGeneric으로 완전 대체) ──
            RegisterRecipeSkills();
        }

        /// <summary>
        /// Recipe가 등록된 스킬 중, 커스텀 클래스 미등록인 것만
        /// SimSkillGeneric으로 등록.
        /// </summary>
        private static void RegisterRecipeSkills()
        {
            int[] recipeSkillIds = {
                215532401, // 필리아: DelayedApply, Damage + 3단계 VFX + 마커
                217433303, // 하티: DelayedApply, Damage + Knockback + 3단계 VFX
                215252102, // 유니: DelayedApply, 최저HP 3명 Heal + RemoveDebuffs
                215422301, // 멘샤: DelayedApply, 같은 행 아군 Shield
                217323201, // 미사: DelayedApply, 최고공격력 적 Stun + 마커
                217553404, // 클레이: Channeling, Zone 힐+데미지+디버프
                215642501, // 엘리스: Channeling, 2단계 Diamond AoE
                230101005, // 몬스터 SingleProjectile
                230202004, // 몬스터 SingleProjectile
                217513401, // 아트레시아: 3칸 폭 직선 관통 투사체
                215322201, // 메이: Plus AoE + 넉백 + 방어 버프
                250108001, // 보스탱커: 전방 10칸 순차 타격
            };

            for (int i = 0; i < recipeSkillIds.Length; i++)
            {
                int id = recipeSkillIds[i];
                if (_registry.ContainsKey(id)) continue;
                if (!TryGetRecipe(id, out var recipe)) continue;

                var capturedRecipe = recipe;
                Register(id, () =>
                {
                    var skill = new SimSkillGeneric();
                    skill.SetRecipe(capturedRecipe);
                    return skill;
                });
            }
        }

        /// <summary>팩토리 등록 해제 (테스트/재시작용)</summary>
        public static void Clear()
        {
            _registry.Clear();
            _paramsCache.Clear();
            _specListCache.Clear();
            _initialized = false;
            // _recipes, _archetypeRecipes는 static constructor에서 1회 초기화되는 불변 데이터 — Clear 대상 아님
        }

        // ══════════════════════════════
        // Recipe Builder 헬퍼
        // ══════════════════════════════

        /// <summary>스킬 Recipe Builder 시작. Register()로 _recipes에 자동 등록.</summary>
        private static SkillRecipeBuilder Skill(int skillId, SkillExecutionType exec, SkillTargetType target)
            => new SkillRecipeBuilder(_recipes, skillId, exec, target);

        /// <summary>아키타입 Recipe Builder. Build()로 SkillRecipe 반환.</summary>
        private static SkillRecipeBuilder ArchetypeBuilder(SkillExecutionType exec, SkillTargetType target)
            => new SkillRecipeBuilder(null, 0, exec, target);

        /// <summary>아키타입 Recipe 등록</summary>
        private static void DefineArchetype(SimSkillArchetype archetype, SkillRecipe recipe)
            => _archetypeRecipes[archetype] = recipe;

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
}
