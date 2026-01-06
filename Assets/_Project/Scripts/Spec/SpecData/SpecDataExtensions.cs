using System;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class RewardItem
    {
        public int Key { get; init; }
        public int Count { get; init; }

        public RewardItem() {}

        public RewardItem(int keyValue, int countValue)
        {
            Key = keyValue;
            Count = countValue;
        }

        public Tech.Hive.V1.Reward ToGrpcReward()
        {
            return new Tech.Hive.V1.Reward
            {
                ItemId = (uint)Key,
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
        
        public static string ToCharacterResourcePath(this CharacterType characterType, int prefabId)
        {
            return characterType switch
            {
                CharacterType.CHARACTER => ZString.Format("SD/{0}/InGame_{0}.prefab", prefabId),
                CharacterType.OBSTACLE => ZString.Format("SD/{0}/InGame_{0}.prefab", prefabId),
                CharacterType.BATTLEITEM => ZString.Format("SD/{0}/InGame_{0}.prefab", prefabId),
                _ => ZString.Format("SD/{0}/InGame_{0}.prefab", prefabId),
            };
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
        
        public static bool IsCharacterId(this int rewardId)
        {
            // TODO: rewardId가 캐릭터인지 체크
            return false;
        }
        
        public static bool IsCharacterPieceId(this int rewardId)
        {
            // TODO: rewardId가 캐릭터 조각인지 체크
            return false;
        }
    }
}
