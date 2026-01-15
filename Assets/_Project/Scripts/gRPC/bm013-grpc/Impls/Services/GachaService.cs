using System.Threading;
using CookApps.NetLite;
using Tech.Hive.V1;
using Cysharp.Threading.Tasks;

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

            if (resp != null && resp.IsSuccess)
            {
                // 통화 변화 적용 (가챠 비용 차감 + 획득 아이템)
                if (resp.CurrencyDeltas != null && resp.CurrencyDeltas.Count > 0)
                {
                    ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }
            }

            return resp;
        }
    }
}