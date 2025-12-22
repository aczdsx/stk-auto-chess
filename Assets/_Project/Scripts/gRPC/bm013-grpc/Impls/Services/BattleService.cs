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

            // 서버 응답으로 로컬 데이터 갱신
            if (resp != null && resp.Status.Code == 0)
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
            BattleListChapterResponse resp = await ExecuteAsync(
                ServiceClient.ListChapterAsync,
                new BattleListChapterRequest(),
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp != null && resp.Status.Code == 0)
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
            BattleListStageResponse resp = await ExecuteAsync(
                ServiceClient.ListStageAsync,
                new BattleListStageRequest { ChapterId = chapterId },
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp != null && resp.Status.Code == 0)
            {
                ServerDataManager.Instance.Battle.SetStages(resp.StageList);
            }

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

            // 서버 응답으로 로컬 데이터 갱신
            if (resp != null && resp.Status.Code == 0)
            {
                // 스테이지 진행 정보 업데이트
                if (resp.StageProgress != null)
                {
                    ServerDataManager.Instance.Battle.UpdateStageProgress(resp.StageProgress);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas != null && resp.CurrencyDeltas.Count > 0)
                {
                    ServerDataManager.Instance.Wallet.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }

                // 캐릭터 경험치 획득 반영
                if (resp.CharacterExpGains != null && resp.CharacterExpGains.Count > 0)
                {
                    foreach (var expGain in resp.CharacterExpGains)
                    {
                        var character = ServerDataManager.Instance.Character.GetCharacter(expGain.CharacterInstanceId);
                        if (character != null)
                        {
                            // 캐릭터 레벨 업데이트 (레벨이 변경된 경우)
                            if (character.Level != expGain.LevelAfter)
                            {
                                character.Level = expGain.LevelAfter;
                                ServerDataManager.Instance.Character.UpdateCharacter(character);
                            }
                        }
                    }
                }
            }

            return resp;
        }
    }
}
