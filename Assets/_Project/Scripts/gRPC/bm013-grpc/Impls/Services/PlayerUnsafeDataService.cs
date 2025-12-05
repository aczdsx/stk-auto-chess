using System.Threading;
using CookApps.NetLite;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.PlayerUnsafeDataService.PlayerUnsafeDataServiceClient))]
    public partial class PlayerUnsafeDataService
    {
        /// <summary>
        /// 플레이어 설정 등 중요하지 않은 데이터 조회
        /// </summary>
        public async UniTask<PlayerUnsafeDataGetResponse> GetAsync(CancellationToken cancellationToken = default)
        {
            PlayerUnsafeDataGetResponse resp = await ExecuteAsync(
                ServiceClient.GetAsync,
                new PlayerUnsafeDataGetRequest(),
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 플레이어 설정 등 중요하지 않은 데이터 수정 (덮어쓰기)
        /// </summary>
        public async UniTask<PlayerUnsafeDataSetResponse> SetAsync(ByteString data, CancellationToken cancellationToken = default)
        {
            PlayerUnsafeDataSetResponse resp = await ExecuteAsync(
                ServiceClient.SetAsync,
                new PlayerUnsafeDataSetRequest { Data = data },
                cancellationToken: cancellationToken
            );
            return resp;
        }
    }
}
