using System.Threading;
using CookApps.NetLite;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.EventService.EventServiceClient))]
    public partial class EventService
    {
        /// <summary>
        /// 이벤트 목록 조회
        /// </summary>
        public async UniTask<EventListResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            EventListResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ListAsync,
                new EventListRequest(),
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp is { IsSuccess: true })
            {
                ServerDataManager.Instance.Event.SetEvents(resp.Events);
            }

            return resp;
        }

        /// <summary>
        /// 이벤트 진행 사항 업데이트
        /// </summary>
        public async UniTask<EventUpdateProgressResponse> UpdateProgressAsync(uint eventId, uint addCurrentCount, CancellationToken cancellationToken = default)
        {
            EventUpdateProgressResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.UpdateProgressAsync,
                new EventUpdateProgressRequest
                {
                    EventId = eventId,
                    AddCurrentCount = addCurrentCount
                },
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp is { IsSuccess: true, Event: not null })
            {
                ServerDataManager.Instance.Event.UpdateEvent(resp.Event);
            }

            return resp;
        }

        /// <summary>
        /// 이벤트 보상 수령
        /// </summary>
        public async UniTask<EventClaimRewardResponse> ClaimRewardAsync(uint eventId, uint eventConditionId, CancellationToken cancellationToken = default)
        {
            EventClaimRewardResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ClaimRewardAsync,
                new EventClaimRewardRequest
                {
                    EventId = eventId,
                    EventConditionId = eventConditionId
                },
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true })
            {
                // 이벤트 데이터 갱신
                if (resp.Event != null)
                {
                    ServerDataManager.Instance.Event.UpdateEvent(resp.Event);
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
