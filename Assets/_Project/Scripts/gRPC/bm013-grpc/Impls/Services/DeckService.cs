using System.Collections.Generic;
using System.Threading;
using CookApps.NetLite;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.DeckService.DeckServiceClient))]
    public partial class DeckService
    {
        /// <summary>
        /// 캐릭터 배치 덱 목록 조회
        /// </summary>
        public async UniTask<DeckListResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            DeckListResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ListAsync,
                new DeckListRequest(),
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true })
            {
                ServerDataManager.Instance.Deck.SetDecks(resp.DeckList);
            }

            return resp;
        }

        /// <summary>
        /// 캐릭터 배치 덱 조회
        /// </summary>
        public async UniTask<DeckGetResponse> GetAsync(uint deckSlotId, CancellationToken cancellationToken = default)
        {
            DeckGetResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.GetAsync,
                new DeckGetRequest { DeckSlotId = deckSlotId },
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true, Deck: not null })
            {
                ServerDataManager.Instance.Deck.UpdateDeck(resp.Deck);
            }

            return resp;
        }

        /// <summary>
        /// 캐릭터 배치 덱 저장
        /// </summary>
        public async UniTask<DeckSaveResponse> SaveAsync(
            uint deckSlotId,
            string deckName,
            IEnumerable<DeckCharacterPlacement> characterPlacements,
            IEnumerable<DeckTacticPlacement> tacticPlacements = null,
            Google.Protobuf.ByteString clientData = null,
            CancellationToken cancellationToken = default)
        {
            var request = new DeckSaveRequest
            {
                DeckSlotId = deckSlotId,
                DeckName = deckName ?? string.Empty
            };

            if (characterPlacements is not null)
            {
                request.CharacterPlacements.AddRange(characterPlacements);
            }

            if (tacticPlacements is not null)
            {
                request.TacticPlacements.AddRange(tacticPlacements);
            }

            if (clientData is not null)
            {
                request.ClientData = clientData;
            }

            DeckSaveResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.SaveAsync,
                request,
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true, Deck: not null })
            {
                ServerDataManager.Instance.Deck.UpdateDeck(resp.Deck);
            }

            return resp;
        }

        /// <summary>
        /// 캐릭터 배치 덱 삭제
        /// </summary>
        public async UniTask<DeckDeleteResponse> DeleteAsync(uint deckSlotId, CancellationToken cancellationToken = default)
        {
            DeckDeleteResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.DeleteAsync,
                new DeckDeleteRequest { DeckSlotId = deckSlotId },
                cancellationToken: cancellationToken
            );

            if (resp is { IsSuccess: true })
            {
                ServerDataManager.Instance.Deck.RemoveDeck(deckSlotId);
            }

            return resp;
        }
    }
}
