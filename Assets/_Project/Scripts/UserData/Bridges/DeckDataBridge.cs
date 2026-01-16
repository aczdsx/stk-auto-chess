using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 덱 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// </summary>
    public class DeckDataBridge : DataBridgeBase
    {
        private DeckModel Model;

        // Public Observable 노출
        public Observable<Unit> OnChanged;
        public Observable<DeckData> OnDeckAdded;
        public Observable<DeckData> OnDeckUpdated;
        public Observable<uint> OnDeckRemoved;

        public DeckDataBridge()
        {
            Model = ServerDataManager.Instance.Deck;
            OnChanged = Model.OnChanged;
            OnDeckAdded = Model.OnDeckAdded;
            OnDeckUpdated = Model.OnDeckUpdated;
            OnDeckRemoved = Model.OnDeckRemoved;
        }

        #region 덱 조회

        /// <summary>
        /// 덱 가져오기
        /// </summary>
        public DeckData GetDeck(uint deckSlotId)
        {
            return Model?.GetDeck(deckSlotId);
        }

        /// <summary>
        /// 모든 덱 가져오기
        /// </summary>
        public void GetAllDecks(List<DeckData> output)
        {
            Model?.GetAllDecks(output);
        }

        /// <summary>
        /// 덱 개수
        /// </summary>
        public int DeckCount => Model?.DeckCount ?? 0;

        /// <summary>
        /// 덱 존재 여부
        /// </summary>
        public bool HasDeck(uint deckSlotId)
        {
            return Model?.HasDeck(deckSlotId) ?? false;
        }

        /// <summary>
        /// 특정 덱의 캐릭터 배치 목록 가져오기
        /// </summary>
        public void GetCharacterPlacements(uint deckSlotId, List<DeckCharacterPlacement> output)
        {
            Model?.GetCharacterPlacements(deckSlotId, output);
        }

        /// <summary>
        /// 특정 덱의 전술 배치 목록 가져오기
        /// </summary>
        public void GetTacticPlacements(uint deckSlotId, List<DeckTacticPlacement> output)
        {
            Model?.GetTacticPlacements(deckSlotId, output);
        }

        #endregion
    }
}
