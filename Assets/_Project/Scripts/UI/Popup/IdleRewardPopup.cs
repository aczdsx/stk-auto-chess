using System;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class IdleRewardPopup : UILayerPopupBase
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
            _getRewardButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickGetRewardButtonAsync(), AwaitOperation.Drop)
                .AddTo(this);
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
            string maxTimeGuideString = LanguageManager.Instance.GetDefaultText("UI_IDLE_REWARD_MAX_TIME_GUIDE");
            int maxMinute = IdleRewardHelper.GetMaxTimeLimitMinutes();
            _maxTimeGuideText.text = string.Format(maxTimeGuideString, maxMinute / 60);

            RefreshIdleRewardData();

            await CalculateIdleRewardTime();
        }

        private void RefreshIdleRewardData()
        {
            _currentIdleRewardItemList = UserDataManager.Instance.GetCurrentIdleRewardItemList();

            // 남은 시간 데이터 세팅
            _accTimeGuideText.text = IdleRewardHelper.FormatElapsedTime();

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

            int maxTimeLimitMinute = IdleRewardHelper.GetMaxTimeLimitMinutes();
            int refreshMinute = IdleRewardHelper.GetElapsedTime().Minutes;

            try
            {
                while (!IdleRewardHelper.IsFull())
                {
                    var elapsed = IdleRewardHelper.GetElapsedTime();

                    _accTimeGuideText.text = IdleRewardHelper.FormatElapsedTime();
                    _accTimeSlider.maxValue = maxTimeLimitMinute;
                    _accTimeSlider.value = (float)elapsed.TotalMinutes;
                    _accTimeSliderText.text = IdleRewardHelper.FormatSliderText();

                    await UniTask.Delay(1000, cancellationToken: token);

                    if (refreshMinute != IdleRewardHelper.GetElapsedTime().Minutes)
                    {
                        RefreshIdleRewardData();
                        refreshMinute = IdleRewardHelper.GetElapsedTime().Minutes;
                    }
                }

                // 최대 시간 도달 처리
                _accTimeGuideText.text = IdleRewardHelper.FormatElapsedTime();
                _accTimeSliderText.text = IdleRewardHelper.FormatSliderText();
                _accTimeSlider.maxValue = maxTimeLimitMinute;
                _accTimeSlider.value = maxTimeLimitMinute;

                _fullRewardSliderLayerObject.SetActive(true);
                _fullRewardBoxLayerObject.SetActive(true);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private async UniTask OnClickGetRewardButtonAsync()
        {
            RefreshIdleRewardData();

            // 수령 가능한 보상이 있는지 확인
            if (_currentIdleRewardItemList == null || _currentIdleRewardItemList.Count <= 0)
            {
                return;
            }

            // 서버에 보상 수령 요청
            var response = await NetManager.Instance.Elpis.ClaimSimulationRewardAsync();

            if (response == null || !response.IsSuccess)
            {
                return;
            }

            // 보상 결과 표시 (CurrencyDeltas에서 RewardItem 리스트 생성)
            List<RewardItem> rewardItemList = new List<RewardItem>();
            for (int i = 0; i < response.CurrencyDeltas.Count; i++)
            {
                var delta = response.CurrencyDeltas[i];
                if (delta.Delta > 0)
                {
                    rewardItemList.Add(new RewardItem((int)delta.ItemId, (int)delta.Delta));
                }
            }

            await SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList), null);

            // 메인 로비 갱신 (팝업 닫기 전에 호출)
            var battleReadyMain = SceneUILayerManager.Instance.GetUILayer<BattleReadyMain>();
            if (battleReadyMain != null)
            {
                battleReadyMain.RefreshUI(LobbyMainRefreshType.IDLE_REWARD);
            }

            OnClickCloseButton();
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
