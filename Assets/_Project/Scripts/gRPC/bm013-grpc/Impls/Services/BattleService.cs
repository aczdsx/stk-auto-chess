using System.Threading;
using CookApps.NetLite;
using Tech.Hive.V1;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.BattleService.BattleServiceClient))]
    public partial class BattleService
    {
        /// <summary>
        /// 현재 챕터 가져오기
        /// </summary>
        public async UniTask<BattleGetCurrentChapterResponse> GetCurrentChapterAsync(CancellationToken cancellationToken = default)
        {
            BattleGetCurrentChapterResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.GetCurrentChapterAsync,
                new BattleGetCurrentChapterRequest(),
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp != null && resp.IsSuccess)
            {
                ServerDataManager.Instance.Battle.SetCurrentChapter(resp.Chapter, resp.StageList);
            }

            return resp;
        }

        /// <summary>
        /// 챕터 목록 가져오기
        /// </summary>
        public async UniTask<BattleListChapterResponse> ListChapterAsync(CancellationToken cancellationToken = default)
        {
            BattleListChapterResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ListChapterAsync,
                new BattleListChapterRequest(),
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp != null && resp.IsSuccess)
            {
                ServerDataManager.Instance.Battle.SetChapters(
                    resp.ChapterList
                );
            }

            return resp;
        }

        /// <summary>
        /// 스테이지 목록 가져오기
        /// </summary>
        public async UniTask<BattleListStageResponse> ListStageAsync(uint chapterId, CancellationToken cancellationToken = default)
        {
            BattleListStageResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ListStageAsync,
                new BattleListStageRequest { ChapterId = chapterId },
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp != null && resp.IsSuccess)
            {
                ServerDataManager.Instance.Battle.SetStages(resp.StageList);
            }

            return resp;
        }

        /// <summary>
        /// 챕터 마일스톤 보상 수령
        /// </summary>
        public async UniTask<BattleClaimChapterMilestoneRewardResponse> ClaimChapterMilestoneRewardAsync(
            uint chapterId,
            uint milestoneRewardId,
            CancellationToken cancellationToken = default)
        {
            BattleClaimChapterMilestoneRewardResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ClaimChapterMilestoneRewardAsync,
                new BattleClaimChapterMilestoneRewardRequest
                {
                    ChapterId = chapterId,
                    MilestoneRewardId = milestoneRewardId
                },
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp != null && resp.IsSuccess)
            {
                // 통화 변화 적용
                if (resp.CurrencyDeltas != null && resp.CurrencyDeltas.Count > 0)
                {
                    ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }
            }

            return resp;
        }

        /// <summary>
        /// 전투 시작
        /// </summary>
        public async UniTask<BattleStartResponse> StartAsync(
            uint chapterId,
            uint stageId,
            uint deckSlotId,
            string[] observerSkillIds,
            CancellationToken cancellationToken = default)
        {
            var request = new BattleStartRequest
            {
                ChapterId = chapterId,
                StageId = stageId,
                DeckSlotId = deckSlotId,
            };
            request.ObserverSkillIds.AddRange(observerSkillIds);

            BattleStartResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.StartAsync,
                request,
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 전투 종료
        /// </summary>
        public async UniTask<BattleEndResponse> EndAsync(string battleSessionId, BattleResult result, CancellationToken cancellationToken = default)
        {
            BattleEndResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.EndAsync,
                new BattleEndRequest {BattleSessionId = battleSessionId, Result = result },
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp != null && resp.IsSuccess)
            {
                // 스테이지 진행 정보 업데이트
                if (resp.StageProgress != null)
                {
                    ServerDataManager.Instance.Battle.UpdateStageProgress(resp.StageProgress);
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
