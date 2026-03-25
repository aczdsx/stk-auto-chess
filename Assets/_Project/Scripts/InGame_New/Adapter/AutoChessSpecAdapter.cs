using System.Collections.Generic;
using UnityEngine;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 기존 SpecDataManager 데이터를 시뮬레이션용 구조체로 변환하는 어댑터.
    /// CharacterInfo → ChampionSpec, ISpecSynergyData → SynergySpec.
    /// </summary>
    public static class AutoChessSpecAdapter
    {
        /// <summary>GameWorld에 모든 스펙 데이터 주입</summary>
        public static void InjectSpecs(GameWorld world)
        {
            var champions = BuildChampionSpecs(world.Config);
            world.SetChampionPool(champions, champions.Length);

            var synergies = BuildSynergySpecs();
            world.SetSynergySpecs(synergies, synergies.Length);

            // 아이템: 기존 시스템과 구조가 달라 별도 작업 필요
            world.SetItemSpecs(System.Array.Empty<ItemSpec>(), 0);

            Debug.Log($"[AutoChess] Specs injected: {champions.Length} champions, {synergies.Length} synergies");
        }

        // ── 챔피언 스펙 변환 ──

        private static ChampionSpec[] BuildChampionSpecs(GameConfig config)
        {
            var allChars = SpecDataManager.Instance.CharacterInfo.All;
            if (allChars == null || allChars.Count == 0)
            {
                Debug.LogWarning("[AutoChess] CharacterInfo.All is empty");
                return System.Array.Empty<ChampionSpec>();
            }

            var specs = new ChampionSpec[allChars.Count];
            for (int i = 0; i < allChars.Count; i++)
            {
                var c = allChars[i];
                int cost = GetCostFromGrade(config, c.grade_type);

                // 성장 요소 반영 (레벨/돌파/초월 + 엘피스 연구소)
                float bonusRate = CharacterGrowthHelper.CalculateLevelBonusRate(c);
                var (labAd, labDef, labHp) = CharacterGrowthHelper.CalculateElpisLabBonus(c);

                specs[i] = new ChampionSpec
                {
                    ChampionId = c.id,
                    Cost = (byte)cost,
                    Rarity = (byte)cost,
                    TraitFlags = BuildTraitFlags(c.character_element_type, c.character_stella_type),

                    // 기본 스탯 (성장 보정 적용)
                    BaseHP = (int)(c.stat_hp * (1f + bonusRate) + labHp),
                    BaseAttack = (int)(c.stat_atk * (1f + bonusRate) + labAd),
                    BaseDef = (int)(c.stat_def * (1f + bonusRate) + labDef),
                    BaseApReduce = ReduceToIntPercent(c.ap_reduce),
                    AttackSpeed = Mathf.Max(1, (int)(c.atk_speed * 100)),
                    AttackRange = c.atk_range > 0 ? c.atk_range : 1,
                    MoveSpeed = Mathf.Max(1, (int)(c.move_speed * 100)),
                    MaxMana = 100,       // CharacterInfo에 mana 없음 → 기본값
                    StartingMana = 0,
                    SkillId = GetPrimarySkillId(c),
                    PrefabId = c.prefab_id,

                    // 관통/크리 (float → 정수 퍼센트)
                    BaseAtkPierce = Mathf.Clamp((int)(c.stat_atk_pierce * 100), 0, 100),
                    BaseResPierce = Mathf.Clamp((int)(c.stat_res_pierce * 100), 0, 100),
                    BaseCritRate = Mathf.Max(0, (int)(c.crit_rate * 100)),
                    BaseCritPower = Mathf.Max(0, (int)(c.crit_power * 100)),

                    // 추가 스탯
                    BaseAdReduce = ReduceToIntPercent(c.ad_reduce),
                    BaseHealPower = (int)(c.heal_power * 100),
                    BaseImmuneType = (int)c.immune_type,
                    PositionType = (byte)c.character_position_type,
                    JobPassiveParam0 = GetJobPassiveParam0(c.character_position_type),
                    JobPassiveParam1 = GetJobPassiveParam1(c.character_position_type),

                    // 크기 (기본 1x1, 추후 스펙 데이터에 크기 필드 추가 시 매핑)
                    SizeW = 1,
                    SizeH = 1,

                    // 별 배율 (퍼센트: 180 = 1.8x)
                    Star2Multiplier = 180,
                    Star3Multiplier = 320,
                };
            }

            return specs;
        }

        /// <summary>
        /// ad_reduce/ap_reduce 전용: 스펙 float → 정수 퍼센트 변환.
        /// 값이 1 이상이면 이미 퍼센트 정수로 간주, 미만이면 ×100.
        /// 예: 29 → 29, 0.02 → 2
        /// 주의: crit_power(1.43), atk_speed(1.10) 등 배수 필드에는 사용 금지.
        /// </summary>
        public static int ReduceToIntPercent(float value)
        {
            return value >= 1f ? (int)value : (int)(value * 100f);
        }

        private static int BuildTraitFlags(SynergyType element, SynergyType stella)
        {
            int flags = 0;
            int e = (int)element;
            int s = (int)stella;
            // NONE(0)은 건너뜀
            if (e > 0 && e < 32) flags |= (1 << e);
            if (s > 0 && s < 32) flags |= (1 << s);
            return flags;
        }

        private static int GetPrimarySkillId(ISpecCharacterInfo c)
        {
            var ids = c.skill_ids;
            if (ids != null && ids.Length > 0 && ids[0] > 0)
                return ids[0];
            return 0;
        }

        private static int GetCostFromGrade(GameConfig config, GradeType grade)
        {
            int idx = (int)grade;
            if (idx >= 0 && idx < config.GradeCostMap.Length)
                return config.GradeCostMap[idx];
            return 1;
        }

        // ── 직업 패시브 파라미터 추출 ──

        /// <summary>직업 패시브 주 계수 (passive_rate → 정수 퍼센트 또는 프레임)</summary>
        private static int GetJobPassiveParam0(CharacterPositionType positionType)
        {
            var passiveList = SpecDataManager.Instance?.GetJobPassiveList(positionType);
            if (passiveList == null || passiveList.Count == 0) return 0;
            var data = passiveList[0][0]; // grade 0
            return (int)(data.passive_rate * 100); // float 확률 → 정수 퍼센트
        }

        /// <summary>직업 패시브 부 계수 (passive_rate_2 → 정수 퍼센트 또는 프레임)</summary>
        private static int GetJobPassiveParam1(CharacterPositionType positionType)
        {
            var passiveList = SpecDataManager.Instance?.GetJobPassiveList(positionType);
            if (passiveList == null || passiveList.Count == 0) return 0;
            var data = passiveList[0][0]; // grade 0
            return (int)(data.passive_rate_2 * 100);
        }

        // ── 시너지 스펙 변환 ──

        private static SynergySpec[] BuildSynergySpecs()
        {
            var specManager = SpecDataManager.Instance;
            if (specManager == null)
            {
                Debug.LogWarning("[AutoChess] SpecDataManager not available");
                return System.Array.Empty<SynergySpec>();
            }

            var result = new List<SynergySpec>();

            // SynergyType: NONE=0, NORMAL=1, FIRE=2, WIND=3, LIGHTNING=4,
            //              EARTH=5, WATER=6, NOBLESSE=7, TROUBLESHOOTER=8, SUPERNOVA=9
            for (int synergyTypeInt = 1; synergyTypeInt <= 9; synergyTypeInt++)
            {
                var synergyType = (SynergyType)synergyTypeInt;
                var synergyList = specManager.GetSpecSynergyList(synergyType);
                if (synergyList == null || synergyList.Count == 0) continue;

                // grade 순 정렬 (원본 리스트 보호)
                var sorted = new List<ISpecSynergyData>(synergyList);
                sorted.Sort((a, b) => a.grade.CompareTo(b.grade));

                // SUPERNOVA 등: 동일 grade에 여러 행이 있으면 효과를 합산하여 하나의 티어로 묶음
                // 기본 효과(첫 번째 grade 행)는 모든 상위 티어에도 누적 적용
                var tiers = BuildSynergyTiers(synergyType, sorted);

                // element(1-6) → Origin, stella(7-9) → Class
                var category = synergyTypeInt <= 6 ? TraitCategory.Origin : TraitCategory.Class;

                result.Add(new SynergySpec
                {
                    TraitId = synergyTypeInt,
                    Category = category,
                    Tiers = tiers,
                    HasBehavior = SynergyFactory.NeedsBehavior(synergyType),
                });
            }

            return result.ToArray();
        }

        /// <summary>
        /// 정렬된 시너지 데이터 → SynergyTier[].
        /// 동일 grade 행을 그룹화하여 효과를 하나의 티어로 합산.
        /// 기본 효과(가장 낮은 grade의 첫 행)는 모든 상위 티어에도 누적 적용.
        /// </summary>
        private static SynergyTier[] BuildSynergyTiers(SynergyType type, List<ISpecSynergyData> sorted)
        {
            // grade별 그룹화
            var gradeGroups = new List<(int grade, byte minCount, List<SynergyEffect> effects)>();
            SynergyEffect[] baseEffects = null; // 기본 효과 (첫 grade의 첫 행)

            int currentGrade = -1;
            byte currentMinCount = 0;
            List<SynergyEffect> currentEffects = null;

            int withinGradeIdx = 0;

            for (int i = 0; i < sorted.Count; i++)
            {
                var data = sorted[i];

                if (data.grade != currentGrade)
                {
                    // 새 grade 시작
                    if (currentEffects != null)
                        gradeGroups.Add((currentGrade, currentMinCount, currentEffects));

                    currentGrade = data.grade;
                    currentMinCount = (byte)data.min_int;
                    withinGradeIdx = 0;
                }
                else
                {
                    // 동일 grade — 인덱스 증가
                    withinGradeIdx++;
                }

                var effects = BuildEffects(type, data, i);

                if (withinGradeIdx == 0)
                    currentEffects = new List<SynergyEffect>(effects);
                else
                    currentEffects.AddRange(effects);

                // 첫 번째 행의 효과를 기본 효과로 저장
                if (i == 0 && effects.Length > 0)
                    baseEffects = effects;
            }

            if (currentEffects != null)
                gradeGroups.Add((currentGrade, currentMinCount, currentEffects));

            // 동일 grade에 여러 행이 있는 경우 (SUPERNOVA 등):
            // 첫 grade의 첫 행 = 기본 효과 (모든 티어 공통), 두번째 행 = 단계 효과
            // 첫 grade 그룹은 이미 두 행이 합산됨
            // 이후 grade 그룹(단일 행)에는 기본 효과를 prepend
            // 주의: 이후 grade에도 2행 이상이 있다면 기본효과가 중복될 수 있음
            bool hasMultiRowGrades = sorted.Count > gradeGroups.Count;
            if (hasMultiRowGrades && baseEffects != null)
            {
                for (int g = 1; g < gradeGroups.Count; g++)
                {
                    var group = gradeGroups[g];
                    var merged = new List<SynergyEffect>(baseEffects);
                    merged.AddRange(group.effects);
                    gradeGroups[g] = (group.grade, group.minCount, merged);
                }
            }

            // SynergyTier[] 생성
            var tiers = new SynergyTier[gradeGroups.Count];
            for (int g = 0; g < gradeGroups.Count; g++)
            {
                tiers[g] = new SynergyTier
                {
                    RequiredCount = gradeGroups[g].minCount,
                    Effects = gradeGroups[g].effects.ToArray(),
                };
            }

            return tiers;
        }

        // ── 시너지 효과 매핑 ──

        /// <summary>
        /// SynergyCoverType → SynergyTarget 매핑.
        /// SQUAD_ALL/KNIGHT_ALL: 스쿼드 전체에 적용
        /// SQUAD_STELLA: 해당 성군(Asterism) 특성 유닛에만 적용
        /// SYNERGY_ELEMENTAL/SYNERGY_STELLA: 레거시 시너지 시스템용 (여기서는 사용 안 함)
        /// </summary>
        private static SynergyTarget MapCoverType(SynergyCoverType cover)
        {
            switch (cover)
            {
                case SynergyCoverType.SQUAD_STELLA:
                    return SynergyTarget.TraitUnits;
                case SynergyCoverType.SQUAD_ALL:
                case SynergyCoverType.KNIGHT_ALL:
                    return SynergyTarget.AllAllies;
                default:
                    return SynergyTarget.AllAllies;
            }
        }

        /// <summary>
        /// SUPERNOVA 행 → SynergyEffect[].
        /// grade 내 첫 번째 행(withinGradeIndex==0): 기본 효과 (HP%, 공속%).
        /// grade 내 두 번째+ 행: 단계 효과 (공격력%, 흡혈).
        /// </summary>
        /// <summary>
        /// SUPERNOVA 행 → SynergyEffect[].
        /// isBaseRow=true (sorted 첫 행): 기본 효과 (HP%, 공속%) — 모든 티어에 누적.
        /// isBaseRow=false (나머지): 단계 효과 (공격력%, 흡혈).
        /// </summary>
        private static SynergyEffect[] BuildSupernovaRowEffects(ISpecSynergyData data, bool isBaseRow)
        {
            int v1 = data.effect_stat_value_1;
            int v2 = data.effect_stat_value_2;

            if (isBaseRow)
            {
                return new[]
                {
                    new SynergyEffect { Type = SynergyEffectType.BonusHPPercent, Target = SynergyTarget.PrepTarget, ValuePercent = v1 },
                    new SynergyEffect { Type = SynergyEffectType.BonusAttackSpeedPercent, Target = SynergyTarget.PrepTarget, ValuePercent = v2 },
                };
            }
            else
            {
                return new[]
                {
                    new SynergyEffect { Type = SynergyEffectType.BonusAttackPercent, Target = SynergyTarget.PrepTarget, ValuePercent = v1 },
                    new SynergyEffect { Type = SynergyEffectType.LifeSteal, Target = SynergyTarget.PrepTarget, Value = v2 },
                };
            }
        }

        /// <summary>
        /// SynergyType + 스펙 데이터 → SynergyEffect[].
        /// 원소: 스탯 매핑. Asterism: 빈 배열 (행동 클래스에서 처리).
        /// 스펙 테이블 변경 시 이 스위치만 수정.
        /// </summary>
        private static SynergyEffect[] BuildEffects(SynergyType type, ISpecSynergyData data, int globalIndex = 0)
        {
            var target = MapCoverType(data.synergy_cover_type);
            int v1 = data.effect_stat_value_1;
            int v2 = data.effect_stat_value_2;

            switch (type)
            {
                case SynergyType.FIRE:
                    // 공격력 {v1}% 상승, 관통력 {v2}% 상승
                    return new[]
                    {
                        new SynergyEffect { Type = SynergyEffectType.BonusAttackPercent, Target = target, ValuePercent = v1 },
                        new SynergyEffect { Type = SynergyEffectType.BonusPiercePercent, Target = target, ValuePercent = v2 },
                    };

                case SynergyType.WIND:
                    // 공격속도 {v1}%, 회피율 {v2}
                    return new[]
                    {
                        new SynergyEffect { Type = SynergyEffectType.BonusAttackSpeedPercent, Target = target, ValuePercent = v1 },
                        new SynergyEffect { Type = SynergyEffectType.DodgeChance, Target = target, Value = v2 },
                    };

                case SynergyType.LIGHTNING:
                    // 크리확률 {v1}, 크리파워 {v2}
                    return new[]
                    {
                        new SynergyEffect { Type = SynergyEffectType.BonusCritChance, Target = target, Value = v1 },
                        new SynergyEffect { Type = SynergyEffectType.BonusCritMultiplier, Target = target, Value = v2 },
                    };

                case SynergyType.EARTH:
                    // 방어력 {v1}%, 저항력(물리+마법) {v2}
                    return new[]
                    {
                        new SynergyEffect { Type = SynergyEffectType.BonusDefPercent, Target = target, ValuePercent = v1 },
                        new SynergyEffect { Type = SynergyEffectType.BonusAdReduce, Target = target, Value = v2 },
                        new SynergyEffect { Type = SynergyEffectType.BonusApReduce, Target = target, Value = v2 },
                    };

                case SynergyType.WATER:
                    // HP {v1}%, 이동속도 {v2}%
                    return new[]
                    {
                        new SynergyEffect { Type = SynergyEffectType.BonusHPPercent, Target = target, ValuePercent = v1 },
                        new SynergyEffect { Type = SynergyEffectType.BonusMoveSpeedPercent, Target = target, ValuePercent = v2 },
                    };

                case SynergyType.SUPERNOVA:
                    return BuildSupernovaRowEffects(data, globalIndex == 0);

                default:
                    // asterism 등: 스탯 매핑 없음 (행동 클래스에서 처리)
                    return System.Array.Empty<SynergyEffect>();
            }
        }
    }
}