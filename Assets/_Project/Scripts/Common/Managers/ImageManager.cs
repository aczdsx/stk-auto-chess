using System.Collections;
using System.Collections.Generic;
using CookApps.BattleSystem;
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

        public Sprite GetItemSprite(ItemType itemType)
        {
            string resultString = string.Concat("ST_RewardItem_", itemType.ToString());

            return AtlasManager.Instance.GetSprite(Defines.STELLA_ICON_ATLAS_NAME, resultString);
        }

        public Sprite GetGradeTypeSprite(GradeType GradeType, bool isActive = true)
        {
            string spriteName = string.Empty;

            switch (GradeType)
            {
                // case GradeType.COMMON:
                //     return AtlasManager.Instance.GetSprite("UI_Main", "Icon_R");
                case GradeType.RARE:
                    spriteName = isActive ? "Icon_R" : "Icon_R_Locked";
                    break;
                case GradeType.EPIC:
                    spriteName = isActive ? "Icon_SR" : "Icon_SR_Locked";
                    break;
                case GradeType.LEGEND:
                    spriteName = isActive ? "Icon_SSR" : "Icon_SSR_Locked";
                    break;
            }

            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, spriteName);
        }

        public Sprite GetSynergySprite(ElementType type, bool isActive = true)
        {
            string spriteName = string.Empty;

            switch (type)
            {
                case ElementType.FIRE:
                    spriteName = isActive ? "Icon_Fire_Active" : "Icon_Fire_Locked";
                    break;
                case ElementType.WATER:
                    spriteName = isActive ? "Icon_Water_Active" : "Icon_Water_Locked";
                    break;
                case ElementType.EARTH:
                    spriteName = isActive ? "Icon_Ground_Active" : "Icon_Ground_Locked";
                    break;
                case ElementType.WIND:
                    spriteName = isActive ? "Icon_Wind_Active" : "Icon_Wind_Locked";
                    break;
                case ElementType.LIGHT:
                    spriteName = isActive ? "Icon_Light_Active" : "Icon_Light_Locked";
                    break;
                case ElementType.DARK:
                    spriteName = isActive ? "Icon_Dark_Active" : "Icon_Dark_Locked";
                    break;
            }

            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, spriteName);
        }

        public Sprite GetPositionSprite(CharacterPositionType position, bool isActive = true)
        {
            string spriteName = string.Empty;

            switch (position)
            {
                case CharacterPositionType.TANK:
                    spriteName = isActive ? "Icon_Position_Tank_Active" : "Icon_Position_Tank_Locked";
                    break;
                case CharacterPositionType.GUARDIAN:
                    spriteName = isActive ? "Icon_Position_Warrior_Active" : "Icon_Position_Warrior_Locked";
                    break;
                case CharacterPositionType.RANGER:
                    spriteName = isActive ? "Icon_Position_Sniper_Active" : "Icon_Position_Sniper_Locked";
                    break;
                case CharacterPositionType.WIZARD:
                    spriteName = isActive ? "Icon_Position_Wizard_Active" : "Icon_Position_Wizard_Locked";
                    break;
                case CharacterPositionType.SUPPORTER:
                    spriteName = isActive ? "Icon_Position_Supporter_Active" : "Icon_Position_Supporter_Locked";
                    break;
                case CharacterPositionType.ASSASSIN:
                    spriteName = isActive ? "Icon_Position_Assassin_Active" : "Icon_Position_Assassin_Locked";
                    break;
            }

            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, spriteName);
        }

        public Sprite GetBossBannerSprite(int bannerID)
        {
            return AtlasManager.Instance.GetSprite(Defines.UI_BANNER_ATLAS_NAME, $"BossBanner_{bannerID}");
        }

        public Sprite GetCharacterIllustSprite(int prefabID)
        {
            return AtlasManager.Instance.GetSprite(Defines.CHAR_ATLAS_ILLUST_NAME, $"Character_Illust_{prefabID}");
        }

        public Sprite GetCharacterSubIllustSprite(int prefabID)
        {
            return AtlasManager.Instance.GetSprite(Defines.CHAR_INVENTORY_ATLAS_NAME, $"Character_Illust_Sub_{prefabID}");
        }

        public Sprite GetCharacterPieceSprite(int prefabID)
        {
            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_CHARACTER_PIECE, $"Piece_{prefabID}");
        }

        public Sprite GetCharacterInGamePortraitSprite(int prefabID)
        {
            return AtlasManager.Instance.GetSprite(Defines.CHAR_INGAME_PORTRAIT_ATLAS_NAME, $"IngameChaPortrait_{prefabID}");
        }

        public Sprite GetCharacterSmallItemSprite(int prefabID)
        {
            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, $"Icon_CharacterSmallItem_{prefabID}");
        }

        public Sprite GetCharacterStigmaSprite(int prefabID)
        {
            return AtlasManager.Instance.GetSprite(Defines.STIGMA_ATLAS_NAME, $"StigmaIcon_{prefabID}");
        }

        public Sprite GetCharacterSkillSprite(int skillID)
        {
            return AtlasManager.Instance.GetSprite(Defines.CHAR_SKILL_ATLAS_NAME, $"Skill_{skillID}");
        }

        public Sprite GetCommanderSkillSprite(int commanderSkillID)
        {
            return AtlasManager.Instance.GetSprite(Defines.COMMANDER_SKILL_ATLAS_NAME, $"CommanderSkill_{commanderSkillID}");
        }

        public Sprite GetBuffDebuffSprite(string type)
        {
            return AtlasManager.Instance.GetSprite(Defines.CHAR_BUFF_DEBUFF_ICON_NAME, $"BuffDebuffIcon_{type}");
        }

        public Color GetGradeTypeColor(GradeType GradeType)
        {
            Color color = Color.white;
            switch (GradeType)
            {
                case GradeType.COMMON:
                    ColorUtility.TryParseHtmlString("#4EA82E", out color);
                    break;
                case GradeType.RARE:
                    ColorUtility.TryParseHtmlString("#0A9AE0", out color);
                    break;
                case GradeType.EPIC:
                    ColorUtility.TryParseHtmlString("#7C11DC", out color);
                    break;
                case GradeType.LEGEND:
                    ColorUtility.TryParseHtmlString("#FFEA7E", out color);
                    break;
            }
            return color;
        }
    }
}
