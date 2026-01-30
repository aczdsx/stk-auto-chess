using System.Threading;
using CookApps.NetLite;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.GachaService.GachaServiceClient))]
    public partial class GachaService
    {
        /// <summary>
        /// 가챠 목록 조회
        /// </summary>
        public async UniTask<GachaListResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            GachaListResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ListAsync,
                new GachaListRequest(),
                cancellationToken: cancellationToken
            );

            return resp;
        }

        /// <summary>
        /// 가챠 실행
        /// </summary>
        public async UniTask<DrawGachaResponse> DrawAsync(string gachaId, CancellationToken cancellationToken = default)
        {
            DrawGachaResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.DrawAsync,
                new DrawGachaRequest { GachaId = gachaId },
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true, CurrencyDeltas: { Count: > 0 } })
            {
                ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
            }

            return resp;
        }
    }
}