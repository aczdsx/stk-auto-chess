using System;
using System.Collections.Generic;
using R3;
using Tech.Hive.V1;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 캐릭터 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// UI가 직접 데이터 모델을 접근하지 않고 브릿지를 통해 접근
    /// </summary>
    public class CharacterDataBridge
    {
        private CharacterModel Model;
        // Public Observable 노출
        public Observable<CharacterData> OnCharacterAdded;
        public Observable<CharacterData> OnCharacterUpdated;
        public Observable<uint> OnCharacterRemoved;

        public CharacterDataBridge()
        {
            Model = ServerDataManager.Instance.Character;
            OnCharacterAdded = Model.OnCharacterAdded;
            OnCharacterUpdated = Model.OnCharacterUpdated;
            OnCharacterRemoved = Model.OnCharacterRemoved;
        }

        /// <summary>
        /// 모든 캐릭터 가져오기
        /// </summary>
        public void GetAllCharacters(List<CharacterData> output)
        {
            Model?.GetAllCharacters(output);
        }

        /// <summary>
        /// 특정 캐릭터 가져오기 (CharacterId로 조회)
        /// </summary>
        public CharacterData GetCharacter(uint characterId)
        {
            return Model?.GetCharacter(characterId);
        }

        /// <summary>
        /// 캐릭터 개수
        /// </summary>
        public int CharacterCount => Model?.CharacterCount ?? 0;

        /// <summary>
        /// 캐릭터 존재 여부
        /// </summary>
        public bool HasCharacter(uint characterId)
        {
            return Model?.HasCharacter(characterId) ?? false;
        }

        /// <summary>
        /// 조건에 맞는 캐릭터 필터링
        /// </summary>
        public void GetFilteredCharacters(List<CharacterData> output, Func<CharacterData, bool> filter)
        {
            Model?.GetCharactersByCondition(output, filter);
        }

        /// <summary>
        /// 레벨 범위로 필터링
        /// </summary>
        public void GetCharactersByLevelRange(List<CharacterData> output, uint minLevel, uint maxLevel)
        {
            if (Model == null || output == null) return;

            output.Clear();
            using var _ = ListPool<CharacterData>.Get(out var allCharacters);
            Model.GetAllCharacters(allCharacters);

            // for문 사용 (Linq 지양)
            for (int i = 0; i < allCharacters.Count; i++)
            {
                var character = allCharacters[i];
                if (character.Level >= minLevel && character.Level <= maxLevel)
                {
                    output.Add(character);
                }
            }
        }

        /// <summary>
        /// 레어리티로 필터링
        /// </summary>
        public void GetCharactersByRarity(List<CharacterData> output, GradeType gradeType)
        {
            if (Model == null || output == null) return;

            output.Clear();
            using var _ = ListPool<CharacterData>.Get(out var allCharacters);
            Model.GetAllCharacters(allCharacters);

            for (int i = 0; i < allCharacters.Count; i++)
            {
                var character = allCharacters[i];
                var specData = SpecDataManager.Instance.GetCharacterData((int)character.CharacterId);
                if (specData != null && specData.grade_type == gradeType)
                {
                    output.Add(character);
                }
            }
        }

        #region UserDataManager 호환 메서드 (마이그레이션용)

        /// <summary>
        /// 캐릭터 보유 여부 (int 오버로드)
        /// </summary>
        public bool IsHaveCharacter(int characterId)
        {
            return Model?.HasCharacter((uint)characterId) ?? false;
        }

        /// <summary>
        /// CharacterId로 캐릭터 가져오기 (UserDataManager 호환)
        /// </summary>
        public CharacterData GetUserCharacter(int characterId)
        {
            return Model?.GetCharacter(characterId);
        }

        /// <summary>
        /// 모든 보유 캐릭터 목록 (UserDataManager 호환)
        /// </summary>
        public void GetAllUserCharacterList(List<CharacterData> output)
        {
            Model?.GetAllCharacters(output);
        }

        /// <summary>
        /// 캐릭터 최대 레벨 계산
        /// </summary>
        public int GetCharacterMaxLevel(int characterId)
        {
            var character = Model?.GetCharacter(characterId);
            if (character == null) return 0;

            var specCharacterData = SpecDataManager.Instance.GetCharacterData(characterId);
            if (specCharacterData == null) return 0;

            var specTranscendenceData = SpecDataManager.Instance.GetCharacterTranscendenceData(
                specCharacterData.grade_type,
                (int)character.TranscendLevel
            );

            return specTranscendenceData?.max_level ?? 0;
        }

        /// <summary>
        /// 전체 보유 캐릭터 전투력 계산
        /// </summary>
        public int GetAllCharacterBattlePower()
        {
            if (Model == null) return 0;

            double battlePower = 0;

            using var _ = ListPool<CharacterData>.Get(out var allCharacters);
            Model.GetAllCharacters(allCharacters);

            for (int i = 0; i < allCharacters.Count; i++)
            {
                var character = allCharacters[i];
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
        /// 모든 보유 캐릭터 ID 목록
        /// </summary>
        public void GetAllCharacterIds(List<int> output)
        {
            if (Model == null || output == null) return;

            output.Clear();

            using var _ = ListPool<CharacterData>.Get(out var allCharacters);
            Model.GetAllCharacters(allCharacters);

            for (int i = 0; i < allCharacters.Count; i++)
            {
                output.Add((int)allCharacters[i].CharacterId);
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
                if (!IsHaveCharacter(specChar.character_id))
                {
                    output.Add(specChar.character_id);
                }
            }
        }

        /// <summary>
        /// 캐릭터 레벨 가져오기
        /// </summary>
        public int GetCharacterLevel(int characterId)
        {
            var character = Model?.GetCharacter(characterId);
            return (int)(character?.Level ?? 0);
        }

        /// <summary>
        /// 캐릭터 초월 레벨 가져오기
        /// </summary>
        public int GetTranscendenceLevel(int characterId)
        {
            var character = Model?.GetCharacter(characterId);
            return (int)(character?.TranscendLevel ?? 0);
        }

        /// <summary>
        /// 캐릭터 돌파 레벨 가져오기
        /// </summary>
        public int GetExceedLevel(int characterId)
        {
            var character = Model?.GetCharacter(characterId);
            return (int)(character?.ExceedLevel ?? 0);
        }

        /// <summary>
        /// 캐릭터 조각 개수 가져오기
        /// TODO: InventoryModel에서 가져오도록 수정 필요 (pieceItemId 계산 방식 확인 후)
        /// </summary>
        public int GetCharacterPiece(int characterId)
        {
            // TODO: InventoryModel.GetCurrency(pieceItemId) 사용
            // pieceItemId 계산 방식 확인 필요
            // var legacyData = UserDataManager.Instance.GetUserCharacter(characterId);
            // return legacyData?.CharacterPiece ?? 0;
            return 0;
        }

        /// <summary>
        /// 캐릭터 경험치 가져오기 (Legacy fallback)
        /// TODO: 서버 API 마이그레이션 후 수정 필요
        /// </summary>
        public int GetCharacterExp(int characterId)
        {
            return 0;
        }

        #endregion
    }
}
