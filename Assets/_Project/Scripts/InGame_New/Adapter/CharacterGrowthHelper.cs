using CookApps.AutoBattler;
using UnityEngine;
using CharacterInfo = CookApps.AutoBattler.CharacterInfo;
using MonsterInfo = CookApps.AutoBattler.MonsterInfo;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 캐릭터 성장 요소(레벨/돌파/초월 + 엘피스 연구소) 계산 헬퍼.
    /// AutoChessSpecAdapter에서 스탯 보정 시 사용.
    /// </summary>
    public static class CharacterGrowthHelper
    {
        /// <summary>
        /// 레벨/돌파/초월 기반 스탯 보너스 비율 계산.
        /// 반환값을 (1 + result)로 곱하면 보정된 스탯.
        /// CharacterInfo: 유저 데이터에서 레벨 조회 + 돌파 + 초월.
        /// MonsterInfo: 레벨 1 고정 (외부 레벨 지정 시 오버로드 사용).
        /// </summary>
        public static float CalculateLevelBonusRate(ISpecCharacterInfo spec)
        {
            return CalculateLevelBonusRate(spec, -1);
        }

        /// <summary>
        /// 레벨을 외부에서 지정하는 오버로드.
        /// levelOverride > 0이면 해당 값 사용, 아니면 유저 데이터/기본값에서 결정.
        /// </summary>
        public static float CalculateLevelBonusRate(ISpecCharacterInfo spec, int levelOverride)
        {
            int level = 1;
            int transcendStar = 0;

            if (spec is CharacterInfo characterInfo)
            {
                var userData = ServerDataManager.Instance.Character.GetCharacter(characterInfo.id);
                level = levelOverride > 0 ? levelOverride : Mathf.Max(1, (int)(userData?.Level ?? 1));

                // 초월: (유저 초월레벨 - 초기 별) → 초월 스펙 조회
                int userTranscendLevel = (int)(userData?.TranscendLevel ?? 0);
                int currentStar = userTranscendLevel - characterInfo.init_star;
                var transcendData = SpecDataManager.Instance.GetCharacterTranscendenceData(
                    characterInfo.grade_type, currentStar);
                transcendStar = transcendData != null
                    ? transcendData.star - transcendData.init_star
                    : 0;
            }
            else if (spec is MonsterInfo)
            {
                level = levelOverride > 0 ? levelOverride : 1;
            }

            // 레벨 곱: (1 + inc_lv_rate × (level-1)) × (1 + inc_lv_bonus_rate × ⌊(level-1)/10⌋)
            float levelMult = (1f + spec.inc_lv_rate * (level - 1))
                            * (1f + spec.inc_lv_bonus_rate * Mathf.FloorToInt((level - 1) * 0.1f));

            // 돌파 곱 (CharacterInfo만)
            float breakthroughMult = 1f;
            if (spec is CharacterInfo ci)
            {
                int bt = level / 10;
                breakthroughMult += ci.inc_exceed * bt;
            }

            // 초월 곱 (CharacterInfo만)
            float transcendMult = 1f;
            if (spec is CharacterInfo ci2)
            {
                transcendMult += ci2.inc_trancendence * transcendStar;
            }

            return (levelMult * breakthroughMult * transcendMult) - 1f;
        }

        /// <summary>
        /// 엘피스 차원 연구소 고정 보너스 계산.
        /// CharacterInfo만 대상, 그 외 (0, 0, 0) 반환.
        /// </summary>
        public static (float ad, float def, float hp) CalculateElpisLabBonus(ISpecCharacterInfo spec)
        {
            if (spec is not CharacterInfo characterInfo)
                return (0f, 0f, 0f);

            var elpisModel = ServerDataManager.Instance.Elpis;
            var cachedLabs = elpisModel.CachedElpisDimensionLabs;
            if (cachedLabs == null || cachedLabs.Count == 0)
                return (0f, 0f, 0f);

            float ad = 0f;
            float def = 0f;
            float hp = 0f;

            foreach (var lab in cachedLabs)
            {
                switch (lab.upgrade_cover_type)
                {
                    case SynergyCoverType.KNIGHT_ALL:
                        switch (lab.core_research_type)
                        {
                            case CoreResearchType.KnightAttack:
                                ad += lab.effect_stat_value01;
                                break;
                            case CoreResearchType.KnightDefense:
                                def += lab.effect_stat_value01;
                                break;
                            case CoreResearchType.KnightHealth:
                                hp += lab.effect_stat_value01;
                                break;
                        }
                        break;

                    case SynergyCoverType.SYNERGY_ELEMENTAL:
                        if (IsElementMatch(lab.core_research_type, characterInfo.character_element_type))
                        {
                            def += lab.effect_stat_value01;
                            hp += lab.effect_stat_value02;
                        }
                        break;

                    case SynergyCoverType.SYNERGY_STELLA:
                        if (IsStellaMatch(lab.core_research_type, characterInfo.character_stella_type))
                        {
                            ad += lab.effect_stat_value01;
                            def += lab.effect_stat_value02;
                        }
                        break;
                }
            }

            return (ad, def, hp);
        }

        /// <summary>
        /// CoreResearchType과 캐릭터 속성(Element) 매칭 확인
        /// </summary>
        public static bool IsElementMatch(CoreResearchType coreType, SynergyType elementType)
        {
            switch (coreType)
            {
                case CoreResearchType.Fire:      return elementType == SynergyType.FIRE;
                case CoreResearchType.Wind:      return elementType == SynergyType.WIND;
                case CoreResearchType.Earth:     return elementType == SynergyType.EARTH;
                case CoreResearchType.Lightning: return elementType == SynergyType.LIGHTNING;
                case CoreResearchType.Water:     return elementType == SynergyType.WATER;
                default: return false;
            }
        }

        /// <summary>
        /// CoreResearchType과 캐릭터 성군(Stella) 매칭 확인.
        /// ARCANA, ECLIPSE 포함.
        /// </summary>
        public static bool IsStellaMatch(CoreResearchType coreType, SynergyType stellaType)
        {
            switch (coreType)
            {
                case CoreResearchType.Noblesse:      return stellaType == SynergyType.NOBLESSE;
                case CoreResearchType.Supernova:     return stellaType == SynergyType.SUPERNOVA;
                case CoreResearchType.Troubleshooter: return stellaType == SynergyType.TROUBLESHOOTER;
                case CoreResearchType.Arcana:        return stellaType == SynergyType.ARCANA;
                case CoreResearchType.Eclipse:       return stellaType == SynergyType.ECLIPSE;
                default: return false;
            }
        }
    }
}
