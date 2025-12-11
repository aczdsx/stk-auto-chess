using System;
using System.Collections.Generic;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 캐릭터 데이터 모델
    /// 서버의 CharacterInfo 프로토콜을 래핑
    /// 델타 업데이트 지원
    /// </summary>
    public class CharacterModel : IDataModel
    {
        public const string CATEGORY_KEY = "character";

        // 프로토콜 데이터 (서버에서 받은 원본)
        private readonly Dictionary<string, CharacterData> _characters;

        // 버전 정보
        private int _version;

        public string CategoryKey => CATEGORY_KEY;
        public int Version => _version;

        // 이벤트
        public event Action OnChanged;
        public event Action<CharacterData> OnCharacterAdded;
        public event Action<CharacterData> OnCharacterUpdated;
        public event Action<string> OnCharacterRemoved;

        public CharacterModel()
        {
            _characters = new Dictionary<string, CharacterData>(64);
            _version = 0;
        }

        /// <summary>
        /// 델타 업데이트 적용
        /// </summary>
        public void ApplyDelta(IDataModel delta)
        {
            if (delta is not CharacterModel characterDelta)
            {
                Debug.LogError("[CharacterModel] Invalid delta type");
                return;
            }

            // 변경된 캐릭터만 업데이트
            foreach (var kvp in characterDelta._characters)
            {
                var instanceId = kvp.Key;
                var newCharacter = kvp.Value;

                if (_characters.TryGetValue(instanceId, out var existing))
                {
                    // 기존 캐릭터 업데이트
                    _characters[instanceId] = newCharacter;
                    OnCharacterUpdated?.Invoke(newCharacter);
                }
                else
                {
                    // 새 캐릭터 추가
                    _characters[instanceId] = newCharacter;
                    OnCharacterAdded?.Invoke(newCharacter);
                }
            }

            _version = characterDelta._version;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _characters.Clear();
            _version = 0;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// 유효성 검증
        /// </summary>
        public bool Validate()
        {
            // 각 캐릭터 데이터 검증
            foreach (var character in _characters.Values)
            {
                if (string.IsNullOrEmpty(character.InstanceId))
                {
                    Debug.LogError("[CharacterModel] Invalid character: missing InstanceId");
                    return false;
                }
            }
            return true;
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
        internal void SetCharacters(IEnumerable<CharacterData> characters, int version)
        {
            _characters.Clear();

            foreach (var character in characters)
            {
                if (!string.IsNullOrEmpty(character.InstanceId))
                {
                    _characters[character.InstanceId] = character;
                }
            }

            _version = version;
            OnChanged?.Invoke();
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
                OnCharacterAdded?.Invoke(character);
            else
                OnCharacterUpdated?.Invoke(character);

            _version++;
        }

        /// <summary>
        /// 캐릭터 제거 (서버 응답용)
        /// </summary>
        internal void RemoveCharacter(string instanceId)
        {
            if (_characters.Remove(instanceId))
            {
                OnCharacterRemoved?.Invoke(instanceId);
                _version++;
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
    }
}
