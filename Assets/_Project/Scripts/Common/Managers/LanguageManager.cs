using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class LanguageManager : Singleton<LanguageManager>
    {
        public string GetLanguageText(string tokenKey)
        {
            return SpecDataManager.Instance.GetLanguageText(tokenKey);
        }

        public string GetSynergyText(ElementType type)
        {
            switch (type)
            {
                case ElementType.FIRE:
                    return "불";
                case ElementType.WATER:
                    return "물";
                case ElementType.EARTH:
                    return "대지";
                case ElementType.WIND:
                    return "바람";
                case ElementType.LIGHT:
                    return "빛";
                case ElementType.DARK:
                    return "어둠";
                default:
                    return string.Empty;
            }
        }

        public string GetClassText(CharacterPositionType type)
        {
            switch (type)
            {
                case CharacterPositionType.TANK:
                    return "탱커";
                case CharacterPositionType.GUARDIAN:
                    return "전사";
                case CharacterPositionType.RANGER:
                    return "레인저";
                case CharacterPositionType.WIZARD:
                    return "마법사";
                case CharacterPositionType.SUPPORTER:
                    return "서포터";
                case CharacterPositionType.ASSASSIN:
                    return "암살자";
                default:
                    return string.Empty;
            }
        }

        public string GetGradeText(GradeType type)
        {
            switch (type)
            {
                case GradeType.COMMON:
                    return "N";
                case GradeType.RARE:
                    return "R";
                case GradeType.EPIC:
                    return "SR";
                case GradeType.LEGEND:
                    return "SSR";
                default:
                    return string.Empty;
            }
        }

        public string GetAtkTypeText(AtkType type)
        {
            switch (type)
            {
                case AtkType.AP:
                    return "마법";
                case AtkType.AD:
                    return "물리";
                default:
                    return string.Empty;
            }
        }
    }
}
