using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class ImageManager : Singleton<ImageManager>
    {
        public Sprite GetGradeSprite(Grade grade)
        {
            switch (grade)
            {
                // case Grade.COMMON:
                //     return AtlasManager.Instance.GetSprite("UI_Main", "Icon_R");
                case Grade.RARE:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_R");
                case Grade.EPIC:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_SR");
                case Grade.LEGEND:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_SSR");
                default:
                    return null;
            }
        }

        public Sprite GetSynergySprite(CharacterType type)
        {
            switch (type)
            {
                case CharacterType.FIRE:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_Fire_Active");
                case CharacterType.WATER:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_Water_Active");
                case CharacterType.EARTH:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_Ground_Active");
                case CharacterType.WIND:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_Wind_Active");
                case CharacterType.LIGHT:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_Light_Active");
                case CharacterType.DARK:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_Dark_Active");
                default:
                    return null;
            }
        }

        public Sprite GetPositionSprite(CharacterPosition position)
        {
            switch (position)
            {
                case CharacterPosition.TANK:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_Position_Tank_Active");
                case CharacterPosition.WARRIOR:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_Position_Warrior_Active");
                case CharacterPosition.RANGER:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_Position_Sniper_Active");
                case CharacterPosition.WIZARD:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_Position_Wizard_Active");
                case CharacterPosition.SUPPORTER:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_Position_Supporter_Active");
                case CharacterPosition.ASSASSIN:
                    return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, "Icon_Position_Assassin_Active");
                default:
                    return null;
            }
        }
    }
}
