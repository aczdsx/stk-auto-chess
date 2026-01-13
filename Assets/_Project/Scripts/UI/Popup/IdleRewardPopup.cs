using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class IdleRewardPopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimCloseButton;
        [SerializeField] private CAButton _getRewardButton;

        [Space(10)]
        [SerializeField] private GameObject _emptyLayerObject;
        [SerializeField] private TextMeshProUGUI _accTimeGuideText;
        [SerializeField] private TextMeshProUGUI _maxTimeGuideText;
        [SerializeField] private Slider _accTimeSlider;
        [SerializeField] private TextMeshProUGUI _accTimeSliderText;
        [SerializeField] private GameObject _fullRewardSliderLayerObject;
        [SerializeField] private GameObject _fullRewardBoxLayerObject;

        [Space(10)]
        [SerializeField] private GameObject _rewardGridLayerObject;
        [SerializeField] private GameObject _rewardSlotObject;

        private List<RewardItem> _currentIdleRewardItemList = new List<RewardItem>();
        private List<RewardItemSlot> _currentIdleRewardItemSlotList = new List<RewardItemSlot>();

        protected override void Awake()
        {
            base.Awake();

            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _dimCloseButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _getRewardButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickGetRewardButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            InitIdleRewardData();
        }

        private async void InitIdleRewardData()
        {
            string maxTimeGuideString = LanguageManager.Instance.GetLanguageText("UI_IDLE_REWARD_MAX_TIME_GUIDE");
            int maxMinute = SpecDataManager.Instance.GetGameConfig<int>("idle_reward_acc_time_limit");
            _maxTimeGuideText.text = string.Format(maxTimeGuideString, maxMinute / 60);
            
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
            var token = this.GetCancellationTokenOnDestroy();

            _fullRewardSliderLayerObject.SetActive(false);
            _fullRewardBoxLayerObject.SetActive(false);
            
            TimeSpan currentRewardTimeSpan = TimeManager.Instance.GetTimeSpanFromNow(UserDataManager.Instance.UserIdleData.LastRewardGetTimestamp);

            int maxTimeLimitMinute = SpecDataManager.Instance.GetGameConfig<int>("idle_reward_acc_time_limit");

            int refreshMinute = currentRewardTimeSpan.Minutes;  // 분 단위로 갱신
            try
            {
                while (maxTimeLimitMinute > currentRewardTimeSpan.TotalMinutes)
                {
                    _accTimeGuideText.text = $"{currentRewardTimeSpan.Hours.ToString("D2")}:{currentRewardTimeSpan.Minutes.ToString("D2")}:{currentRewardTimeSpan.Seconds.ToString("D2")}";

                    _accTimeSlider.maxValue = maxTimeLimitMinute;
                    _accTimeSlider.value = (float)currentRewardTimeSpan.TotalMinutes;
                    
                    _accTimeSliderText.text = $"{currentRewardTimeSpan.Hours.ToString("D2")}:{currentRewardTimeSpan.Minutes.ToString("D2")}:{currentRewardTimeSpan.Seconds.ToString("D2")} / {(maxTimeLimitMinute/60).ToString("D2")}:00:00";
                    
                    await UniTask.Delay(1000, cancellationToken:token);

                    currentRewardTimeSpan = TimeManager.Instance.GetTimeSpanFromNow(UserDataManager.Instance.UserIdleData.LastRewardGetTimestamp);

                    if (refreshMinute != currentRewardTimeSpan.Minutes)
                    {
                        RefreshIdleRewardData();
                        refreshMinute = currentRewardTimeSpan.Minutes;
                    }
                }

                // 최대 시간 도달 처리
                if (maxTimeLimitMinute <= currentRewardTimeSpan.TotalMinutes)
                {
                    _accTimeGuideText.text = $"{(maxTimeLimitMinute/60).ToString("D2")}:00:00";
                    _accTimeSliderText.text = $"{(maxTimeLimitMinute/60).ToString("D2")}:00:00 / {(maxTimeLimitMinute/60).ToString("D2")}:00:00";
                    
                    _accTimeSlider.maxValue = maxTimeLimitMinute;
                    _accTimeSlider.value = (float)currentRewardTimeSpan.TotalMinutes;
                    
                    _fullRewardSliderLayerObject.SetActive(true);
                    _fullRewardBoxLayerObject.SetActive(true);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
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

            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", _currentIdleRewardItemList)).Forget();

            // 방치 보상 데이터 갱신
            UserDataManager.Instance.RefreshLastRewardGetTime();

            // 퀘스트 데이터 갱신
            UserDataManager.Instance.SetUserQuestActionCount(QuestType.GET_IDLE_REWARD, 1, true, true);

            // temp - 일단은 off
            OnClickCloseButton();

            // 메인 로비 갱신
            var battleReadyMain = SceneUILayerManager.Instance.GetUILayer<BattleReadyMain>();
            if (battleReadyMain != null)
            {
                battleReadyMain.RefreshUI(LobbyMainRefreshType.IDLE_REWARD);
            }
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearSlot()
        {
            BMUtil.RemoveChildObjects(_rewardGridLayerObject.transform);

            _currentIdleRewardItemSlotList.Clear();
        }
    }
}
