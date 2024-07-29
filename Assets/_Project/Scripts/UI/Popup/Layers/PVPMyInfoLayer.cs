using System;
using System.Collections;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
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

        private void Awake()
        {
            _settingDefenseDeckButton.onClick.AddListener(OnClickSettingDefenseDeckButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            _settingDefenseDeckButton.onClick.RemoveListener(OnClickSettingDefenseDeckButton);
        }

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
            
            for(int i = 0; i < _specPVPTierData.tier_order; i++)
            {
                _myTierLevelObjectList[i].SetActive(true);
            }
            
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

        private void OnClickSettingDefenseDeckButton()
        {
            SceneTransition_Animator transition = SceneTransition_Animator.Create();
            UserPVPBattleDetailData data = UserDataManager.Instance.GetCurrentPVPDetailProfileData();
            SceneLoading.GoToNextScene("InGame",
                (InGameType.PVP_DEFENSE, (IGameStateUI) new InGameMainStatePvpDefenseUI(), data),
                transition).Forget();
        }

        private void ClearLayer()
        {
            BMUtil.RemoveChildObjects(_rewardContentObject.transform);
            
            _myTierLevelObjectList?.ForEach(obj => obj.SetActive(false));
        }
    }
}