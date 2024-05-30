using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class RewardView : CachedMonoBehaviour
    {
        [SerializeField] protected SimpleSwapper frameSwapper;
        [SerializeField] protected Image rewardIcon;
        [SerializeField] protected TMP_Text rewardCount;

        private Reward reward;
        public Reward Reward => reward;

        public virtual void SetOnlyGrade(GradeType grade)
        {
            if (frameSwapper != null)
            {
                frameSwapper.Swap(grade.ToSimpleSwapType());
            }

            rewardIcon.gameObject.SetActive(false);
            if (rewardCount != null)
            {
                rewardCount.gameObject.SetActive(false);
            }
        }

        public virtual void SetReward(Reward reward)
        {
            this.reward = reward;

            var rewardType = (ItemType) reward.RewardType;
            rewardIcon.gameObject.SetActive(true);
            // rewardIcon.sprite = AtlasManager.Instance.GetSprite("UI", type.GetIconName());
            if (frameSwapper != null)
            {
                frameSwapper.Swap(SimpleSwapType.Grade_0);
            }
        }
    }
}
