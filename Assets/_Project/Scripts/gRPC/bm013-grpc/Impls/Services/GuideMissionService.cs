using System.Threading;
using CookApps.NetLite;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

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
            if (resp is { IsSuccess: true, GuideMission: not null })
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

            if (resp is { IsSuccess: true })
            {
                // GuideMissionModel 갱신
                if (resp.GuideMission is not null)
                {
                    ServerDataManager.Instance.GuideMission.SetGuideMission(resp.GuideMission);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas is { Count: > 0 })
                {
                    ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }
            }

            return resp;
        }

        /// <summary>
        /// 클라이언트 액션 완료 보고 (CLICK_ATTENDANCE 등 클라이언트에서만 알 수 있는 액션)
        /// </summary>
        public async UniTask<GuideMissionUpdateActionResponse> UpdateActionAsync(uint addCount = 1, CancellationToken cancellationToken = default)
        {
            GuideMissionUpdateActionResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.UpdateActionAsync,
                new GuideMissionUpdateActionRequest { AddCount = addCount },
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true, GuideMission: not null })
            {
                ServerDataManager.Instance.GuideMission.SetGuideMission(resp.GuideMission);
            }

            return resp;
        }
    }
}
