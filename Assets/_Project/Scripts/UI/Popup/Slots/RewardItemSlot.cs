using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class RewardItemSlot : CachedMonoBehaviour
    {
        [SerializeField] private Image _rewardItemImage;
        [SerializeField] private TextMeshProUGUI _rewardItemCountText;

        public void SetRewardItem(RewardItem rewardItem)
        {
            if (rewardItem == null) return;

            _rewardItemImage.sprite = ImageManager.Instance.GetItemSprite(rewardItem.Type);
            _rewardItemCountText.text = $"x{rewardItem.Count}";
        }
    }
}
