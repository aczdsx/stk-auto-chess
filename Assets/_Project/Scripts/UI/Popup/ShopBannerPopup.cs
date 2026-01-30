using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using R3;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class ShopBannerPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _closeButton;

        [Space(10)] 
        [SerializeField] private List<ShopBannerLayer> _bannerLayerList;

        private ShopInfo _specShopData;
        private ShopBanner _specShopBannerData;
        
        protected override void Awake()
        {
            base.Awake();

            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
            
            int shopID = (int)param;
            
            _specShopData = SpecDataManager.Instance.GetShopData(shopID);
            _specShopBannerData = SpecDataManager.Instance.GetShopBannerData(shopID);

            ActiveBannerLayer();
        }

        private void ActiveBannerLayer()
        {
            if (_specShopData == null) return;
            if (_specShopBannerData == null) return;

            ClearPopup();

            foreach (var bannerLayer in _bannerLayerList)
            {
                if (bannerLayer.ShopID != _specShopData.shop_id) continue;

                bannerLayer.gameObject.SetActive(true);
                bannerLayer.SetShopBannerLayer(this);

                var shopPurchaseData = ClientShopPurchaseData.Get();
                shopPurchaseData.SetShopBannerShowCount(_specShopBannerData.shop_id, 1, true);

                break;
            }
        }
        
        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearPopup()
        {
            foreach (var bannerLayerObject in _bannerLayerList)
            {
                bannerLayerObject.gameObject.SetActive(false);
            }
        }
    }   
}
