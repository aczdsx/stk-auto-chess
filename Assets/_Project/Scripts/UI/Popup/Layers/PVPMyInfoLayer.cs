using System.Collections;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class PVPMyInfoLayer : CachedMonoBehaviour
    {
        [SerializeField] private Image _myTierImage;
        [SerializeField] private Slider _myTierSlider;
        [SerializeField] private TextMeshProUGUI _myTierNameText;
        [SerializeField] private List<GameObject> _myTierLevelObjectList;
        
        [SerializeField] private TextMeshProUGUI _myRankingText;
        [SerializeField] private TextMeshProUGUI _myRankingPointText;
        [SerializeField] private TextMeshProUGUI _myBattlePointText;

        [Space(10)] 
        [SerializeField] private GameObject _rewardContentObject;
        [SerializeField] private GameObject _rewardItemSlotObject;

        [Space(10)] 
        [SerializeField] private CAButton _settingDefenseDeckButton;

        private ArenaMainPopup _parentPopup;

        private UserPVP _currentUserPVPData;

        private SpecPVPTier _specPVPTierData;
        
        public void InitLayer(ArenaMainPopup parent)
        {
            _parentPopup = parent;

            ClearLayer();

            _currentUserPVPData = UserDataManager.Instance.UserPVP;

            _specPVPTierData = SpecDataManager.Instance.GetPVPTierDataByRankPoint(RankingType.SCORE, _currentUserPVPData.RankPoint);

            _myTierImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(_specPVPTierData.pvp_tier_type);
            _myTierNameText.text = LanguageManager.Instance.GetPVPTierText(_specPVPTierData.pvp_tier_type);
            _myRankingText.text = _currentUserPVPData.Ranking.ToString();
            _myRankingPointText.text = $"{_currentUserPVPData.RankPoint}<color=#ACB2C0>/{_specPVPTierData.ranking_max}</color>";
            
            // 시즌 보상 리스트
            var seasonRewardList = SpecDataManager.Instance.GetRewardItemListByPVPRewardList(PvpRewardType.PVP_REWARD_SEASON, _currentUserPVPData.RankId);
            foreach (var rewardData in seasonRewardList)
            {
                GameObject newObject = Instantiate(_rewardItemSlotObject, _rewardContentObject.transform);
                var rewardSlot = newObject.GetComponent<RewardItemSlot>();
                
                rewardSlot.SetRewardSlot(rewardData);
            }
        }

        public void RefreshLayer()
        {
            _currentUserPVPData = UserDataManager.Instance.UserPVP;
        }

        private void UpdateLayer()
        {
            
        }

        private void ClearLayer()
        {
            BMUtil.RemoveChildObjects(_rewardContentObject.transform);
        }
    }
}