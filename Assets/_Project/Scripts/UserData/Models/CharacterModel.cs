using System;
using System.Collections.Generic;
using R3;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 캐릭터 데이터 모델
    /// 서버의 CharacterInfo 프로토콜을 래핑
    /// </summary>
    public class CharacterModel
    {
        // 프로토콜 데이터 (서버에서 받은 원본)
        private readonly Dictionary<string, CharacterData> _characters = new (64);

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<CharacterData> OnCharacterAdded = new();
        public readonly Subject<CharacterData> OnCharacterUpdated = new();
        public readonly Subject<string> OnCharacterRemoved = new();

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _characters.Clear();
            _characterIdToInstanceId.Clear();
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 캐릭터 가져오기
        /// </summary>
        public CharacterData GetCharacter(string instanceId)
        {
            return _characters.TryGetValue(instanceId, out var character) ? character : null;
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
        public bool HasCharacter(string instanceId)
        {
            return _characters.ContainsKey(instanceId);
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
                if (!string.IsNullOrEmpty(character.InstanceId))
                {
                    _characters[character.InstanceId] = character;
                }
            }

            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 단일 캐릭터 업데이트 (서버 응답용)
        /// </summary>
        internal void UpdateCharacter(CharacterData character)
        {
            if (character == null || string.IsNullOrEmpty(character.InstanceId))
            {
                Debug.LogError("[CharacterModel] Invalid character data");
                return;
            }

            bool isNew = !_characters.ContainsKey(character.InstanceId);
            _characters[character.InstanceId] = character;

            if (isNew)
                OnCharacterAdded.OnNext(character);
            else
                OnCharacterUpdated.OnNext(character);
        }

        /// <summary>
        /// 캐릭터 제거 (서버 응답용)
        /// </summary>
        internal void RemoveCharacter(string instanceId)
        {
            if (_characters.Remove(instanceId))
            {
                OnCharacterRemoved.OnNext(instanceId);
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

        #region CharacterId 기반 조회 (마이그레이션용)

        // CharacterId -> InstanceId 매핑 캐시
        private readonly Dictionary<uint, string> _characterIdToInstanceId = new(64);

        /// <summary>
        /// CharacterId로 캐릭터 가져오기
        /// </summary>
        public CharacterData GetCharacterByCharacterId(uint characterId)
        {
            // 캐시에서 먼저 찾기
            if (_characterIdToInstanceId.TryGetValue(characterId, out var instanceId))
            {
                if (_characters.TryGetValue(instanceId, out var cached))
                {
                    return cached;
                }
                // 캐시가 유효하지 않으면 제거
                _characterIdToInstanceId.Remove(characterId);
            }

            // 전체 순회
            foreach (var character in _characters.Values)
            {
                if (character.CharacterId == characterId)
                {
                    _characterIdToInstanceId[characterId] = character.InstanceId;
                    return character;
                }
            }

            return null;
        }

        /// <summary>
        /// CharacterId로 캐릭터 보유 여부 확인
        /// </summary>
        public bool HasCharacterByCharacterId(uint characterId)
        {
            return GetCharacterByCharacterId(characterId) != null;
        }

        /// <summary>
        /// 모든 보유 캐릭터 ID 목록 가져오기
        /// </summary>
        public void GetOwnedCharacterIds(List<uint> output)
        {
            if (output == null) return;

            output.Clear();
            output.Capacity = Math.Max(output.Capacity, _characters.Count);

            foreach (var character in _characters.Values)
            {
                output.Add(character.CharacterId);
            }
        }

        /// <summary>
        /// 캐시 초기화 (내부용)
        /// </summary>
        internal void ClearCache()
        {
            _characterIdToInstanceId.Clear();
        }

        #endregion
    }
}
