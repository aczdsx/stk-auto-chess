using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;

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

        public Reward ToGrpcReward()
        {
            return new Reward
            {
                RewardType = (int)Type,
                RewardKey = Key,
                RewardCount = Count,
            };
        }
    }

    public static class SpecDataExtensions
    {
        public static SimpleSwapType ToSimpleSwapType(this GradeType grade)
        {
            return SimpleSwapType.Grade_0 + ((int) grade - 1);
        }

        public static SimpleSwapType ToSimpleSwapType(this ElementType type)
        {
            return SimpleSwapType.Custom_0 + ((int) type - 1);
        }

        public static SimpleSwapType ToSimpleSwapType(this CharacterPositionType pos)
        {
            return SimpleSwapType.Custom_0 + ((int) pos - 1);
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
    }

    public partial class SpecChest
    {
        public RewardItem ToRewardItem()
        {
            return new RewardItem
            {
                Type = type,
                Key = key,
                Count = value,
            };
        }
    }

    public partial class TestSpecCharacter
    {
        public int GetLineIndex()
        {
            return position switch
            {
                CharacterPositionType.WARRIOR => 0,
                CharacterPositionType.TANK => 0,
                CharacterPositionType.RANGER => 1,
                CharacterPositionType.WIZARD => 1,
                CharacterPositionType.ASSASSIN => 2,
                CharacterPositionType.SUPPORTER => 2,
                _ => 0,
            };
        }
    }
}
