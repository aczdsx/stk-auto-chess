using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using Tech.Hive.V1;
using UnityEngine;
using Reward = Tech.Hive.V1.Reward;

// LevelUpCommanderSkillAsync
namespace CookApps.AutoBattler
{
    public class RewardItem
    {
        public ItemId Id { get; init; }
        public int Count { get; init; }

        public RewardItem() {}

        public RewardItem(int id, int count)
        {
            Id = id;
            Count = count;
        }

        // From gRPC Reward
        public RewardItem(Reward reward)
        {
            Id = (int)reward.ItemId;
            Count = (int)reward.Count;
        }

        public Reward ToGrpcReward()
        {
            return new Reward
            {
                ItemId = Id,
                Count = (ulong)Count
            };
        }
    }

    public static class SpecDataExtensions
    {
        public static SimpleSwapType ToSimpleSwapType(this GradeType grade)
        {
            return SimpleSwapType.Grade_0 + ((int) grade - 1);
        }

        public static SimpleSwapType ToSimpleSwapType(this SynergyType type)
        {
            return SimpleSwapType.Custom_0 + ((int) type - 1);
        }


        public static BattleSystem.AttackRangeShape ToInGameAttackRangeShape(this AttackRangeShape type)
        {
            return type switch
            {
                AttackRangeShape.Rectangle => BattleSystem.AttackRangeShape.Rectangle,
                AttackRangeShape.RectangleCut1Edge => BattleSystem.AttackRangeShape.RectangleCut1Edge,
                AttackRangeShape.RectangleCut2Edge => BattleSystem.AttackRangeShape.RectangleCut2Edge,
                AttackRangeShape.RectangleCut3Edge => BattleSystem.AttackRangeShape.RectangleCut3Edge,
                AttackRangeShape.RectangleCut4Edge => BattleSystem.AttackRangeShape.RectangleCut4Edge,
                AttackRangeShape.RectangleCut5Edge => BattleSystem.AttackRangeShape.RectangleCut5Edge,
                _ => BattleSystem.AttackRangeShape.Rectangle,
            };
        }
        public static AttackRangeShape ToSpecAttackRangeShape(this BattleSystem.AttackRangeShape type)
        {
            return type switch
            {
                BattleSystem.AttackRangeShape.Rectangle => AttackRangeShape.Rectangle,
                BattleSystem.AttackRangeShape.RectangleCut1Edge => AttackRangeShape.RectangleCut1Edge,
                BattleSystem.AttackRangeShape.RectangleCut2Edge => AttackRangeShape.RectangleCut2Edge,
                BattleSystem.AttackRangeShape.RectangleCut3Edge => AttackRangeShape.RectangleCut3Edge,
                BattleSystem.AttackRangeShape.RectangleCut4Edge => AttackRangeShape.RectangleCut4Edge,
                BattleSystem.AttackRangeShape.RectangleCut5Edge => AttackRangeShape.RectangleCut5Edge,
                _ => AttackRangeShape.Rectangle,
            };
        }
        
        public static ElpisFacilityType ToServerType(this FacilityType specFacilityType)
        {
            return specFacilityType switch
            {
                FacilityType.COMMAND_CENTER => ElpisFacilityType.FacilityTypeCommandCenter,
                FacilityType.NEST => ElpisFacilityType.FacilityTypeNest,
                FacilityType.DIMENSION_LAB => ElpisFacilityType.FacilityTypeDimensionLab,
                FacilityType.SIMULATION_CENTER => ElpisFacilityType.FacilityTypeSimulationCenter,
                _ => ElpisFacilityType.FacilityTypeUnspecified,
            };
        }
        
        public static FacilityType ToSpecType(this ElpisFacilityType serverFacilityType)
        {
            return serverFacilityType switch
            {
                ElpisFacilityType.FacilityTypeCommandCenter => FacilityType.COMMAND_CENTER,
                ElpisFacilityType.FacilityTypeNest => FacilityType.NEST,
                ElpisFacilityType.FacilityTypeDimensionLab => FacilityType.DIMENSION_LAB,
                ElpisFacilityType.FacilityTypeSimulationCenter => FacilityType.SIMULATION_CENTER,
                _ => FacilityType.NONE,
            };
        }
        
        public static string ToCharacterResourcePath(this ISpecCharacterInfo characterInfo)
        {
            return ZString.Format("SD/{0}/InGame_{0}.prefab", characterInfo.prefab_id);
        }

        public static string ToObstacleResourcePath(int obstacleId)
        {
            return ZString.Format("SD/{0}/InGame_{0}.prefab", obstacleId);
        }

        public static Color GetGradeTypeColor(this GradeType gradeType)
        {
            Color color = Color.white;
            _ = gradeType switch
            {
                GradeType.COMMON => ColorUtility.TryParseHtmlString("#4EA82E", out color),
                GradeType.RARE => ColorUtility.TryParseHtmlString("#0A9AE0", out color),
                GradeType.EPIC => ColorUtility.TryParseHtmlString("#7C11DC", out color),
                GradeType.LEGENDARY => ColorUtility.TryParseHtmlString("#FFEA7E", out color),
                _ => false
            };

            return color;
        }
        
        public static Tech.Hive.V1.QuestType ToServerType(this TermType termType)
        {
            return termType switch
            {
                TermType.DAILY => Tech.Hive.V1.QuestType.Daily,
                // QuestType.CLEAR_WEEKLY_QUEST => Tech.Hive.V1.QuestType.Unspecified,
                _ => Tech.Hive.V1.QuestType.Unspecified,
            };
        }
    }
}
