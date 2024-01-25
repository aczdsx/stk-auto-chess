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
}
