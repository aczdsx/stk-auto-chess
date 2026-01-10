using System;
using System.Collections.Generic;
using R3;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 덱 데이터 모델
    /// 서버의 DeckData 프로토콜을 래핑
    /// </summary>
    public class DeckModel
    {
        // 프로토콜 데이터 (서버에서 받은 원본)
        // Key: deck_slot_id
        private readonly Dictionary<uint, DeckData> _decks = new(8);

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<DeckData> OnDeckAdded = new();
        public readonly Subject<DeckData> OnDeckUpdated = new();
        public readonly Subject<uint> OnDeckRemoved = new();

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _decks.Clear();
            OnChanged.OnNext(Unit.Default);
        }

        #region 덱 조회

        /// <summary>
        /// 덱 가져오기
        /// </summary>
        public DeckData GetDeck(uint deckSlotId)
        {
            return _decks.TryGetValue(deckSlotId, out var deck) ? deck : null;
        }

        /// <summary>
        /// 모든 덱 가져오기 (메모리 할당 최소화)
        /// </summary>
        public void GetAllDecks(List<DeckData> output)
        {
            if (output == null) return;

            output.Clear();
            output.Capacity = Math.Max(output.Capacity, _decks.Count);

            foreach (var deck in _decks.Values)
            {
                output.Add(deck);
            }
        }

        /// <summary>
        /// 덱 개수
        /// </summary>
        public int DeckCount => _decks.Count;

        /// <summary>
        /// 덱 존재 여부
        /// </summary>
        public bool HasDeck(uint deckSlotId)
        {
            return _decks.ContainsKey(deckSlotId);
        }

        /// <summary>
        /// 특정 덱의 캐릭터 배치 목록 가져오기
        /// </summary>
        public void GetCharacterPlacements(uint deckSlotId, List<DeckCharacterPlacement> output)
        {
            if (output == null) return;

            output.Clear();

            if (_decks.TryGetValue(deckSlotId, out var deck))
            {
                for (int i = 0; i < deck.CharacterPlacements.Count; i++)
                {
                    output.Add(deck.CharacterPlacements[i]);
                }
            }
        }

        /// <summary>
        /// 특정 덱의 전술 배치 목록 가져오기
        /// </summary>
        public void GetTacticPlacements(uint deckSlotId, List<DeckTacticPlacement> output)
        {
            if (output == null) return;

            output.Clear();

            if (_decks.TryGetValue(deckSlotId, out var deck))
            {
                for (int i = 0; i < deck.TacticPlacements.Count; i++)
                {
                    output.Add(deck.TacticPlacements[i]);
                }
            }
        }

        #endregion

        #region 내부용 (서버 응답 처리)

        /// <summary>
        /// 서버 응답으로 덱 목록 설정 (내부용)
        /// </summary>
        internal void SetDecks(IReadOnlyList<DeckData> decks)
        {
            _decks.Clear();

            for (var i = 0; i < decks.Count; i++)
            {
                var deck = decks[i];
                _decks[deck.DeckSlotId] = deck;
            }

            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 단일 덱 업데이트 (서버 응답용)
        /// </summary>
        internal void UpdateDeck(DeckData deck)
        {
            if (deck == null) return;

            bool isNew = !_decks.ContainsKey(deck.DeckSlotId);
            _decks[deck.DeckSlotId] = deck;

            if (isNew)
                OnDeckAdded.OnNext(deck);
            else
                OnDeckUpdated.OnNext(deck);

            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 덱 제거 (서버 응답용)
        /// </summary>
        internal void RemoveDeck(uint deckSlotId)
        {
            if (_decks.Remove(deckSlotId))
            {
                OnDeckRemoved.OnNext(deckSlotId);
                OnChanged.OnNext(Unit.Default);
            }
        }

        #endregion

        #region 편의 메서드 (마이그레이션용)

        /// <summary>
        /// InGameType에 따른 배틀 덱 목록 가져오기
        /// </summary>
        public List<DeckCharacterPlacement> GetBattleDeckListByInGameType(InGameType inGameType)
        {
            // InGameType에 따른 덱 슬롯 ID 결정 로직
            uint deckSlotId = inGameType switch
            {
                InGameType.STAGE => 1,
                InGameType.TRIAL => 2,
                _ => 1
            };

            var result = new List<DeckCharacterPlacement>();
            GetCharacterPlacements(deckSlotId, result);
            return result;
        }

        /// <summary>
        /// 덱 전투력 계산
        /// </summary>
        public int GetDeckBattlePower(List<DeckCharacterPlacement> deckList)
        {
            if (deckList == null || deckList.Count == 0) return 0;

            double battlePower = 0;

            for (int i = 0; i < deckList.Count; i++)
            {
                var placement = deckList[i];
                var characterData = ServerDataManager.Instance.Character.GetCharacter(placement.CharacterId);
                if (characterData == null) continue;

                var characterStat = new CharacterStatData(
                    (int)characterData.CharacterId,
                    (int)characterData.Level,
                    GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()
                );
                battlePower += characterStat.GetAttrValueCP();
            }

            return (int)battlePower;
        }

        #endregion
    }
}
