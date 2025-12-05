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
            BattleGetCurrentChapterResponse resp = await ExecuteAsync(
                ServiceClient.GetCurrentChapterAsync,
                new BattleGetCurrentChapterRequest(),
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 챕터 목록 가져오기
        /// </summary>
        public async UniTask<BattleListChapterResponse> ListChapterAsync(CancellationToken cancellationToken = default)
        {
            BattleListChapterResponse resp = await ExecuteAsync(
                ServiceClient.ListChapterAsync,
                new BattleListChapterRequest(),
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 스테이지 목록 가져오기
        /// </summary>
        public async UniTask<BattleListStageResponse> ListStageAsync(uint chapterId, CancellationToken cancellationToken = default)
        {
            BattleListStageResponse resp = await ExecuteAsync(
                ServiceClient.ListStageAsync,
                new BattleListStageRequest { ChapterId = chapterId },
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 전투 시작
        /// </summary>
        public async UniTask<BattleStartResponse> StartAsync(string stageId, CancellationToken cancellationToken = default)
        {
            BattleStartResponse resp = await ExecuteAsync(
                ServiceClient.StartAsync,
                new BattleStartRequest { StageId = stageId },
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 전투 종료
        /// </summary>
        public async UniTask<BattleEndResponse> EndAsync(string battleSessionId, BattleResult result, CancellationToken cancellationToken = default)
        {
            BattleEndResponse resp = await ExecuteAsync(
                ServiceClient.EndAsync,
                new BattleEndRequest {BattleSessionId = battleSessionId, Result = result },
                cancellationToken: cancellationToken
            );
            return resp;
        }
    }
}
