using System;
using System.Collections.Generic;
using R3;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 캐릭터 데이터 모델
    /// 서버의 CharacterData 프로토콜을 래핑
    /// Key: CharacterId (uint)
    /// </summary>
    public class CharacterModel
    {
        // 프로토콜 데이터 (서버에서 받은 원본)
        // Key: CharacterId
        private readonly Dictionary<uint, CharacterData> _characters = new(64);

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<CharacterData> OnCharacterAdded = new();
        public readonly Subject<CharacterData> OnCharacterUpdated = new();
        public readonly Subject<uint> OnCharacterRemoved = new();

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _characters.Clear();
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 캐릭터 가져오기 (CharacterId로 조회)
        /// </summary>
        public CharacterData GetCharacter(uint characterId)
        {
            return _characters.TryGetValue(characterId, out var character) ? character : null;
        }

        /// <summary>
        /// 모든 캐릭터 가져오기 (메모리 할당 최소화)
        /// </summary>
        public void GetAllCharacters(List<CharacterData> output)
        {
            if (output == null) return;

            output.Clear();
            output.Capacity = Math.Max(output.Capacity, _characters.Count);

            foreach (var character in _characters.Values)
            {
                output.Add(character);
            }
        }

        /// <summary>
        /// 캐릭터 개수
        /// </summary>
        public int CharacterCount => _characters.Count;

        /// <summary>
        /// 캐릭터 존재 여부
        /// </summary>
        public bool HasCharacter(uint characterId)
        {
            return _characters.ContainsKey(characterId);
        }

        /// <summary>
        /// 서버 응답으로 캐릭터 설정 (내부용)
        /// </summary>
        internal void SetCharacters(IReadOnlyList<CharacterData> characters)
        {
            _characters.Clear();

            for (var i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                if (character.CharacterId > 0)
                {
                    _characters[character.CharacterId] = character;
                }
            }

            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 단일 캐릭터 업데이트 (서버 응답용)
        /// </summary>
        internal void UpdateCharacter(CharacterData character)
        {
            if (character == null || character.CharacterId == 0)
            {
                Debug.LogError("[CharacterModel] Invalid character data");
                return;
            }

            bool isNew = !_characters.ContainsKey(character.CharacterId);
            _characters[character.CharacterId] = character;

            if (isNew)
                OnCharacterAdded.OnNext(character);
            else
                OnCharacterUpdated.OnNext(character);

            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 캐릭터 제거 (서버 응답용)
        /// </summary>
        internal void RemoveCharacter(uint characterId)
        {
            if (_characters.Remove(characterId))
            {
                OnCharacterRemoved.OnNext(characterId);
                OnChanged.OnNext(Unit.Default);
            }
        }

        /// <summary>
        /// 특정 조건의 캐릭터 필터링 (for문 사용, Linq 지양)
        /// </summary>
        public void GetCharactersByCondition(List<CharacterData> output, Func<CharacterData, bool> predicate)
        {
            if (output == null || predicate == null) return;

            output.Clear();

            foreach (var character in _characters.Values)
            {
                if (predicate(character))
                {
                    output.Add(character);
                }
            }
        }

        /// <summary>
        /// 모든 보유 캐릭터 ID 목록 가져오기
        /// </summary>
        public void GetOwnedCharacterIds(List<uint> output)
        {
            if (output == null) return;

            output.Clear();
            output.Capacity = Math.Max(output.Capacity, _characters.Count);

            foreach (var characterId in _characters.Keys)
            {
                output.Add(characterId);
            }
        }

        #region 호환 메서드 (마이그레이션용)

        /// <summary>
        /// CharacterUid로 캐릭터 가져오기 (Deck 배치용)
        /// CharacterUid = CharacterId
        /// </summary>
        public CharacterData GetCharacterByUid(uint characterUid)
        {
            return GetCharacter(characterUid);
        }

        /// <summary>
        /// CharacterId로 캐릭터 가져오기 (레거시 호환)
        /// </summary>
        public CharacterData GetCharacterByCharacterId(uint characterId)
        {
            return GetCharacter(characterId);
        }

        /// <summary>
        /// CharacterId로 캐릭터 보유 여부 확인 (레거시 호환)
        /// </summary>
        public bool HasCharacterByCharacterId(uint characterId)
        {
            return HasCharacter(characterId);
        }

        #endregion
    }
}
