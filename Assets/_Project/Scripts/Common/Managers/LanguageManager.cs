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

        public string GetClassText(CharacterPosition type)
        {
            switch (type)
            {
                case CharacterPosition.TANK:
                    return "탱커";
                case CharacterPosition.WARRIOR:
                    return "전사";
                case CharacterPosition.RANGER:
                    return "레인저";
                case CharacterPosition.WIZARD:
                    return "마법사";
                case CharacterPosition.SUPPORTER:
                    return "서포터";
                case CharacterPosition.ASSASSIN:
                    return "암살자";
                default:
                    return string.Empty;
            }
        }
    }
}
