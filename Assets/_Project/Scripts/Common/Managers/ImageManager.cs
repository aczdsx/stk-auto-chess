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
                case GradeType.LEGENDARY:
                    spriteName = isActive ? "Icon_SSR" : "Icon_SSR_Locked";
                    break;
            }

            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, spriteName);
        }

        public Sprite GetElementSprite(SynergyType elementType, bool isActive = true)
        {
            string spriteName = string.Empty;

            switch (elementType)
            {
                case SynergyType.FIRE:
                    spriteName = isActive ? "Icon_Fire_Active" : "Icon_Fire_Locked";
                    break;
                case SynergyType.WATER:
                    spriteName = isActive ? "Icon_Water_Active" : "Icon_Water_Locked";
                    break;
                case SynergyType.EARTH:
                    spriteName = isActive ? "Icon_Ground_Active" : "Icon_Ground_Locked";
                    break;
                case SynergyType.WIND:
                    spriteName = isActive ? "Icon_Wind_Active" : "Icon_Wind_Locked";
                    break;
                case SynergyType.LIGHTNING:
                    spriteName = isActive ? "Icon_Lightning_Active" : "Icon_Lightning_Locked";
                    break;
                default:
                    return null;
            }

            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, spriteName);
        }

        public Sprite GetSynergySprite(SynergyType synergyType, bool isActive = true)
        {
            string spriteName = string.Empty;

            switch (synergyType)
            {
                case SynergyType.NOBLESSE:
                    spriteName = isActive ? "Icon_Position_Noblesse_Active" : "Icon_Position_Noblesse_Locked";
                    break;
                case SynergyType.SUPERNOVA:
                    spriteName = isActive ? "Icon_Position_Supernova_Active" : "Icon_Position_Supernova_Locked";
                    break;
                case SynergyType.TROUBLESHOOTER:
                    spriteName = isActive ? "Icon_Position_TroubleShooter_Active" : "Icon_Position_TroubleShooter_Locked";
                    break;

                default:
                    return null;
            }

            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, spriteName);
        }

        public Sprite GetDungeonTrialClassSprite(TrialType type, bool isCompete)
        {
            string spriteName = string.Empty;

            switch (type)
            {
                case TrialType.BRONZE:
                    spriteName = isCompete ? "Icon_DungeonTrialClass_Bronze_Completed" : "Icon_DungeonTrialClass_Bronze";
                    break;
                case TrialType.SILVER:
                    spriteName = isCompete ? "Icon_DungeonTrialClass_Silver_Completed" : "Icon_DungeonTrialClass_Silver";
                    break;
                case TrialType.GOLD:
                    spriteName = isCompete ? "Icon_DungeonTrialClass_Gold_Completed" : "Icon_DungeonTrialClass_Gold";
                    break;
                case TrialType.PLATINUM:
                    spriteName = isCompete ? "Icon_DungeonTrialClass_Platinum_Completed" : "Icon_DungeonTrialClass_Platinum";
                    break;
                case TrialType.DIAMOND:
                    spriteName = isCompete ? "Icon_DungeonTrialClass_Legend_Completed" : "Icon_DungeonTrialClass_Legend";
                    break;
            }

            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, spriteName);
        }

        public Sprite GetPVPTierIconSprite(PVPTierType type)
        {
            string spriteName = string.Empty;

            switch (type)
            {
                case PVPTierType.BRONZE:
                    spriteName = "Icon_Rank_Bronze";
                    break;
                case PVPTierType.SILVER:
                    spriteName = "Icon_Rank_Silver";
                    break;
                case PVPTierType.GOLD:
                    spriteName = "Icon_Rank_Gold";
                    break;
                case PVPTierType.PLATINUM:
                    spriteName = "Icon_Rank_Platinum";
                    break;
                case PVPTierType.DIAMOND:
                    spriteName = "Icon_Rank_Diamond";
                    break;
            }

            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, spriteName);
        }

        public Sprite GetChapterIconSprite(int chapterID)
        {
            return AtlasManager.Instance.GetSprite(Defines.UI_ATLAS_NAME, $"Icon_Chapter_{chapterID}");
        }

        public Sprite GetCutSceneSprite(string spriteName)
        {
            return AtlasManager.Instance.GetSprite(Defines.UI_CUT_SCENE_ATLAS, spriteName);
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

        public Sprite GetObstacleInGamePortraitSprite(int prefabID)
        {
            return AtlasManager.Instance.GetSprite(Defines.CHAR_INGAME_PORTRAIT_ATLAS_NAME, $"IngameObsPortrait_{prefabID}");
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

        public Sprite GetBuffDebuffSprite(int codeID)
        {
            return AtlasManager.Instance.GetSprite(Defines.CHAR_BUFF_DEBUFF_ICON_NAME, $"BuffDebuffIcon_{codeID}");
        }

        public Sprite GetInfoImageSprite(int infoID)
        {
            return AtlasManager.Instance.GetSprite(Defines.UI_INFO_IMAGE, $"InfoImage_{infoID}");
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
                case GradeType.LEGENDARY:
                    ColorUtility.TryParseHtmlString("#FFEA7E", out color);
                    break;
            }
            return color;
        }
    }
}
