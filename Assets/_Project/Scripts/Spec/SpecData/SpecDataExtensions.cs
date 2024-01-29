using System.Collections.Generic;
using Com.Cookapps.Sampleteambattle;
using CookApps.TeamBattle.Utility;

namespace CookApps.SampleTeamBattle
{
    public class RewardItem
    {
        public RewardType Type { get; init; }
        public int Key { get; init; }
        public int Count { get; init; }
        public double Probability { get; init; }

        public Reward ToGrpcReward()
        {
            return new Reward
            {
                RewardType = (int) Type,
                RewardKey = Key,
                RewardCount = Count,
            };
        }
    }

    public static class SpecDataExtensions
    {
        public static SimpleSwapType ToSimpleSwapType(this Grade grade)
        {
            return SimpleSwapType.Grade_0 + ((int) grade - 1);
        }
    }

    public partial class SpecStage
    {
        private RewardItem[] starRewards;
        private int[] front;
        private int[] mid;
        private int[] back;

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

        public int[] GetFront()
        {
            if (this.front != null)
            {
                return this.front;
            }

            var front = new List<int>();
            if (front_1 > 0)
            {
                front.Add(front_1);
            }

            if (front_2 > 0)
            {
                front.Add(front_2);
            }

            if (front_3 > 0)
            {
                front.Add(front_3);
            }

            this.front = front.ToArray();
            return this.front;
        }

        public int[] GetMid()
        {
            if (this.mid != null)
            {
                return this.mid;
            }

            var mid = new List<int>();
            if (mid_1 > 0)
            {
                mid.Add(mid_1);
            }

            if (mid_2 > 0)
            {
                mid.Add(mid_2);
            }

            if (mid_3 > 0)
            {
                mid.Add(mid_3);
            }

            this.mid = mid.ToArray();
            return this.mid;
        }

        public int[] GetBack()
        {
            if (this.back != null)
            {
                return this.back;
            }

            var back = new List<int>();
            if (back_1 > 0)
            {
                back.Add(back_1);
            }

            if (back_2 > 0)
            {
                back.Add(back_2);
            }

            if (back_3 > 0)
            {
                back.Add(back_3);
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
                CharacterPosition.WARRIOR => 0,
                CharacterPosition.TANK => 0,
                CharacterPosition.RANGER => 1,
                CharacterPosition.WIZARD => 1,
                CharacterPosition.ASSASSIN => 2,
                CharacterPosition.SUPPORTER => 2,
                _ => 0,
            };
        }
    }
}
