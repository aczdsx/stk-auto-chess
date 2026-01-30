using System;
using System.Threading;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class ShopBannerLayer : CachedMonoBehaviour
    {
        [SerializeField] private int shopID;

        [Space(10)] 
        [SerializeField] private CAButton _purchaseButton;
        [SerializeField] private TextMeshProUGUI _remainTimeText;

        private ShopBannerPopup _parentShopBannerPopup;

        private ShopBannerData _currentShopBannerData;
        private ClientShopPurchaseData _shopPurchaseData;
        
        private CancellationTokenSource _unitaskCancelToken = new CancellationTokenSource();

        private bool isAvailPurchase = true;
        
        public int ShopID => shopID;

        private void Awake()
        {
            _purchaseButton?.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickPurchaseButton()).AddTo(this);
        }

        public void SetShopBannerLayer(ShopBannerPopup parentPopup)
        {
            if (parentPopup == null) return;

            _parentShopBannerPopup = parentPopup;
            _shopPurchaseData = ClientShopPurchaseData.Get();
            _currentShopBannerData = _shopPurchaseData.GetShopBannerData(ShopID);

            SetPurchaseRemainTime();
        }

        private async void SetPurchaseRemainTime()
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
            TimeSpan remainTimeSpan = TimeManager.Instance.GetTimeSpanFromTarget(_currentShopBannerData.EndPurchaseTimestamp);

            try
            {
                while (remainTimeSpan.TotalSeconds > 0)
                {
                    _remainTimeText.text = LanguageManager.Instance.GetRemainTimeText(remainTimeSpan);

                    await UniTask.Delay(1000, cancellationToken: cancelToken);

                    remainTimeSpan = TimeManager.Instance.GetTimeSpanFromTarget(_currentShopBannerData.EndPurchaseTimestamp);
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
        
        private void OnClickPurchaseButton()
        {
            if (_currentShopBannerData == null) return;

            if (_shopPurchaseData.CheckPurchaseLimitCount(_currentShopBannerData.ShopId) == false)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_PURCHASE_COUNT_OVER");
                return;
            }

            if (_shopPurchaseData.CheckValidShopTime(_currentShopBannerData.ShopId) == false)
            {
                ToastManager.Instance.ShowToastByTokenKey("PURCHASE_TIME_OVER_ALERT");
                return;
            }

            // todo..구매 로직 추가
        }
    }
}