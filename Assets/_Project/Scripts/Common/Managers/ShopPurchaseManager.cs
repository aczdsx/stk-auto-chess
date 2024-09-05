using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class ShopPurchaseManager : Singleton<ShopPurchaseManager>
    {
        //  현재 상점 배너의 상태를 체크 (팝업을 띄울지 여부)
        public void ShowShopBannerPopup(ShopBannerShowType showType)
        {
            var userShopBannerList = UserDataManager.Instance.GetAllShopBannerDataList();
            
            foreach (var userShopBannerData in userShopBannerList)
            {
                var specShopBannerData = SpecDataManager.Instance.GetShopBannerData(userShopBannerData.ShopId);
                
                if (specShopBannerData == null) continue;
                if (userShopBannerData.ShowCount > 0) continue;     // 자동 배너 팝업은 1회만 보여주는것을 디폴트로 함
                if (userShopBannerData.ShopBannerStateType == (int)ShopBannerStateType.INACTIVE) continue;
                if (specShopBannerData.shop_banner_show_type != showType) continue;
                
                // 팝업 띄우기
                SceneUILayerManager.Instance.PushUILayerAsync<ShopBannerPopup>(userShopBannerData.ShopId).Forget();

                break;
            }
        }
        
        // 현재 상점 배너의 전체 상태를 체크
        public void UpdateShopBannerConditionValue(ShopBannerConditionType conditionType, int value, bool isAdd)
        {
            var userShopBannerList = UserDataManager.Instance.GetAllShopBannerDataList();

            bool needSave = false;
            foreach (var userShopBannerData in userShopBannerList)
            {
                var specShopBannerData = SpecDataManager.Instance.GetShopBannerData(userShopBannerData.ShopId);
                if (specShopBannerData != null && specShopBannerData.shop_banner_condition_type == conditionType)
                {
                    UserDataManager.Instance.SetShopBannerConditionValue(userShopBannerData.ShopId, value, isAdd, false);
                    needSave = true;
                }
            }

            if (needSave)
            {
                UserDataManager.Instance.SaveUserShopPurchaseData();
            }
        }
    }
}