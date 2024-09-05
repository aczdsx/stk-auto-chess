using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/ShopBannerPopup.prefab")]
    public class ShopBannerPopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;

        [Space(10)] 
        [SerializeField] private List<ShopBannerLayer> _bannerLayerList;

        private SpecShop _specShopData;
        private SpecShopBanner _specShopBannerData;
        
        protected override void Awake()
        {
            base.Awake();

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
            
            foreach (var bannerLayerObject in _bannerLayerList)
            {
                if (bannerLayerObject.ShopID != _specShopData.shop_id) continue;
                
                bannerLayerObject.gameObject.SetActive(true);
                UserDataManager.Instance.SetShopBannerShowCount(_specShopBannerData.shop_id, 1, true, true);

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
