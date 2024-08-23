using System;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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


        [Header("Top Layer")]
        [SerializeField] 
        private TextMeshProUGUI _remainTimeText;
        private readonly DateTime _targetTime = new DateTime(2024, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        
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
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.PVP_Ticket, TopPanelType.Gold);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            CurrentTabType = param as ArenaMainPopupTabType? ?? ArenaMainPopupTabType.PVP_BATTLE;
            
            SceneUILayerManager.Instance.PushUILayerAsync<LoadingPopup>().Forget();
            
            LoadPVPInfoData();
            
            CheckAndUpdatePVPDataRefreshTime(PVPTimeRefreshType.MATCHING_REFRESH_COUNT, false);
            CheckAndUpdatePVPDataRefreshTime(PVPTimeRefreshType.BUY_TICKET, false);
            CheckAndUpdatePVPDataRefreshTime(PVPTimeRefreshType.DAILY_REWARD, false);
            CheckAndUpdatePVPDataRefreshTime(PVPTimeRefreshType.REFILL_TICKET, false);
            
            UserDataManager.Instance.SaveUserPVPData(); // 여기서 한번에 저장처리
            
            StartCountdown().Forget();
        }

        public void OnClickBattleTabButton()
        {
            ChangeTabType(ArenaMainPopupTabType.PVP_BATTLE, false);
            
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
        }
        
        public void OnClickBattleLogTabButton()
        {
            ChangeTabType(ArenaMainPopupTabType.PVP_BATTLE_LOG, false);
            
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
        }
        
        public void OnClickRankTabButton()
        {
            ChangeTabType(ArenaMainPopupTabType.PVP_RANK, false);
            
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
        }
        
        public void OnClickSeasonRewardTabButton()
        {
            ChangeTabType(ArenaMainPopupTabType.PVP_SEASON_REWARD, false);
            
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
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
                    _pvpBattleTabToggle.isOn = true;
                    _pvpBattleTabLayer.gameObject.SetActive(true);
                    
                    LoadPVPMatchingData();
                    break;
                case ArenaMainPopupTabType.PVP_BATTLE_LOG:
                    _pvpBattleLogTabToggle.isOn = true;
                    _pvpBattleLogTabLayer.gameObject.SetActive(true);

                    LoadPVPLogHistoryData();
                    _pvpBattleTabLayer.InitLayer(this);     // temp - 임시 처리
                    break;
                case ArenaMainPopupTabType.PVP_RANK:
                    _pvpRankTabToggle.isOn = true;
                    _pvpRankTabLayer.gameObject.SetActive(true);

                    LoadPVPRankData();
                    break;
                case ArenaMainPopupTabType.PVP_SEASON_REWARD:
                    _pvpSeasonRewardTabToggle.isOn = true;
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
            bool isNeedRefresh = CheckAndUpdatePVPDataRefreshTime(PVPTimeRefreshType.RANKING_LIST, true);
            if (isNeedRefresh)
            {
                await PVPManager.Instance.UpdatePVPRankList();
            }
            
            _pvpRankTabLayer.InitLayer(this);
        }

        // PVP 팝업 갱신 시간 업데이트 및 체크 (result - true: 갱신 필요)
        private bool CheckAndUpdatePVPDataRefreshTime(PVPTimeRefreshType timeType, bool needPVPDateSave)
        {
            bool result = false;
            
            switch (timeType)
            {
                case PVPTimeRefreshType.MATCHING_LIST:
                    break;
                case PVPTimeRefreshType.MATCHING_REFRESH_COUNT:
                    if (UserDataManager.Instance.UserPVP.RefreshMatchingCntTimestamp <= TimeManager.Instance.UtcNowTimeStampLocal())
                    {
                        UserDataManager.Instance.UserPVP.MatchRefreshCnt = 0;
                        UserDataManager.Instance.UpdateNextRefreshTimeStamp(PVPTimeRefreshType.MATCHING_REFRESH_COUNT, needPVPDateSave);
                        result = true;
                    }
                    break;
                case PVPTimeRefreshType.RANKING_LIST:
                    if (UserDataManager.Instance.UserPVP.RefreshRankingTimestamp <= TimeManager.Instance.UtcNowTimeStampLocal())
                    {
                        UserDataManager.Instance.UpdateNextRefreshTimeStamp(PVPTimeRefreshType.RANKING_LIST, needPVPDateSave);
                        result = true;
                    }
                    break;
                case PVPTimeRefreshType.AUTO_PROFILE:
                    break;
                case PVPTimeRefreshType.DAILY_REWARD:
                    if (UserDataManager.Instance.UserPVP.DailyRewardResetTimestamp <= TimeManager.Instance.UtcNowTimeStampLocal())
                    {
                        // 일일 보상 지급
                        var dailyRewardList = SpecDataManager.Instance.GetRewardItemListByPVPRewardList(PvpRewardType.PVP_REWARD_DAILY, UserDataManager.Instance.UserPVP.RankId);
                        if (dailyRewardList != null && dailyRewardList.Count > 0)
                        {
                            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("PVP_DAILY_REWARD_TITLE", dailyRewardList)).Forget();
                            
                            UserDataManager.Instance.IncreaseRewardItemList(dailyRewardList, true);
                        }
                        
                        // 일일 보상 데이터 처리
                        UserDataManager.Instance.UserPVP.DailyRewardCnt = 0;
                        UserDataManager.Instance.UpdateNextRefreshTimeStamp(PVPTimeRefreshType.DAILY_REWARD, needPVPDateSave);
                        result = true;
                    }
                    break;
                case PVPTimeRefreshType.BUY_TICKET:
                    if (UserDataManager.Instance.UserPVP.BuyTicketResetTimestamp <= TimeManager.Instance.UtcNowTimeStampLocal())
                    {
                        UserDataManager.Instance.UserPVP.BuyTicketCnt = 0;
                        UserDataManager.Instance.UpdateNextRefreshTimeStamp(PVPTimeRefreshType.BUY_TICKET, needPVPDateSave);
                        result = true;
                    }
                    break;
                case PVPTimeRefreshType.REFILL_TICKET:
                    if (UserDataManager.Instance.UserPVP.PvpTicketTimestamp <= TimeManager.Instance.UtcNowTimeStampLocal())
                    {
                        int maxTicket = SpecDataManager.Instance.GetGameConfig<int>("PVP_DAILY_MAX_TICKET_COUNT");
                        UserDataManager.Instance.SetItemCount(ItemType.PVP_TICKET, 0, maxTicket, true, false);
                        UserDataManager.Instance.UpdateNextRefreshTimeStamp(PVPTimeRefreshType.REFILL_TICKET, needPVPDateSave);
                        result = true;
                    }
                    break;
            }

            return result;
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

        public void PlayGuide()
        {
            _myPVPInfoLayer.PlayGuideFx();
        }
        
        private async UniTaskVoid StartCountdown()
        {
            while (true)
            {
                DateTime currentTime = DateTime.UtcNow;

                TimeSpan timeRemaining = _targetTime - currentTime;

                int days = timeRemaining.Days;
                int hours = timeRemaining.Hours;
                int minutes = timeRemaining.Minutes;

                string timeString = "";

                if (days > 0)
                {
                    timeString += $"{days}일 ";
                }
                if (hours > 0)
                {
                    timeString += $"{hours}시간 ";
                }
                if (minutes > 0 || timeString == "")
                {
                    timeString += $"{minutes}분";
                }

                _remainTimeText.text = timeString;

                if (timeRemaining.TotalSeconds <= 0)
                {
                    _remainTimeText.text = "종료되었습니다.";
                    break;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}