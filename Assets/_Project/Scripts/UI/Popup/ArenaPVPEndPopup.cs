using System.Collections;
using System.Collections.Generic;
using CookApps.BattleSystem;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/ArenaPopup/ArenaPvpEndPopup.prefab")]
    public class ArenaPVPEndPopup : UILayer
    {
        [Header("Common")]
        [SerializeField] private CAButton _okButton;

        [Header("Tier Info Layer")] 
        [SerializeField] private Image _tierImage;
        [SerializeField] private Image _tierAfterImage;
        [SerializeField] private Image _tierSecondImage;
        [SerializeField] private UICircle _tierSlider;
        [SerializeField] private TextMeshProUGUI _tierNameText;
        [SerializeField] private TextMeshProUGUI _tierPointText;
        [SerializeField] private TextMeshProUGUI _tierPointChangeText;
        [SerializeField] private List<GameObject> _tierLevelObjectList;
        
        [Header("Reward Layer")]
        [SerializeField] private GameObject _rewardContentObject;
        [SerializeField] private GameObject _rewardItemSlotObject;
        
        private bool _isVictory = false;
        private bool _isRevenge = false;
        
        private UserPVPBattleDetailData _detailData;
        private MatchPvpResponse _matchResultData;
        
        private void Awake()
        {
            _okButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _okButton.onClick.RemoveListener(OnClickCloseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.PVP_Ticket);

            ClearPopup();
            
            SoundManager.Instance.StopBGM();
            
            (_isVictory, _detailData, _matchResultData) = ((bool, UserPVPBattleDetailData, MatchPvpResponse))param;
            
            var specTierData = SpecDataManager.Instance.GetPVPTierData(_matchResultData.MyCurrentTier);

            var detailDeckData = InGameManager.Instance.UserPvpBattleDeckList;
            _isRevenge = string.IsNullOrEmpty(detailDeckData.MatchId) == false;
            
            // 등급 변화 체크
            bool isTierChanage = false;
            int checkScore = _matchResultData.MyCurrentScore + _matchResultData.MyDeltaScore;
            var afterSpecTierData = SpecDataManager.Instance.GetPVPTierDataByRankPoint(RankingType.SCORE, checkScore);
            if (afterSpecTierData != null && specTierData != null)
            {
                isTierChanage = specTierData.ranking_id != afterSpecTierData.ranking_id;
            }
            
            // 애니메이션 연출 적용
            string animKey = "";
            if (isTierChanage)
            {
                animKey = _isVictory ? "SetTierUp" : "SetTierDown";
            }
            else
            {
                animKey = _isVictory ? "SetUp" : "SetDown";
            }
            baseAnimator.SetTrigger(animKey);
            
            // 일반 데이터 세팅
            _tierImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(specTierData.pvp_tier_type);
            _tierAfterImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(afterSpecTierData.pvp_tier_type);
            _tierSecondImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(specTierData.pvp_tier_type);
            _tierNameText.text = LanguageManager.Instance.GetPVPTierText(specTierData.pvp_tier_type);
            _tierPointText.text = _matchResultData.MyCurrentScore.ToString("n0");
            _tierPointChangeText.text = $"({_matchResultData.MyDeltaScore.ToString("n0")})";
            
            _tierSlider.Progress = (float)_matchResultData.MyCurrentScore / specTierData.ranking_max;
            
            for(int i = 0; i < specTierData.tier_order; i++)
            {
                _tierLevelObjectList[i].SetActive(true);
            }
            
            // 보상 데이터 세팅
            PvpRewardType targetRewardType = _isVictory ? PvpRewardType.PVP_REWARD_VICTORY : PvpRewardType.PVP_REWARD_LOSE;
            var pvpRewardList = SpecDataManager.Instance.GetRewardItemListByPVPRewardList(targetRewardType, specTierData.ranking_id);
            foreach (var rewardData in pvpRewardList)
            {
                GameObject newObject = Instantiate(_rewardItemSlotObject, _rewardContentObject.transform);
                var rewardSlot = newObject.GetComponent<RewardItemSlot>();
                
                rewardSlot.SetRewardSlot(rewardData);
            }
            
            // 보상 지급 처리
            if (pvpRewardList != null && pvpRewardList.Count > 0)
            {
                UserDataManager.Instance.IncreaseRewardItemList(pvpRewardList, true);
            }
            
            // 앱 이벤트 처리
            var myDeck = UserDataManager.Instance.GetUserCharacterBattleDeckList(InGameType.PVP);
            int myDeckPower = UserDataManager.Instance.GetDeckBattlePower(myDeck);
        
            string result = _isVictory ? "win" : "lose";
            
            var battleTime = 60 - InGameMain.GetInGameMain().InGameTime;
            
            AppEventManager.Instance.PVPEnd(1, _isRevenge, specTierData.pvp_tier_type, _matchResultData.MyCurrentRank, 
                _matchResultData.MyCurrentScore, battleTime, result, myDeckPower, _detailData);
        }
        
        private async void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
            
            SceneUILayerManager.Instance.PopUILayer(this);
            
            //InGameManager.Instance.EndInGame();
            int lastPlayStageID = UserDataManager.Instance.GetLastPlayStageID();
            var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);
            var transition = SceneTransition_FadeInOut.Create();
            await SceneLoading.GoToNextScene("Lobby", (int) specLastStageData.chapter_id, transition);
            
            SceneUILayerManager.OnSceneLoadedEvent += OpenArenaMainPopupAction;
        }
    
        private void OpenArenaMainPopupAction(string scenename)
        {
            if (scenename == "Lobby")
            {
                SceneUILayerManager.Instance.PushUILayerAsync<ArenaMainPopup>().Forget();
            
                SceneUILayerManager.OnSceneLoadedEvent -= OpenArenaMainPopupAction;
            }
        }
        
        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(_rewardContentObject.transform);
            
            _tierLevelObjectList?.ForEach(obj => obj.SetActive(false));
        }
    }
}