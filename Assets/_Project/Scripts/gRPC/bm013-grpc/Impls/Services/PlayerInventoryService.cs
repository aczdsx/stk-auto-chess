using System.Threading;
using CookApps.NetLite;
using Tech.Hive.V1;
using Cysharp.Threading.Tasks;

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
            PlayerInventoryListResponse resp = await ExecuteAsync(
                ServiceClient.ListAsync,
                new PlayerInventoryListRequest(),
                cancellationToken: cancellationToken
            );
            return resp;
        }
        
        
    }
}
