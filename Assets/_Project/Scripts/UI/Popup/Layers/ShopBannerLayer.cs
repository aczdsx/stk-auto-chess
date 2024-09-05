using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
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

        private UserShopBannerData _currentUserShopBannerData;
        
        private CancellationTokenSource _unitaskCancelToken = new CancellationTokenSource();

        private bool isAvailPurchase = true;
        
        public int ShopID => shopID;

        private void OnEnable()
        {
            _purchaseButton?.onClick.AddListener(OnClickPurchaseButton);
        }
        
        private void OnDisable()
        {
            _purchaseButton?.onClick.RemoveListener(OnClickPurchaseButton);
        }

        public void SetShopBannerLayer(ShopBannerPopup parentPopup)
        {
            if (parentPopup == null) return;
            
            _parentShopBannerPopup = parentPopup;
            _currentUserShopBannerData = UserDataManager.Instance.GetShopBannerData(ShopID);

            
            
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
            TimeSpan remainTimeSpan = TimeManager.Instance.GetTimeSpanFromTarget(_currentUserShopBannerData.EndPurchaseTimestamp);

            try
            {
                while (remainTimeSpan.TotalSeconds > 0)
                {
                    _remainTimeText.text = LanguageManager.Instance.GetRemainTimeText(remainTimeSpan);

                    await UniTask.Delay(1000, cancellationToken: cancelToken);

                    remainTimeSpan = TimeManager.Instance.GetTimeSpanFromTarget(_currentUserShopBannerData.EndPurchaseTimestamp);
                }

                // 시간이 경과하였을 경우 처리
                if (remainTimeSpan.TotalSeconds <= 0)
                {
                    _remainTimeText.text = LanguageManager.Instance.GetLanguageText("PURCHASE_TIME_OVER_ALERT");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
        }
        
        private void OnClickPurchaseButton()
        {
            if (_currentUserShopBannerData == null) return;

            if (UserDataManager.Instance.CheckValidShopTime(_currentUserShopBannerData.ShopId) == false)
            {
                ToastManager.Instance.ShowToastByTokenKey("PURCHASE_TIME_OVER_ALERT");
                return;
            }

            // todo..구매 로직 추가
        }
    }
}