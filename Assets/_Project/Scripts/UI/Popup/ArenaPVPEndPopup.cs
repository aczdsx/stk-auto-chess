using System.Collections;
using System.Collections.Generic;
using CookApps.BattleSystem;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            
            // 애니메이션 연출 적용
            string animKey = _isVictory ? "SetUp" : "SetDown";
            baseAnimator.SetTrigger(animKey);

            var specTierData = SpecDataManager.Instance.GetPVPTierData(_detailData.RankId);

            _tierImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(specTierData.pvp_tier_type);
            _tierSecondImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(specTierData.pvp_tier_type);
            _tierNameText.text = LanguageManager.Instance.GetPVPTierText(specTierData.pvp_tier_type);
            _tierPointText.text = _matchResultData.MyCurrentScore.ToString("n0");
            _tierPointChangeText.text = $"({_matchResultData.MyDeltaScore.ToString("n0")})";
            
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
            }
        }
        
        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(_rewardContentObject.transform);
            
            _tierLevelObjectList?.ForEach(obj => obj.SetActive(false));
        }
    }
}