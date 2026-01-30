using System.Threading;
using CookApps.NetLite;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

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
            CharacterListResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ListAsync,
                new CharacterListRequest(),
                cancellationToken: cancellationToken
            );

            // CharacterModel 갱신
            if (resp is { IsSuccess: true, Characters: not null })
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
            CharacterGetResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.GetAsync,
                new CharacterGetRequest { CharacterId = characterId },
                cancellationToken: cancellationToken
            );

            // CharacterModel 갱신
            if (resp is { IsSuccess: true, Character: not null })
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
            CharacterCreateResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.CreateAsync,
                new CharacterCreateRequest { CharacterId = characterId },
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true })
            {
                // CharacterModel 갱신
                if (resp.Character is not null)
                {
                    ServerDataManager.Instance.Character.UpdateCharacter(resp.Character);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas is { Count: > 0 })
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
            CharacterLevelUpResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.LevelUpAsync,
                new CharacterLevelUpRequest { CharacterId = characterId },
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true })
            {
                // CharacterModel 갱신
                if (resp.Character is not null)
                {
                    ServerDataManager.Instance.Character.UpdateCharacter(resp.Character);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas is { Count: > 0 })
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
            CharacterTranscendResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.TranscendAsync,
                new CharacterTranscendRequest { CharacterId = characterId },
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true })
            {
                // CharacterModel 갱신
                if (resp.Character is not null)
                {
                    ServerDataManager.Instance.Character.UpdateCharacter(resp.Character);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas is { Count: > 0 })
                {
                    ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }
            }

            return resp;
        }

        /// <summary>
        /// 캐릭터 돌파
        /// </summary>
        public async UniTask<CharacterExceedResponse> ExceedAsync(uint characterId, CancellationToken cancellationToken = default)
        {
            CharacterExceedResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ExceedAsync,
                new CharacterExceedRequest { CharacterId = characterId },
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true })
            {
                // CharacterModel 갱신
                if (resp.Character is not null)
                {
                    ServerDataManager.Instance.Character.UpdateCharacter(resp.Character);
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
