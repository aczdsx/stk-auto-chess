using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class ImageManager : Singleton<ImageManager>
    {
        public Sprite GetSprite(string atlasName, string spriteName)
        {
            return AtlasManager.Instance.GetSprite(atlasName, spriteName);
        }

        public Sprite GetGradeSprite(Grade grade, bool isActive = true)
        {
            string spriteName = string.Empty;

            switch (grade)
            {
                // case Grade.COMMON:
                //     return AtlasManager.Instance.GetSprite("UI_Main", "Icon_R");
                case Grade.RARE:
                    spriteName = isActive ? "Icon_R" : "Icon_R_Locked";
                    break;
                case Grade.EPIC:
                    spriteName = isActive ? "Icon_SR" : "Icon_SR_Locked";
                    break;
                case Grade.LEGEND:
                    spriteName = isActive ? "Icon_SSR" : "Icon_SSR_Locked";
                    break;
            }

            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, spriteName);
        }

        public Sprite GetSynergySprite(CharacterType type, bool isActive = true)
        {
            string spriteName = string.Empty;

            switch (type)
            {
                case CharacterType.FIRE:
                    spriteName = isActive ? "Icon_Fire_Active" : "Icon_Fire_Locked";
                    break;
                case CharacterType.WATER:
                    spriteName = isActive ? "Icon_Water_Active" : "Icon_Water_Locked";
                    break;
                case CharacterType.EARTH:
                    spriteName = isActive ? "Icon_Ground_Active" : "Icon_Ground_Locked";
                    break;
                case CharacterType.WIND:
                    spriteName = isActive ? "Icon_Wind_Active" : "Icon_Wind_Locked";
                    break;
                case CharacterType.LIGHT:
                    spriteName = isActive ? "Icon_Light_Active" : "Icon_Light_Locked";
                    break;
                case CharacterType.DARK:
                    spriteName = isActive ? "Icon_Dark_Active" : "Icon_Dark_Locked";
                    break;
            }

            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, spriteName);
        }

        public Sprite GetClassSprite(CharacterPosition position, bool isActive = true)
        {
            string spriteName = string.Empty;

            switch (position)
            {
                case CharacterPosition.TANK:
                    spriteName = isActive ? "Icon_Position_Tank_Active" : "Icon_Position_Tank_Locked";
                    break;
                case CharacterPosition.WARRIOR:
                    spriteName = isActive ? "Icon_Position_Warrior_Active" : "Icon_Position_Warrior_Locked";
                    break;
                case CharacterPosition.RANGER:
                    spriteName = isActive ? "Icon_Position_Sniper_Active" : "Icon_Position_Sniper_Locked";
                    break;
                case CharacterPosition.WIZARD:
                    spriteName = isActive ? "Icon_Position_Wizard_Active" : "Icon_Position_Wizard_Locked";
                    break;
                case CharacterPosition.SUPPORTER:
                    spriteName = isActive ? "Icon_Position_Supporter_Active" : "Icon_Position_Supporter_Locked";
                    break;
                case CharacterPosition.ASSASSIN:
                    spriteName = isActive ? "Icon_Position_Assassin_Active" : "Icon_Position_Assassin_Locked";
                    break;
            }

            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, spriteName);
        }
    }
}
