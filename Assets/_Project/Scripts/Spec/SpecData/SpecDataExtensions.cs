using System.Collections.Generic;
using Com.Cookapps.Sampleteambattle;
using CookApps.TeamBattle.Utility;

namespace CookApps.SampleTeamBattle
{
    public static class SpecDataExtensions
    {
        public static SimpleSwapType ToSimpleSwapType(this Grade grade)
        {
            return SimpleSwapType.Grade_0 + ((int) grade - 1);
        }
    }

    public partial class SpecStage
    {
        private Reward[] starRewards;

        public Reward[] GetStarRewards()
        {
            return starRewards ??= new Reward[]
            {
                new ()
                {
                    RewardType = (int) star_reward_type_1,
                    RewardId = star_reward_key_1,
                    RewardCount = star_reward_count_1,
                },
                new ()
                {
                    RewardType = (int) star_reward_type_2,
                    RewardId = star_reward_key_2,
                    RewardCount = star_reward_count_2,
                },
                new ()
                {
                    RewardType = (int) star_reward_type_3,
                    RewardId = star_reward_key_3,
                    RewardCount = star_reward_count_3,
                },
            };
        }
    }
}
