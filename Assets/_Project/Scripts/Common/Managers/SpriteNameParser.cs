using Cysharp.Text;

namespace CookApps.AutoBattler
{
    public static class SpriteNameParser
    {
        public static string GetSpriteName(GradeType GradeType, bool isActive = true)
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

            return spriteName;
        }

        public static string GetSpriteName(SynergyType synergyType, bool isActive = true)
        {
            string spriteName = string.Empty;

            switch (synergyType)
            {
                case SynergyType.NOBLESSE:
                    spriteName = isActive ? "Icon_Constellation_Noblesse_Active" : "Icon_Constellation_Noblesse_Locked";
                    break;
                case SynergyType.SUPERNOVA:
                    spriteName = isActive ? "Icon_Constellation_Supernova_Active" : "Icon_Constellation_Supernova_Locked";
                    break;
                case SynergyType.TROUBLESHOOTER:
                    spriteName = isActive ? "Icon_Constellation_TroubleShooter_Active" : "Icon_Constellation_TroubleShooter_Locked";
                    break;
                case SynergyType.NORMAL:
                    spriteName = "Icon_Enemy_Normal_Active";
                    break;


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
            }

            return spriteName;
        }

        public static string GetSpriteName(TrialType type, bool isCompete)
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

            return spriteName;
        }

        public static string GetSpriteName(PVPTierType type)
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

            return spriteName;
        }

        public static string GetItemSprite(int itemId)
        {
            // TODO: 아이템 스프라이트 리턴하기
            return null;
            // return ZString.Format("ST_RewardItem_{0}", itemType);
        }

        public static string GetChapterIcon(int chapterID)
        {
            return ZString.Format("Icon_Chapter_{0}", chapterID);
        }

        public static string GetBossBannerSprite(int bannerID)
        {
            return ZString.Format("BossBanner_{0}", bannerID);
        }

        public static string GetCharacterIllustSprite(int prefabID)
        {
            return ZString.Format("Character_Illust_{0}", prefabID);
        }

        public static string GetCharacterSubIllustSprite(int prefabID)
        {
            return ZString.Format("Character_Illust_Sub_{0}", prefabID);
        }

        public static string GetCharacterPieceSprite(int prefabID)
        {
            return ZString.Format("Piece_{0}", prefabID);
        }

        public static string GetCharacterInGamePortraitSprite(int prefabID)
        {
            return ZString.Format("IngameChaPortrait_{0}", prefabID);
        }

        public static string GetObstacleInGamePortraitSprite(int prefabID)
        {
            return ZString.Format("IngameObsPortrait_{0}", prefabID);
        }

        public static string GetCharacterSmallItemSprite(int prefabID)
        {
            return ZString.Format("Icon_CharacterSmallItem_{0}", prefabID);
        }

        public static string GetCharacterStigmaSprite(int prefabID)
        {
            return ZString.Format("StigmaIcon_{0}", prefabID);
        }

        public static string GetCharacterSkillSprite(int skillID)
        {
            return ZString.Format("Skill_{0}", skillID);
        }

        public static string GetCommanderSkillSprite(int commanderSkillID)
        {
            return ZString.Format("CommanderSkill_{0}", commanderSkillID);
        }

        public static string GetBuffDebuffSprite(int codeID)
        {
            return ZString.Format("BuffDebuffIcon_{0}", codeID);
        }

        public static string GetInfoImageSprite(int infoID)
        {
            return ZString.Format("InfoImage_{0}", infoID);
        }
    }
}
