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

            // CharacterModel 갱신
            if (resp != null && resp.IsSuccess && resp.Characters != null)
            {
                ServerDataManager.Instance.Character.SetCharacters(resp.Characters);
            }

            return resp;
        }

        /// <summary>
        /// 특정 캐릭터 정보 가져오기
        /// </summary>
        public async UniTask<CharacterGetResponse> GetCharacterAsync(uint characterId, CancellationToken cancellationToken = default)
        {
            CharacterGetResponse resp = await ExecuteAsync(
                ServiceClient.GetAsync,
                new CharacterGetRequest { CharacterId = characterId },
                cancellationToken: cancellationToken
            );

            // CharacterModel 갱신
            if (resp != null && resp.IsSuccess && resp.Character != null)
            {
                ServerDataManager.Instance.Character.UpdateCharacter(resp.Character);
            }

            return resp;
        }

        /// <summary>
        /// 캐릭터 조각 교환을 통한 캐릭터 생성
        /// </summary>
        public async UniTask<CharacterCreateResponse> CreateAsync(uint characterId, CancellationToken cancellationToken = default)
        {
            CharacterCreateResponse resp = await ExecuteAsync(
                ServiceClient.CreateAsync,
                new CharacterCreateRequest { CharacterId = characterId },
                cancellationToken: cancellationToken
            );

            if (resp != null && resp.IsSuccess)
            {
                // CharacterModel 갱신
                if (resp.Character != null)
                {
                    ServerDataManager.Instance.Character.UpdateCharacter(resp.Character);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas != null && resp.CurrencyDeltas.Count > 0)
                {
                    ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }
            }

            return resp;
        }

        /// <summary>
        /// 캐릭터 레벨업
        /// </summary>
        public async UniTask<CharacterLevelUpResponse> LevelUpAsync(uint characterId, CancellationToken cancellationToken = default)
        {
            CharacterLevelUpResponse resp = await ExecuteAsync(
                ServiceClient.LevelUpAsync,
                new CharacterLevelUpRequest { CharacterId = characterId },
                cancellationToken: cancellationToken
            );

            if (resp != null && resp.IsSuccess)
            {
                // CharacterModel 갱신
                if (resp.Character != null)
                {
                    ServerDataManager.Instance.Character.UpdateCharacter(resp.Character);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas != null && resp.CurrencyDeltas.Count > 0)
                {
                    ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }
            }

            return resp;
        }

        /// <summary>
        /// 캐릭터 초월
        /// </summary>
        public async UniTask<CharacterTranscendResponse> TranscendAsync(uint characterId, CancellationToken cancellationToken = default)
        {
            CharacterTranscendResponse resp = await ExecuteAsync(
                ServiceClient.TranscendAsync,
                new CharacterTranscendRequest { CharacterId = characterId },
                cancellationToken: cancellationToken
            );

            if (resp != null && resp.IsSuccess)
            {
                // CharacterModel 갱신
                if (resp.Character != null)
                {
                    ServerDataManager.Instance.Character.UpdateCharacter(resp.Character);
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
