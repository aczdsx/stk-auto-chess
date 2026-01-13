using System.Collections.Generic;
using System.Threading;
using CookApps.NetLite;
using Tech.Hive.V1;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.CheatService.CheatServiceClient))]
    public partial class CheatService
    {
        /// <summary>
        /// 모든 캐릭터 획득
        /// </summary>
        public async UniTask<EarnAllCharactersResponse> EarnAllCharactersAsync(CancellationToken cancellationToken = default)
        {
            EarnAllCharactersResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.EarnAllCharactersAsync,
                new EarnAllCharactersRequest(),
                cancellationToken: cancellationToken
            );

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
        /// 재화 변경
        /// </summary>
        public async UniTask<ChangeCurrencyResponse> ChangeCurrencyAsync(
            IEnumerable<CurrencyDelta> currencyDeltas,
            CancellationToken cancellationToken = default)
        {
            var request = new ChangeCurrencyRequest();
            request.CurrencyDeltas.AddRange(currencyDeltas);

            ChangeCurrencyResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ChangeCurrencyAsync,
                request,
                cancellationToken: cancellationToken
            );

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
    }
}
