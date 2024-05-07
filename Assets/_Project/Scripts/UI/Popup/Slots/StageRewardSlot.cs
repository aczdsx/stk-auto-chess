using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class StageRewardSlot : CachedMonoBehaviour
    {
        [SerializeField] private RewardView rewardView;
        [SerializeField] private GameObject specialRewardNode;
        [SerializeField] private GameObject[] starNodes;
        [SerializeField] private GameObject characterPieceNode;

        public void SetReward(StageReward reward)
        {
            rewardView.SetReward(reward.rewardItem.ToGrpcReward());
            var isSpecialReward = false;
            for (var i = 0; i < starNodes.Length; i++)
            {
                starNodes[i].SetActive(i < reward.targetStarCount);
                isSpecialReward |= i < reward.targetStarCount;
            }

            bool isKnightPiece = reward.rewardItem.ToGrpcReward().RewardType == (int) RewardType.KNIGHT_PIECE;
            isSpecialReward |= isKnightPiece;
            characterPieceNode.SetActive(isKnightPiece);

            specialRewardNode.SetActive(isSpecialReward);
        }
    }
}
