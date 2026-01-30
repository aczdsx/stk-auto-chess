using System.Threading;
using CookApps.NetLite;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.QuestService.QuestServiceClient))]
    public partial class QuestService
    {
        /// <summary>
        /// 일일 퀘스트 목록 조회
        /// </summary>
        public async UniTask<QuestListDailyQuestResponse> ListDailyQuestAsync(uint? dateIndex = null, CancellationToken cancellationToken = default)
        {
            var request = new QuestListDailyQuestRequest();
            if (dateIndex.HasValue)
            {
                request.DateIndex = dateIndex.Value;
            }

            QuestListDailyQuestResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ListDailyQuestAsync,
                request,
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp is { IsSuccess: true })
            {
                ServerDataManager.Instance.Quest.SetQuests(resp.QuestList, resp.NextResetAt, resp.DateIndex);
            }

            return resp;
        }

        /// <summary>
        /// 퀘스트 보상 수령
        /// </summary>
        public async UniTask<QuestClaimQuestRewardResponse> ClaimQuestRewardAsync(uint questId, Tech.Hive.V1.QuestType questType, uint? dateIndex = null, CancellationToken cancellationToken = default)
        {
            var request = new QuestClaimQuestRewardRequest
            {
                QuestId = questId,
                QuestType = questType
            };
            if (dateIndex.HasValue)
            {
                request.DateIndex = dateIndex.Value;
            }

            QuestClaimQuestRewardResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ClaimQuestRewardAsync,
                request,
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true })
            {
                // 퀘스트 데이터 갱신
                if (resp.Quest is not null)
                {
                    ServerDataManager.Instance.Quest.UpdateQuest(resp.Quest);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas is { Count: > 0 })
                {
                    ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }
            }

            return resp;
        }
    }
}
