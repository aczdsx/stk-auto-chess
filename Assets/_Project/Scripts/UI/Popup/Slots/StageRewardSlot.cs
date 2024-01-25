using CookApps.TeamBattle;

namespace CookApps.SampleTeamBattle
{
    public class StageRewardSlot : CachedMonoBehaviour
    {
        private RewardView rewardView;

        public void SetReward(StageReward reward)
        {
            rewardView.SetReward(reward.rewardItem.ToGrpcReward());
        }
    }
}
