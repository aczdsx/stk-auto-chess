using System.Threading;
using CookApps.NetLite;
using Tech.Hive.V1;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.CharacterService.CharacterServiceClient))]
    public partial class CharacterService
    {
        
        /// <summary>
        /// 모든 캐릭터 목록 가져오기
        /// </summary>
        public async UniTask<CharacterListResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            CharacterListResponse resp = await ExecuteAsync(
                ServiceClient.ListAsync,
                new CharacterListRequest(),
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 특정 캐릭터 정보 가져오기
        /// </summary>
        public async UniTask<CharacterGetResponse> GetCharacterAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            CharacterGetResponse resp = await ExecuteAsync(
                ServiceClient.GetAsync,
                new CharacterGetRequest { InstanceId = instanceId },
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 캐릭터 레벨업
        /// </summary>
        public async UniTask<CharacterLevelUpResponse> LevelUpAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            CharacterLevelUpResponse resp = await ExecuteAsync(
                ServiceClient.LevelUpAsync,
                new CharacterLevelUpRequest { InstanceId = instanceId },
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 캐릭터 초월
        /// </summary>
        public async UniTask<CharacterTranscendResponse> TranscendAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            CharacterTranscendResponse resp = await ExecuteAsync(
                ServiceClient.TranscendAsync,
                new CharacterTranscendRequest { InstanceId = instanceId },
                cancellationToken: cancellationToken
            );
            return resp;
        }
    }
}
