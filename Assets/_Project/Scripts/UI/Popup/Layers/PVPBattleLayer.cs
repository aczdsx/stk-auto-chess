using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class PVPBattleLayer : CachedMonoBehaviour
    {
        [Header("Common")]
        [SerializeField] private GameObject _emptyLayerObject;  // 매칭리스트에 아무도 없을 경우 노출
        
        [Header("Refresh Maching List")]
        [SerializeField] private CAButton _matchRefreshButton;
        [SerializeField] private Image _matchRefreshItemImage;
        [SerializeField] private TextMeshProUGUI _matchRefreshItemAmountText;
        [SerializeField] private TextMeshProUGUI _matchRefreshRemainTimeText;
        
        [Header("Matching List")]
        [SerializeField] private ScrollRect _matchingScrollRect;
        [SerializeField] private GameObject _matchingSlotObject;
        
        private ArenaMainPopup _parentPopup;

        private UserPVP _currentUserPVPData;
        private List<UserPVPBattleSimpleData> _currentServerMatchingDataList;

        private int _refreshPrice;

        private bool _isAvailRefresh = false;
        
        private CancellationTokenSource _unitaskCancelToken = new CancellationTokenSource();
        
        private void Awake()
        {
            _matchRefreshButton.onClick.AddListener(OnClickMatchingRefreshButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            _matchRefreshButton.onClick.RemoveListener(OnClickMatchingRefreshButton);
        }

        public async void InitLayer(ArenaMainPopup parent)
        {
            _parentPopup = parent;

            _currentUserPVPData = UserDataManager.Instance.UserPVP;

            _refreshPrice = SpecDataManager.Instance.GetGameConfig<int>("PVP_REFRESH_MATCHING_LIST_COST");

            _matchingScrollRect.verticalNormalizedPosition = 1;

            RefreshLayer();
        }
        
        public async void RefreshLayer()
        {
            CreateMatchingScrollList();
            
            _emptyLayerObject?.SetActive(_currentServerMatchingDataList == null || _currentServerMatchingDataList.Count == 0);
            
            // 남은 갱신 시간 unitask
            _unitaskCancelToken.Cancel();
            _unitaskCancelToken = new CancellationTokenSource();
            
            try
            {
                await CheckRefreshMatchListTime(_unitaskCancelToken.Token).AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
        }
        
        private void CreateMatchingScrollList()
        {
            ClearLayer();

            _currentServerMatchingDataList = UserDataManager.Instance.GetPVPMatchingDataList();
            
            foreach (var matchData in _currentServerMatchingDataList)
            {
                GameObject newSlotObject = Instantiate(_matchingSlotObject, _matchingScrollRect.content);
                var matchingSlot = newSlotObject.GetComponent<ArenaBattleEnemySlot>();
                    
                matchingSlot?.InitMatchSlot(matchData);
            }
        }
        
        private async UniTask CheckRefreshMatchListTime(CancellationToken cancelToken)
        {
            _isAvailRefresh = false;
            
            TimeSpan currentRewardTimeSpan = TimeManager.Instance.GetTimeSpan(UserDataManager.Instance.UserPVP.NextRefreshMatchingListTimestamp);

            try
            {
                while (currentRewardTimeSpan.TotalSeconds > 0)
                {
                    _matchRefreshRemainTimeText.text = $"다음 갱신 까지:{currentRewardTimeSpan.Hours.ToString("D2")}:{currentRewardTimeSpan.Minutes.ToString("D2")}:{currentRewardTimeSpan.Seconds.ToString("D2")}";

                    await UniTask.Delay(1000, cancellationToken:cancelToken);

                    currentRewardTimeSpan = TimeManager.Instance.GetTimeSpan(UserDataManager.Instance.UserPVP.NextRefreshMatchingListTimestamp);
                }

                // 최대 시간 도달 처리
                if (currentRewardTimeSpan.TotalSeconds <= 0)
                {
                    _isAvailRefresh = true;
                    _matchRefreshRemainTimeText.text = "";
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        
        private void OnClickMatchingRefreshButton()
        {
            // 리프레쉬 가능 상태 체크
            if (_isAvailRefresh == false)
            {
                ToastManager.Instance.ShowToast("TEST - 아직 갱신할 수 없습니다.");
                return;
            }
            
            // 재화 소지 체크
            if (!UserDataManager.Instance.CheckEnoughItem(ItemType.GOLD, 0, _refreshPrice, true))
            {
                return;
            }
            
            // 재화 소모 처리
            UserDataManager.Instance.DecreaseItem(ItemType.GOLD, 0, _refreshPrice, true, false);
            
            UserDataManager.Instance.UpdateNextRefreshTimeStamp(PVPTimeRefreshType.MATCHING_LIST, true);

            _parentPopup?.RefreshTabLayer(ArenaMainPopupTabType.PVP_BATTLE);

            _isAvailRefresh = false;
        }
        
        private void ClearLayer()
        {
            BMUtil.RemoveChildObjects(_matchingScrollRect.content);
        }
    }
}