using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public class ShopPurchaseManager : Singleton<ShopPurchaseManager>
    {
        //  현재 상점 배너의 상태를 체크 (팝업을 띄울지 여부)
        public void ShowShopBannerPopup(ShopBannerShowType showType)
        {
            // 24.09.26 - 상점 배너 팝업 임시 off
            return;

            var shopPurchaseData = ClientShopPurchaseData.Get();
            var shopBannerList = shopPurchaseData.GetAllShopBannerDataList();

            foreach (var shopBannerData in shopBannerList)
            {
                var specShopBannerData = SpecDataManager.Instance.GetShopBannerData(shopBannerData.ShopId);

                if (specShopBannerData == null) continue;
                if (shopBannerData.ShowCount > 0) continue;     // 자동 배너 팝업은 1회만 보여주는것을 디폴트로 함
                if (shopBannerData.ShopBannerStateType == ShopBannerStateType.INACTIVE) continue;
                if (specShopBannerData.shop_banner_show_type != showType) continue;

                // 팝업 띄우기
                SceneUILayerManager.Instance.PushUILayerAsync<ShopBannerPopup>(shopBannerData.ShopId).Forget();

                break;
            }
        }

        // 현재 상점 배너의 전체 상태를 체크
        public void UpdateShopBannerConditionValue(ShopBannerConditionType conditionType, int conditionKey, int value, bool isAdd)
        {
            // 24.09.26 - 상점 배너 팝업 임시 off
            return;

            var shopPurchaseData = ClientShopPurchaseData.Get();
            var shopBannerList = shopPurchaseData.GetAllShopBannerDataList();

            foreach (var shopBannerData in shopBannerList)
            {
                var specShopBannerData = SpecDataManager.Instance.GetShopBannerData(shopBannerData.ShopId);
                if (specShopBannerData != null &&
                    specShopBannerData.shop_banner_condition_type == conditionType &&
                    specShopBannerData.condition_key == conditionKey)
                {
                    shopPurchaseData.SetShopBannerConditionValue(shopBannerData.ShopId, value, isAdd);
                }
            }
        }
    }
}