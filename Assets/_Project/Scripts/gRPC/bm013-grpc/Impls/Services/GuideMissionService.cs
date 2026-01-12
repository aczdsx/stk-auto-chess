using System.Threading;
using CookApps.NetLite;
using Tech.Hive.V1;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.GuideMissionService.GuideMissionServiceClient))]
    public partial class GuideMissionService
    {
        /// <summary>
        /// 가이드 미션 조회
        /// </summary>
        public async UniTask<GuideMissionGetResponse> GetAsync(CancellationToken cancellationToken = default)
        {
            GuideMissionGetResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.GetAsync,
                new GuideMissionGetRequest(),
                cancellationToken: cancellationToken
            );

            // GuideMissionModel 갱신
            if (resp != null && resp.IsSuccess && resp.GuideMission != null)
            {
                ServerDataManager.Instance.GuideMission.SetGuideMission(resp.GuideMission);
            }

            return resp;
        }

        /// <summary>
        /// 가이드 미션 보상 수령
        /// </summary>
        public async UniTask<GuideMissionClaimRewardResponse> ClaimRewardAsync(uint guideMissionId, CancellationToken cancellationToken = default)
        {
            GuideMissionClaimRewardResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ClaimRewardAsync,
                new GuideMissionClaimRewardRequest { GuideMissionId = guideMissionId },
                cancellationToken: cancellationToken
            );

            if (resp != null && resp.IsSuccess)
            {
                // GuideMissionModel 갱신
                if (resp.GuideMission != null)
                {
                    ServerDataManager.Instance.GuideMission.SetGuideMission(resp.GuideMission);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas != null && resp.CurrencyDeltas.Count > 0)
                {
                    ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }
            }

            return resp;
        }
    }
}
