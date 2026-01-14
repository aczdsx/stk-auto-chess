using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class GachaPickUpCharacterLayer : GachaBaseLayer
    {
        [Header("Button")]
        [SerializeField] private CAButton _gacha1Button;
        [SerializeField] private Image _gacha1ButtonCostImage;
        [SerializeField] private SpriteLoader _gacha1ButtonCostSpriteLoader;
        [SerializeField] private TextMeshProUGUI _gacha1ButtonCostText;

        [Space(10)]
        [SerializeField] private CAButton _gacha10Button;
        [SerializeField] private Image _gacha10ButtonCostImage;
        [SerializeField] private SpriteLoader _gacha10ButtonCostSpriteLoader;
        [SerializeField] private TextMeshProUGUI _gacha10ButtonCostText;

        [Space(10)]
        [SerializeField] private TextMeshProUGUI _remainTimeText;

        private CancellationTokenSource _unitaskCancelToken = new CancellationTokenSource();

        private void Awake()
        {
            _gacha1Button?.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickGacha1Button(), AwaitOperation.Drop).AddTo(this);
            _gacha10Button?.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickGacha10Button(), AwaitOperation.Drop).AddTo(this);
        }

        public void SetGachaLayer(GachaPopup parentPopup)
        {
            _parentGachaPopup = parentPopup;

            _specGachaDataOneTime = SpecDataManager.Instance.GetGachaData(CurrentGachaType, Defines.GACHA_1_TIME_COUNT);
            _specGachaDataTenTime = SpecDataManager.Instance.GetGachaData(CurrentGachaType, Defines.GACHA_10_TIME_COUNT);

            _gacha1ButtonCostSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(_specGachaDataOneTime.gacha_cost_item_id)).Forget();
            _gacha1ButtonCostText.text = $"x{_specGachaDataOneTime.gacha_cost}";

            _gacha10ButtonCostSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(_specGachaDataTenTime.gacha_cost_item_id)).Forget();
            _gacha10ButtonCostText.text = $"x{_specGachaDataTenTime.gacha_cost}";

            SetGachaRemainTime();
        }

        private async void SetGachaRemainTime()
        {
            try
            {
                _unitaskCancelToken.Cancel();
                _unitaskCancelToken = new CancellationTokenSource();

                await UpdateRemainTime(_unitaskCancelToken.Token).AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
        }

        private async UniTask UpdateRemainTime(CancellationToken cancelToken)
        {
            var endTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(_specGachaDataOneTime.end_at);

            TimeSpan remainTimeSpan = TimeManager.Instance.GetTimeSpanFromTarget(endTimeStamp);

            try
            {
                while (remainTimeSpan.TotalSeconds > 0)
                {
                    _remainTimeText.text = LanguageManager.Instance.GetRemainTimeText(remainTimeSpan);

                    await UniTask.Delay(1000, cancellationToken: cancelToken);

                    remainTimeSpan = TimeManager.Instance.GetTimeSpanFromTarget(endTimeStamp);
                }

                // 시간이 경과하였을 경우 처리
                if (remainTimeSpan.TotalSeconds <= 0)
                {
                    _remainTimeText.text = LanguageManager.Instance.GetDefaultText("PURCHASE_TIME_OVER_ALERT");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
        }

        private async UniTask OnClickGacha1Button()
        {
            if (_specGachaDataOneTime == null) return;

            await ProcessCharacterGacha(GachaCountType.ONE);
        }

        private async UniTask OnClickGacha10Button()
        {
            if (_specGachaDataTenTime == null) return;

            await ProcessCharacterGacha(GachaCountType.TEN);
        }
    }
}