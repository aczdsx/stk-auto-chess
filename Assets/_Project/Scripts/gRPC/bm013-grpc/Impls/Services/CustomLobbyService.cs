using System.Threading;
using CookApps.NetLite;
using Tech.Hive.V1;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.CustomLobbyService.CustomLobbyServiceClient))]
    public partial class CustomLobbyService
    {
        /// <summary>
        /// 내 정보 조회
        /// </summary>
        public async UniTask<CustomLobbyGetMyPlayerDataResponse> GetMyPlayerDataAsync(CancellationToken cancellationToken = default)
        {
            CustomLobbyGetMyPlayerDataResponse resp = await ExecuteAsync(
                ServiceClient.GetMyPlayerDataAsync,
                new CustomLobbyGetMyPlayerDataRequest(),
                cancellationToken: cancellationToken
            );

            // PlayerDataModel 갱신
            if (resp != null && resp.IsSuccess && resp.Data != null)
            {
                ServerDataManager.Instance.PlayerData.SetPlayerData(resp.Data);
            }

            return resp;
        }

        /// <summary>
        /// 대표 캐릭터 설정
        /// </summary>
        public async UniTask<CustomLobbySetRepresentativeCharacterResponse> SetRepresentativeCharacterAsync(uint characterId, CancellationToken cancellationToken = default)
        {
            CustomLobbySetRepresentativeCharacterResponse resp = await ExecuteAsync(
                ServiceClient.SetRepresentativeCharacterAsync,
                new CustomLobbySetRepresentativeCharacterRequest { CharacterId = characterId.ToString() },
                cancellationToken: cancellationToken
            );

            // 성공 시 플레이어 데이터 다시 가져오기
            if (resp != null && resp.IsSuccess)
            {
                await GetMyPlayerDataAsync(cancellationToken);
            }

            return resp;
        }

        /// <summary>
        /// 기타 보상 수령
        /// </summary>
        public async UniTask<CustomLobbyClaimOtherRewardResponse> ClaimOtherRewardAsync(uint rewardId, CancellationToken cancellationToken = default)
        {
            CustomLobbyClaimOtherRewardResponse resp = await ExecuteAsync(
                ServiceClient.ClaimOtherRewardAsync,
                new CustomLobbyClaimOtherRewardRequest { RewardId = rewardId },
                cancellationToken: cancellationToken
            );

            // 서버 응답이 없을 경우 spec에서 읽어서 fallback 처리
            if (resp == null || resp.Status == null)
            {
                resp = CreateFallbackRewardResponse(rewardId);
            }

            // 통화 변화 적용
            if (resp != null && resp.IsSuccess && resp.CurrencyDeltas != null && resp.CurrencyDeltas.Count > 0)
            {
                ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
            }

            return resp;
        }

        private CustomLobbyClaimOtherRewardResponse CreateFallbackRewardResponse(uint rewardId)
        {
            var resp = new CustomLobbyClaimOtherRewardResponse
            {
                Status = new ResponseStatus { Code = 200 }
            };

            var rewardInfoList = SpecDataManager.Instance.GetSpecRewardInfoList((int)rewardId);
            if (rewardInfoList != null)
            {
                for (int i = 0; i < rewardInfoList.Count; i++)
                {
                    var rewardInfo = rewardInfoList[i];
                    resp.Rewards.Add(new Reward
                    {
                        ItemId = (uint)rewardInfo.item_id,
                        Count = (ulong)rewardInfo.item_count
                    });
                }
            }

            return resp;
        }
    }
}
