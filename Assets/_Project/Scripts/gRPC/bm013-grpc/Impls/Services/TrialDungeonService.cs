using System.Threading;
using CookApps.NetLite;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.TrialDungeonService.TrialDungeonServiceClient))]
    public partial class TrialDungeonService
    {
        /// <summary>
        /// 현재 시련 던전 조회
        /// </summary>
        public async UniTask<TrialDungeonGetResponse> GetAsync(CancellationToken cancellationToken = default)
        {
            TrialDungeonGetResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.GetAsync,
                new TrialDungeonGetRequest(),
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true, TrialDungeon: not null })
            {
                ServerDataManager.Instance.TrialDungeon.SetTrialDungeon(resp.TrialDungeon);
            }

            return resp;
        }

        /// <summary>
        /// 시련 던전 입장
        /// </summary>
        public async UniTask<TrialDungeonEnterResponse> EnterAsync(CancellationToken cancellationToken = default)
        {
            TrialDungeonEnterResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.EnterAsync,
                new TrialDungeonEnterRequest(),
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true, TrialDungeon: not null })
            {
                ServerDataManager.Instance.TrialDungeon.SetTrialDungeon(resp.TrialDungeon);
            }

            return resp;
        }

        /// <summary>
        /// 시련 던전 클리어
        /// </summary>
        public async UniTask<TrialDungeonClearResponse> ClearAsync(
            string battleSessionId,
            bool isVictory,
            CancellationToken cancellationToken = default)
        {
            TrialDungeonClearResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ClearAsync,
                new TrialDungeonClearRequest
                {
                    BattleSessionId = battleSessionId,
                    IsVictory = isVictory
                },
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true })
            {
                // 시련 던전 데이터 갱신
                if ((resp.TrialDungeon, isVictory) is (not null, true))
                {
                    ServerDataManager.Instance.TrialDungeon.SetTrialDungeon(resp.TrialDungeon);
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
