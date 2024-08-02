using System.Collections;
using System.Collections.Generic;
using CookApps.BattleSystem;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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

            // 등급 변화 체크
            bool isTierChanage = false;
            int checkScore = _matchResultData.MyCurrentScore + _matchResultData.MyDeltaScore;
            var checkSpecTierData = SpecDataManager.Instance.GetPVPTierDataByRankPoint(RankingType.SCORE, checkScore);
            if (checkSpecTierData != null && specTierData != null)
            {
                isTierChanage = specTierData.ranking_id != checkSpecTierData.ranking_id;
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
            _tierSecondImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(specTierData.pvp_tier_type);
            _tierNameText.text = LanguageManager.Instance.GetPVPTierText(specTierData.pvp_tier_type);
            _tierPointText.text = _matchResultData.MyCurrentScore.ToString("n0");
            _tierPointChangeText.text = $"({_matchResultData.MyDeltaScore.ToString("n0")})";
            
            _tierSlider.Progress = (float)_matchResultData.MyCurrentScore / specTierData.ranking_max;
            
            for(int i = 0; i < specTierData.tier_order; i++)
            {
                _tierLevelObjectList[i].SetActive(true);
            }
            
            // 시즌 보상 리스트
            var seasonRewardList = SpecDataManager.Instance.GetRewardItemListByPVPRewardList(PvpRewardType.PVP_REWARD_SEASON, specTierData.ranking_id);
            foreach (var rewardData in seasonRewardList)
            {
                GameObject newObject = Instantiate(_rewardItemSlotObject, _rewardContentObject.transform);
                var rewardSlot = newObject.GetComponent<RewardItemSlot>();
                
                rewardSlot.SetRewardSlot(rewardData);
            }
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