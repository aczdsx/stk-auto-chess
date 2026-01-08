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
            if (resp?.Status?.Code == 0 && resp.Data != null)
            {
                ServerDataManager.Instance.PlayerData.SetPlayerData(resp.Data);
            }

            return resp;
        }

        /// <summary>
        /// 대표 캐릭터 설정
        /// </summary>
        public async UniTask<CustomLobbySetRepresentativeCharacterResponse> SetRepresentativeCharacterAsync(string characterInstanceId, CancellationToken cancellationToken = default)
        {
            CustomLobbySetRepresentativeCharacterResponse resp = await ExecuteAsync(
                ServiceClient.SetRepresentativeCharacterAsync,
                new CustomLobbySetRepresentativeCharacterRequest { CharacterId = characterInstanceId },
                cancellationToken: cancellationToken
            );

            // 성공 시 플레이어 데이터 다시 가져오기
            if (resp?.Status?.Code == 0)
            {
                await GetMyPlayerDataAsync(cancellationToken);
            }

            return resp;
        }
    }
}
