using System.Collections.Generic;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class ArenaSeasonRewardSlot : CachedMonoBehaviour
    {
        [SerializeField] private Image _rankTierImage;
        [SerializeField] private TextMeshProUGUI _rankNameText;
        [SerializeField] private TextMeshProUGUI _rankPointText;
        [SerializeField] private List<GameObject> _tierLevelStickObjectList;
        
        [SerializeField] private GameObject _rewardSlotObject;
        
        [Header("Daily Reward Layer")]
        [SerializeField] private GameObject _dailyRewardContentObject;
        
        [Header("Season Reward Layer")]
        [SerializeField] private GameObject _seasonRewardContentObject;

        private SpecPVPTier _specPVPTierData;
        
        public void InitSlot(SpecPVPTier data)
        {
            if (data == null) return;

            ClearSlot();

            _specPVPTierData = data;
            var dailyRewardDataList = SpecDataManager.Instance.GetRewardItemListByPVPRewardList(PvpRewardType.PVP_REWARD_DAILY, _specPVPTierData.ranking_id);
            var seasonRewardDataList = SpecDataManager.Instance.GetRewardItemListByPVPRewardList(PvpRewardType.PVP_REWARD_SEASON, _specPVPTierData.ranking_id);
            
            _rankTierImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(_specPVPTierData.pvp_tier_type);
            _rankNameText.text = LanguageManager.Instance.GetPVPTierText(_specPVPTierData.pvp_tier_type);
            _rankPointText.text = _specPVPTierData.ranking_min.ToString();

            for(int i = 0; i < _specPVPTierData.tier_order; i++)
            {
                _tierLevelStickObjectList[i].SetActive(true);
            }
            
            // 보상 데이터 리스트 생성 (일일)
            foreach (var rewardItem in dailyRewardDataList)
            {
                GameObject newObject = Instantiate(_rewardSlotObject, _dailyRewardContentObject.transform);
                var rewardSlot = newObject.GetComponent<RewardItemSlot>();
                rewardSlot.SetRewardSlot(rewardItem);
            }
            
            // 보상 데이터 리스트 생성 (시즌)
            foreach (var rewardItem in seasonRewardDataList)
            {
                GameObject newObject = Instantiate(_rewardSlotObject, _seasonRewardContentObject.transform);
                var rewardSlot = newObject.GetComponent<RewardItemSlot>();
                rewardSlot.SetRewardSlot(rewardItem);
            }
        }

        private void ClearSlot()
        {
            BMUtil.RemoveChildObjects(_dailyRewardContentObject.transform);
            BMUtil.RemoveChildObjects(_seasonRewardContentObject.transform);
            
            _tierLevelStickObjectList?.ForEach(obj => obj.SetActive(false));
        }
    }
}