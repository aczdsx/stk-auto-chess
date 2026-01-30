using System.Threading;
using CookApps.NetLite;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.PlayerInventoryService.PlayerInventoryServiceClient))]
    public partial class PlayerInventoryService
    {
        /// <summary>
        /// 인벤토리 목록 가져오기
        /// </summary>
        public async UniTask<PlayerInventoryListResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            PlayerInventoryListResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ListAsync,
                new PlayerInventoryListRequest(),
                cancellationToken: cancellationToken
            );

            // InventoryModel 갱신
            if (resp is { IsSuccess: true, ItemList: not null })
            {
                var inventoryModel = ServerDataManager.Instance.Inventory;
                for (var i = 0; i < resp.ItemList.Count; i++)
                {
                    var item = resp.ItemList[i];
                    inventoryModel.SetCurrency(item.ItemId, item.Count, item.Metadata);
                }
            }

            return resp;
        }

        /// <summary>
        /// 인벤토리 가져오기
        /// </summary>
        public async UniTask<PlayerInventoryListResponse> GetAsync(uint currencyId, CancellationToken cancellationToken = default)
        {
            PlayerInventoryListResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ListAsync,
                new PlayerInventoryListRequest { ItemIds = { currencyId } },
                cancellationToken: cancellationToken
            );

            // InventoryModel 갱신
            if (resp is { IsSuccess: true, ItemList: not null })
            {
                var inventoryModel = ServerDataManager.Instance.Inventory;
                for (var i = 0; i < resp.ItemList.Count; i++)
                {
                    var item = resp.ItemList[i];
                    inventoryModel.SetCurrency(item.ItemId, item.Count, item.Metadata);
                }
            }

            return resp;
        }
    }
}
