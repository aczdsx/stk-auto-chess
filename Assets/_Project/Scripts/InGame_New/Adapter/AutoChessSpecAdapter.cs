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

                specs[i] = new ChampionSpec
                {
                    ChampionId = c.id,
                    Cost = (byte)cost,
                    Rarity = (byte)cost,
                    TraitFlags = BuildTraitFlags(c.character_element_type, c.character_stella_type),

                    // 기본 스탯
                    BaseHP = c.stat_hp,
                    BaseAttack = c.stat_atk,
                    BaseArmor = c.stat_def,
                    BaseMagicResist = (int)c.ap_reduce,
                    AttackSpeed = Mathf.Max(1, (int)(c.atk_speed * 100)),  // float → 정수 (100 = 1.0)
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
                    BaseAdReduce = (int)(c.ad_reduce * 100),
                    BaseHealPower = (int)(c.heal_power * 100),
                    BaseImmuneType = (int)c.immune_type,

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

                var tiers = new SynergyTier[sorted.Count];
                for (int i = 0; i < sorted.Count; i++)
                {
                    var data = sorted[i];
                    tiers[i] = new SynergyTier
                    {
                        RequiredCount = (byte)data.min_int,
                        // TODO: 시너지 효과 매핑 (기존 EffectCode 기반 → 데이터 기반 전환 필요)
                        Effects = System.Array.Empty<SynergyEffect>(),
                    };
                }

                // element(1-6) → Origin, stella(7-9) → Class
                var category = synergyTypeInt <= 6 ? TraitCategory.Origin : TraitCategory.Class;

                result.Add(new SynergySpec
                {
                    TraitId = synergyTypeInt,
                    Category = category,
                    Tiers = tiers,
                });
            }

            return result.ToArray();
        }
    }
}