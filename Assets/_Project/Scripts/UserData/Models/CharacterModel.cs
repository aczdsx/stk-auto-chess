using System;
using System.Collections.Generic;
using R3;
using Tech.Hive.V1;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 캐릭터 데이터 모델
    /// 서버의 CharacterData 프로토콜을 래핑
    /// Key: CharacterId (uint)
    ///
    /// [관리 데이터]
    /// - 캐릭터 정보: 보유 캐릭터 목록, 캐릭터 레벨/초월/돌파
    /// - 필터링: 레벨 범위, 레어리티, 미보유 캐릭터
    /// - 통계: 전체 전투력 계산
    ///
    /// [변경 이력]
    /// - CharacterDataBridge에서 마이그레이션된 메서드:
    ///   GetCharacterLevel, GetTranscendenceLevel, GetExceedLevel,
    ///   GetCharactersByLevelRange, GetAllCharacterIds, GetAllCharacterBattlePower,
    ///   GetCharactersByRarity, GetAllNotHaveCharacterList
    /// - CharacterDataBridge에서 삭제된 메서드 (1:1 래퍼):
    ///   GetAllCharacters, GetCharacter, CharacterCount, HasCharacter,
    ///   GetFilteredCharacters, IsHaveCharacter, GetUserCharacter, GetAllUserCharacterList
    /// - CharacterDataBridge에서 삭제된 메서드 (레거시 스텁):
    ///   GetCharacterPiece, GetCharacterExp
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

        #region CharacterDataBridge에서 마이그레이션

        /// <summary>
        /// 캐릭터 레벨 가져오기
        /// </summary>
        public int GetCharacterLevel(int characterId)
        {
            var character = GetCharacter(characterId);
            return (int)(character?.Level ?? 0);
        }

        /// <summary>
        /// 캐릭터 초월 레벨 가져오기
        /// </summary>
        public int GetTranscendenceLevel(int characterId)
        {
            var character = GetCharacter(characterId);
            return (int)(character?.TranscendLevel ?? 0);
        }

        /// <summary>
        /// 캐릭터 돌파 레벨 가져오기
        /// </summary>
        public int GetExceedLevel(int characterId)
        {
            var character = GetCharacter(characterId);
            return (int)(character?.ExceedLevel ?? 0);
        }

        /// <summary>
        /// 레벨 범위로 필터링
        /// </summary>
        public void GetCharactersByLevelRange(List<CharacterData> output, uint minLevel, uint maxLevel)
        {
            if (output == null) return;

            output.Clear();
            foreach (var character in _characters.Values)
            {
                if (character.Level >= minLevel && character.Level <= maxLevel)
                {
                    output.Add(character);
                }
            }
        }

        /// <summary>
        /// 모든 보유 캐릭터 ID 목록 (int)
        /// </summary>
        public void GetAllCharacterIds(List<int> output)
        {
            if (output == null) return;

            output.Clear();
            output.Capacity = Math.Max(output.Capacity, _characters.Count);

            foreach (var characterId in _characters.Keys)
            {
                output.Add((int)characterId);
            }
        }

        /// <summary>
        /// 전체 보유 캐릭터 전투력 계산
        /// </summary>
        public int GetAllCharacterBattlePower()
        {
            double battlePower = 0;

            foreach (var character in _characters.Values)
            {
                var characterStat = new CharacterStatData(
                    (int)character.CharacterId,
                    (int)character.Level,
                    GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()
                );
                battlePower += characterStat.GetAttrValueCP();
            }

            return (int)battlePower;
        }

        /// <summary>
        /// 레어리티로 필터링
        /// </summary>
        public void GetCharactersByRarity(List<CharacterData> output, GradeType gradeType)
        {
            if (output == null) return;

            output.Clear();
            foreach (var character in _characters.Values)
            {
                var specData = SpecDataManager.Instance.GetCharacterData((int)character.CharacterId);
                if (specData != null && specData.grade_type == gradeType)
                {
                    output.Add(character);
                }
            }
        }

        /// <summary>
        /// 미보유 캐릭터 목록 (SpecData 기반)
        /// </summary>
        public void GetAllNotHaveCharacterList(List<int> output)
        {
            if (output == null) return;

            output.Clear();

            var allCharacterList = SpecDataManager.Instance.GetCharacterListByCharacterType(CharacterType.CHARACTER);
            for (int i = 0; i < allCharacterList.Count; i++)
            {
                var specChar = allCharacterList[i];
                if (!HasCharacter(specChar.id))
                {
                    output.Add(specChar.id);
                }
            }
        }

        #endregion
    }
}
