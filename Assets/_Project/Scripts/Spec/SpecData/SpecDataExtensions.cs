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
        public double Probability { get; init; }

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

        public static SimpleSwapType ToSimpleSwapType(this CharacterType type)
        {
            return SimpleSwapType.Custom_0 + ((int) type - 1);
        }

        public static SimpleSwapType ToSimpleSwapType(this CharacterPositionType pos)
        {
            return SimpleSwapType.Custom_0 + ((int) pos - 1);
        }

        public static BattleSystem.AttackRangeShape ToInGameAttackRangeShape(this BattleSystem.AttackRangeShape type)
        {
            return type switch
            {
                BattleSystem.AttackRangeShape.Rectangle => BattleSystem.AttackRangeShape.Rectangle,
                BattleSystem.AttackRangeShape.RectangleCut1Edge => BattleSystem.AttackRangeShape.RectangleCut1Edge,
                BattleSystem.AttackRangeShape.RectangleCut2Edge => BattleSystem.AttackRangeShape.RectangleCut2Edge,
                BattleSystem.AttackRangeShape.RectangleCut3Edge => BattleSystem.AttackRangeShape.RectangleCut3Edge,
                BattleSystem.AttackRangeShape.RectangleCut4Edge => BattleSystem.AttackRangeShape.RectangleCut4Edge,
                BattleSystem.AttackRangeShape.RectangleCut5Edge => BattleSystem.AttackRangeShape.RectangleCut5Edge,
                _ => BattleSystem.AttackRangeShape.Rectangle,
            };
        }
        public static BattleSystem.AttackRangeShape ToSpecAttackRangeShape(this BattleSystem.AttackRangeShape type)
        {
            return type switch
            {
                BattleSystem.AttackRangeShape.Rectangle => BattleSystem.AttackRangeShape.Rectangle,
                BattleSystem.AttackRangeShape.RectangleCut1Edge => BattleSystem.AttackRangeShape.RectangleCut1Edge,
                BattleSystem.AttackRangeShape.RectangleCut2Edge => BattleSystem.AttackRangeShape.RectangleCut2Edge,
                BattleSystem.AttackRangeShape.RectangleCut3Edge => BattleSystem.AttackRangeShape.RectangleCut3Edge,
                BattleSystem.AttackRangeShape.RectangleCut4Edge => BattleSystem.AttackRangeShape.RectangleCut4Edge,
                BattleSystem.AttackRangeShape.RectangleCut5Edge => BattleSystem.AttackRangeShape.RectangleCut5Edge,
                _ => BattleSystem.AttackRangeShape.Rectangle,
            };
        }
    }

    public partial class SpecStage
    {
        private RewardItem[] starRewards;
        private (int id, int level)[] front;
        private (int id, int level)[] mid;
        private (int id, int level)[] back;

        public RewardItem[] GetStarRewards()
        {
            return starRewards ??= new RewardItem[]
            {
                new ()
                {
                    Type = star_reward_type_1,
                    Key = star_reward_key_1,
                    Count = star_reward_count_1,
                },
                new ()
                {
                    Type = star_reward_type_2,
                    Key = star_reward_key_2,
                    Count = star_reward_count_2,
                },
                new ()
                {
                    Type = star_reward_type_3,
                    Key = star_reward_key_3,
                    Count = star_reward_count_3,
                },
            };
        }

        public (int id, int level)[] GetFront()
        {
            if (this.front != null)
            {
                return this.front;
            }

            var front = new List<(int id, int level)>();
            if (front_1 > 0)
            {
                front.Add((front_1, front_1Lv));
            }

            if (front_2 > 0)
            {
                front.Add((front_2, front_2Lv));
            }

            if (front_3 > 0)
            {
                front.Add((front_3, front_3Lv));
            }

            this.front = front.ToArray();
            return this.front;
        }

        public (int id, int level)[] GetMid()
        {
            if (this.mid != null)
            {
                return this.mid;
            }

            var mid = new List<(int id, int level)>();
            if (mid_1 > 0)
            {
                mid.Add((mid_1, mid_1Lv));
            }

            if (mid_2 > 0)
            {
                mid.Add((mid_2, mid_2Lv));
            }

            if (mid_3 > 0)
            {
                mid.Add((mid_3, mid_3Lv));
            }

            this.mid = mid.ToArray();
            return this.mid;
        }

        public (int id, int level)[] GetBack()
        {
            if (this.back != null)
            {
                return this.back;
            }

            var back = new List<(int id, int level)>();
            if (back_1 > 0)
            {
                back.Add((back_1, back_1Lv));
            }

            if (back_2 > 0)
            {
                back.Add((back_2, back_2Lv));
            }

            if (back_3 > 0)
            {
                back.Add((back_3, back_3Lv));
            }

            this.back = back.ToArray();
            return this.back;
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
                Probability = rate,
            };
        }
    }

    public partial class SpecCharacter
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
