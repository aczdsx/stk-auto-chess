using System;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class RewardItem
    {
        public ItemType Type { get; init; }
        public int Key { get; init; }
        public int Count { get; init; }

        public RewardItem() {}

        public RewardItem(ItemType typeValue, int keyValue, int countValue)
        {
            Type = typeValue;
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
        
        public static string ToCharacterResourcePath(this CharacterType characterType, int prefabId)
        {
            return characterType switch
            {
                CharacterType.CHARACTER => ZString.Format("SD_Characters/{0}/InGame_{0}.prefab", prefabId),
                CharacterType.OBSTACLE => ZString.Format("SD_Obstacle/{0}/InGame_{0}.prefab", prefabId),
                CharacterType.BATTLEITEM => ZString.Format("SD_Item/{0}/InGame_{0}.prefab", prefabId),
                _ => ZString.Format("SD_Mob/{0}/InGame_{0}.prefab", prefabId),
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
    }
}
