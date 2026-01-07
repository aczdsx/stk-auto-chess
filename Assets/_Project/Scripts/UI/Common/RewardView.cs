using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class RewardView : CachedMonoBehaviour
    {
        [SerializeField] protected SimpleSwapper frameSwapper;
        [SerializeField] protected Image rewardIcon;
        [SerializeField] protected SpriteLoader rewardIconSpriteLoader;
        [SerializeField] protected TMP_Text rewardCount;

        private RewardItem reward;
        public RewardItem Reward => reward;

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

        public virtual void SetReward(RewardItem reward)
        {
            this.reward = reward;
            rewardIcon.gameObject.SetActive(true);
            rewardIconSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(reward.Id)).Forget();
            // rewardIcon.sprite = AtlasManager.Instance.GetSprite("UI", type.GetIconName());
            if (frameSwapper != null)
            {
                frameSwapper.Swap(SimpleSwapType.Grade_0);
            }
        }
    }
}
