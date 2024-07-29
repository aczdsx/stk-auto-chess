using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public enum ArenaMainPopupTabType
    {
        PVP_BATTLE,
        PVP_BATTLE_LOG,
        PVP_RANK,
        PVP_SEASON_REWARD
    }
    
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/ArenaPopup/ArenaMainPopup.prefab")]
    public class ArenaMainPopup : UILayer
    {
        [Header("Common")]
        [SerializeField] private CAButton _closeButton;
        
        [Header("Tab Layer")]
        [SerializeField] private CAToggle _pvpBattleTabToggle;
        [SerializeField] private CAToggle _pvpBattleLogTabToggle;
        [SerializeField] private CAToggle _pvpRankTabToggle;
        [SerializeField] private CAToggle _pvpSeasonRewardTabToggle;
        
        [Space(10)]
        [SerializeField] private PVPBattleLayer _pvpBattleTabLayer;
        [SerializeField] private PVPBattleLogLayer _pvpBattleLogTabLayer;
        [SerializeField] private PVPRankLayer _pvpRankTabLayer;
        [SerializeField] private PVPSeasonRewardLayer _pvpSeasonRewardTabLayer;

        [Header("My PVP Info Layer")] 
        [SerializeField] private PVPMyInfoLayer _myPVPInfoLayer;
        
        public ArenaMainPopupTabType CurrentTabType { get; private set; } = ArenaMainPopupTabType.PVP_BATTLE;
        
        private void Awake()
        {
            _closeButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            CurrentTabType = param as ArenaMainPopupTabType? ?? ArenaMainPopupTabType.PVP_BATTLE;
            
            LoadPVPInfoData();
        }

        public void OnClickBattleTabButton()
        {
            ChangeTabType(ArenaMainPopupTabType.PVP_BATTLE, false);
        }
        
        public void OnClickBattleLogTabButton()
        {
            ChangeTabType(ArenaMainPopupTabType.PVP_BATTLE_LOG, false);
        }
        
        public void OnClickRankTabButton()
        {
            ChangeTabType(ArenaMainPopupTabType.PVP_RANK, false);
        }
        
        public void OnClickSeasonRewardTabButton()
        {
            ChangeTabType(ArenaMainPopupTabType.PVP_SEASON_REWARD, false);
        }

        public void ChangeTabType(ArenaMainPopupTabType tabType, bool isFirstEnter)
        {
            if (CurrentTabType == tabType && isFirstEnter == false) return;

            ClearLayer();
            
            CurrentTabType = tabType;

            // 공용 레이어 초기화
            _myPVPInfoLayer?.InitLayer(this);
            
            // 탭 레이어 초기화
            switch (tabType)
            {
                case ArenaMainPopupTabType.PVP_BATTLE:
                    //_pvpBattleTabToggle.isOn = true;
                    _pvpBattleTabLayer.gameObject.SetActive(true);
                    
                    LoadPVPMatchingData();
                    break;
                case ArenaMainPopupTabType.PVP_BATTLE_LOG:
                    //_pvpBattleLogTabToggle.isOn = true;
                    _pvpBattleLogTabLayer.gameObject.SetActive(true);

                    //LoadPVPLogHistoryData();
                    _pvpBattleTabLayer.InitLayer(this);     // temp - 임시 처리
                    break;
                case ArenaMainPopupTabType.PVP_RANK:
                    //_pvpRankTabToggle.isOn = true;
                    _pvpRankTabLayer.gameObject.SetActive(true);

                    LoadPVPRankData();
                    break;
                case ArenaMainPopupTabType.PVP_SEASON_REWARD:
                    //_pvpSeasonRewardTabToggle.isOn = true;
                    _pvpSeasonRewardTabLayer.gameObject.SetActive(true);
                    
                    _pvpSeasonRewardTabLayer?.InitLayer(this);
                    break;
            }
        }

        public void RefreshTabLayer(ArenaMainPopupTabType tabType)
        {
            _myPVPInfoLayer?.RefreshLayer();
            
            switch (tabType)
            {
                case ArenaMainPopupTabType.PVP_BATTLE:
                    RefreshPVPMatchingData();
                    break;
                case ArenaMainPopupTabType.PVP_BATTLE_LOG:
                    _pvpBattleLogTabLayer?.RefreshLayer();
                    break;
                case ArenaMainPopupTabType.PVP_RANK:
                    _pvpRankTabLayer?.RefreshLayer();
                    break;
                case ArenaMainPopupTabType.PVP_SEASON_REWARD:
                    _pvpSeasonRewardTabLayer?.RefreshLayer();
                    break;
            }
        }

        private async void LoadPVPInfoData()
        {
            await PVPManager.Instance.UpdatePVPInfo();
            
            ChangeTabType(CurrentTabType, true);
        }

        private async void LoadPVPMatchingData()
        {
            var getUserPVPDataList = UserDataManager.Instance.GetPVPMatchingDataList();
            if (getUserPVPDataList == null || getUserPVPDataList.Count <= 0)    // 매칭 데이터가 없을 경우 새로 갱신
            {
                await PVPManager.Instance.UpdatePVPMatchList();
            }
            
            _pvpBattleTabLayer?.InitLayer(this);
        }
        
        private async void RefreshPVPMatchingData()
        {
            await PVPManager.Instance.UpdatePVPMatchList();
            
            _pvpBattleTabLayer?.RefreshLayer();
        }
        
        private async void LoadPVPLogHistoryData()
        {
            await PVPManager.Instance.UpdatePVPHistoryList();
            
            _pvpBattleLogTabLayer.InitLayer(this);
        }
        
        private async void LoadPVPRankData()
        {
            await PVPManager.Instance.UpdatePVPRankList();
            
            _pvpRankTabLayer.InitLayer(this);
        }
        
        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearLayer()
        {
            // _pvpBattleTabToggle.isOn = false;
            // _pvpBattleLogTabToggle.isOn = false;
            // _pvpRankTabToggle.isOn = false;
            // _pvpSeasonRewardTabToggle.isOn = false;
            
            _pvpBattleTabLayer.gameObject.SetActive(false);
            _pvpBattleLogTabLayer.gameObject.SetActive(false);
            _pvpRankTabLayer.gameObject.SetActive(false);
            _pvpSeasonRewardTabLayer.gameObject.SetActive(false);
        }
    }
}