using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class LanguageManager : Singleton<LanguageManager>
    {
        public string GetSynergyText(CharacterType type)
        {
            switch (type)
            {
                case CharacterType.FIRE:
                    return "불";
                case CharacterType.WATER:
                    return "물";
                case CharacterType.EARTH:
                    return "대지";
                case CharacterType.WIND:
                    return "바람";
                case CharacterType.LIGHT:
                    return "빛";
                case CharacterType.DARK:
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
                case CharacterPositionType.WARRIOR:
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
    }
}
