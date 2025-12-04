/*
 * Copyright (c) CookApps.
 */

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CookApps.NetLite.Initialize;
using Tech.Hive.V1;

namespace CookApps.NetLite.Feat.Grpc
{
    [GrpcService(typeof(ShopService.ShopServiceClient))]
    public partial class GrpcShopService
    {
        private readonly NetLiteInitializeParam _param;

        public GrpcShopService(NetLiteInitializeParam param)
        {
            _param = param;
        }

        /// <summary>
        /// IAP 결제를 통한 상품 구매
        /// </summary>
        /// <param name = "shopId">결제 shop id (SpecData의 ServerShop Id)</param>
        /// <param name = "receipt">구매 영수증</param>
        /// <param name = "orderId">구매 주문 번호</param>
        /// <param name = "currencyPrice">현지 가격</param>
        /// <param name = "currencyCode">가격 단위 (KRW, USD...)</param>
        /// <returns>ShopPurchaseWithIapResponse 객체</returns>
        public async Task<ShopPurchaseWithIapResponse> PurchaseWithIapAsync(string shopId, string receipt, string orderId, double currencyPrice, string currencyCode, CancellationToken cancellationToken = default)
        {
            ShopPurchaseWithIapRequest request = new();

#if UNITY_EDITOR
            request.Store = 0;
#else
            request.Store = (uint)_param.Store;
#endif
            request.Id = shopId;
            request.Receipt = receipt;
            request.OrderId = orderId;
            request.CurrencyPrice = currencyPrice.ToString(CultureInfo.InvariantCulture);
            request.CurrencyCode = currencyCode;
            ShopPurchaseWithIapResponse resp = null; // await ExecuteAsync(ServiceClient.PurchaseWithIapAsync, request, cancellationToken: cancellationToken);
            return resp;
        }
    }
}
