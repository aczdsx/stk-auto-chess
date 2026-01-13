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
            return _characters.GetValueOrDefault(characterId);
        }

        /// <summary>
        /// 캐릭터 가져오기 (CharacterId로 조회)
        /// </summary>
        public CharacterData GetCharacter(int characterId)
        {
            return GetCharacter((uint)characterId);
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
        /// 캐릭터 존재 여부
        /// </summary>
        public bool HasCharacter(int characterId)
        {
            return HasCharacter((uint)characterId);
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
                var spec = SpecDataManager.Instance.GetSpecCharacter((int)character.CharacterId);
                if (spec != null)
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
            var spec = SpecDataManager.Instance.GetSpecCharacter((int)character.CharacterId);
            if (spec == null)
            {
                Debug.LogError($"[CharacterModel] Spec data not found forCharacterId: {character.CharacterId}");
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
        /// 캐릭터 ID로 새 캐릭터 추가 (보상 획득 시)
        /// </summary>
        internal void AddCharacterById(uint characterId)
        {
            if (characterId == 0) return;

            // 이미 존재하는 캐릭터면 무시
            if (_characters.ContainsKey(characterId))
            {
                return;
            }

            var spec = SpecDataManager.Instance.GetSpecCharacter((int)characterId);
            if (spec == null)
            {
                Debug.LogError($"[CharacterModel] Spec data not found forCharacterId: {characterId}");
                return; 
            }

            // 새 캐릭터 데이터 생성 (기본값)
            var newCharacter = new CharacterData
            {
                CharacterId = characterId,
                Level = 1,
                TranscendLevel = 0,
            };

            _characters[characterId] = newCharacter;
            OnCharacterAdded.OnNext(newCharacter);
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

        /// <summary>
        /// 캐릭터 최대 레벨 계산
        /// </summary>
        public int GetCharacterMaxLevel(int characterId)
        {
            var character = GetCharacter(characterId);
            if (character == null) return 0;

            var specCharacterData = SpecDataManager.Instance.GetCharacterData(characterId);
            if (specCharacterData == null) return 0;

            var specTranscendenceData = SpecDataManager.Instance.GetCharacterTranscendenceData(
                specCharacterData.grade_type,
                (int)character.TranscendLevel
            );

            return specTranscendenceData?.max_level ?? 0;
        }
    }
}
