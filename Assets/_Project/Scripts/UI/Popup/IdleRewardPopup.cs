using System;
using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/IdleRewardPopup.prefab")]
    public class IdleRewardPopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _getRewardButton;

        [Space(10)]
        [SerializeField] private GameObject _emptyLayerObject;
        [SerializeField] private TextMeshProUGUI _accTimeGuideText;

        [Space(10)]
        [SerializeField] private GameObject _rewardGridLayerObject;
        [SerializeField] private GameObject _rewardSlotObject;

        private List<RewardItem> _currentIdleRewardItemList = new List<RewardItem>();
        private List<RewardItemSlot> _currentIdleRewardItemSlotList = new List<RewardItemSlot>();

        protected override void Awake()
        {
            base.Awake();

            _closeButton.onClick.AddListener(OnClickCloseButton);
            _getRewardButton.onClick.AddListener(OnClickGetRewardButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.AddListener(OnClickCloseButton);
            _getRewardButton.onClick.AddListener(OnClickGetRewardButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            InitIdleRewardData();
        }

        private async void InitIdleRewardData()
        {
            RefreshIdleRewardData();

            await CalculateIdleRewardTime();
        }

        private void RefreshIdleRewardData()
        {
            _currentIdleRewardItemList = UserDataManager.Instance.GetCurrentIdleRewardItemList();

            // 남은 시간 데이터 세팅
            TimeSpan currentRewardTimeSpan = TimeManager.Instance.GetTimeSpanFromNow(UserDataManager.Instance.UserIdleData.LastRewardGetTimestamp);

            _accTimeGuideText.text = $"{currentRewardTimeSpan.Hours.ToString("D2")}:{currentRewardTimeSpan.Minutes.ToString("D2")}:{currentRewardTimeSpan.Seconds.ToString("D2")}";

            SetRewardItemSlot();
        }

        private void SetRewardItemSlot()
        {
            ClearSlot();

            foreach (var idleReward in _currentIdleRewardItemList)
            {
                if (idleReward.Count <= 0) continue;

                GameObject newRewardSlot = Instantiate(_rewardSlotObject, _rewardGridLayerObject.transform);
                RewardItemSlot rewardItemSlot = newRewardSlot.GetComponent<RewardItemSlot>();

                rewardItemSlot.SetRewardItem(idleReward);

                _currentIdleRewardItemSlotList.Add(rewardItemSlot);
            }

            _emptyLayerObject.SetActive(_currentIdleRewardItemSlotList.Count <= 0);
        }

        private async UniTask CalculateIdleRewardTime()
        {
            TimeSpan currentRewardTimeSpan = TimeManager.Instance.GetTimeSpanFromNow(UserDataManager.Instance.UserIdleData.LastRewardGetTimestamp);

            int maxTimeLimitMinute = SpecDataManager.Instance.GetGameConfig<int>("idle_reward_acc_time_limit");

            int refreshMinute = currentRewardTimeSpan.Minutes;  // 분 단위로 갱신
            while (maxTimeLimitMinute > currentRewardTimeSpan.TotalMinutes)
            {
                _accTimeGuideText.text = $"{currentRewardTimeSpan.Hours.ToString("D2")}:{currentRewardTimeSpan.Minutes.ToString("D2")}:{currentRewardTimeSpan.Seconds.ToString("D2")}";

                await UniTask.Delay(1000);

                currentRewardTimeSpan = TimeManager.Instance.GetTimeSpanFromNow(UserDataManager.Instance.UserIdleData.LastRewardGetTimestamp);

                if (refreshMinute != currentRewardTimeSpan.Minutes)
                {
                    RefreshIdleRewardData();
                    refreshMinute = currentRewardTimeSpan.Minutes;
                }
            }

            // 최대 시간 도달 처리
            _accTimeGuideText.text = $"{(maxTimeLimitMinute/60).ToString("D2")}:00:00";
        }

        private void OnClickGetRewardButton()
        {
            RefreshIdleRewardData();

            // 수령 가능한 보상이 있는지 확인
            if (_currentIdleRewardItemList == null || _currentIdleRewardItemList.Count <= 0)
            {
                return;
            }

            // 보상 수령
            UserDataManager.Instance.IncreaseRewardItemList(_currentIdleRewardItemList, true);

            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(_currentIdleRewardItemList).Forget();

            // 방치 보상 데이터 갱신
            UserDataManager.Instance.RefreshLastRewardGetTime();

            // temp - 일단은 off
            OnClickCloseButton();
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearSlot()
        {
            BMUtil.RemoveChildObjects(_rewardGridLayerObject.transform);

            _currentIdleRewardItemSlotList.Clear();
        }
    }
}
