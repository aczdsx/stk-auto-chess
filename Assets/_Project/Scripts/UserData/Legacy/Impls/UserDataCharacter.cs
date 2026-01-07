using System;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using Google.Protobuf.Collections;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserCharacterGroup userCharacterGroup;
        public static event Action<UserCharacter> OnUserCharacterChanged;

        public MapField<int, UserCharacter> UserCharacterDic => userCharacterGroup.UserCharacters;

        [Initialize(DataCategory.UserCharacterGroup)]
        private void Initialize_CharacterGroup(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userCharacterGroup = new UserCharacterGroup();

                // 전체 캐릭터 리스트 생성
                var allCharacterList = SpecDataManager.Instance.GetCharacterListByCharacterType(CharacterType.CHARACTER);
                foreach (var character in allCharacterList)
                    userCharacterGroup.UserCharacters.Add(character.character_id, new UserCharacter
                    {
                        CharacterId = character.character_id,
                        Level = 0, // 0: 미획득, 1 이상: 획득
                        Exp = 0,
                        StarLevel = character.init_star,
                        CharacterPiece = 0,
                        TranscendenceLevel = 0
                    });

                return;
            }

            userCharacterGroup = MessageUtility.FromBase64String<UserCharacterGroup>(data);
        }

        [Clear]
        private void Clear_CharacterGroup()
        {
            userCharacterGroup = null;
        }

        public int[] GetAllCharacterIds()
        {
            return userCharacterGroup.UserCharacters.Keys.ToArray();
        }

        // 현재 보유중인 모든 캐릭터의 전투력을 반환
        public int GetAllCharacterBattlePower()
        {
            double battlePower = 0;

            foreach (var deckCharacter in GetAllUserCharacterList())
            {
                var userCharacterData = GetUserCharacter(deckCharacter.CharacterId);
                if (userCharacterData != null)
                {
                    var characterStat = new CharacterStatData(userCharacterData.CharacterId, userCharacterData.Level,
                        GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());

                    battlePower += characterStat.GetAttrValueCP();
                }
            }

            return (int)battlePower;
        }

        // 해당 전투 덱의 전투력을 계산 (일반)
        public int GetDeckBattlePower(List<UserCharacterBattleDeck> targetDeckList)
        {
            double battlePower = 0;

            foreach (var deckCharacter in targetDeckList)
            {
                var userCharacterData = GetUserCharacter(deckCharacter.CharacterId);
                if (userCharacterData != null)
                {
                    var characterStat = new CharacterStatData(userCharacterData.CharacterId, userCharacterData.Level,
                        GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());

                    battlePower += characterStat.GetAttrValueCP();
                }
            }

            return (int)battlePower;
        }

        public int GetCharacterMaxLevel(int characterID)
        {
            if (UserCharacterDic.ContainsKey(characterID))
            {
                var specCharacterData = SpecDataManager.Instance.GetCharacterData(characterID);
                if (specCharacterData != null)
                {
                    var transcendenceLevel = UserCharacterDic[characterID].TranscendenceLevel;
                    var specTranscendenceData =
                        SpecDataManager.Instance.GetCharacterTranscendenceData(specCharacterData.grade_type, transcendenceLevel);

                    return specTranscendenceData.max_lv;
                }
            }

            return 0;
        }

        public void SetUserCharaceterBattleDeckList(InGameType targetType, List<CookApps.BattleSystem.CharacterController> characterList)
        {
            if (characterList == null || characterList.Count <= 0) return;

            if (userCharacterGroup.UserCharacterBattleDeckDic.ContainsKey((int)targetType) == false)
                userCharacterGroup.UserCharacterBattleDeckDic.Add((int)targetType, new UserCharacterBattleDeckList());

            userCharacterGroup.UserCharacterBattleDeckDic[(int)targetType].UserCharacterBattleDecks.Clear();

            foreach (var character in characterList)
            {
                var newUserBattleDeck = new UserCharacterBattleDeck();

                newUserBattleDeck.CharacterId = character.CharacterId;
                newUserBattleDeck.PositionTileX = character.CurrentTile.X;
                newUserBattleDeck.PositionTileY = character.CurrentTile.Y;

                userCharacterGroup.UserCharacterBattleDeckDic[(int)targetType].UserCharacterBattleDecks.Add(newUserBattleDeck);
            }

            SaveCharacterGroup();
        }

        public List<UserCharacterBattleDeck> GetUserCharacterBattleDeckList(InGameType targetType)
        {
            if (userCharacterGroup.UserCharacterBattleDeckDic.ContainsKey((int)targetType) == false)
                userCharacterGroup.UserCharacterBattleDeckDic.Add((int)targetType, new UserCharacterBattleDeckList());

            return userCharacterGroup.UserCharacterBattleDeckDic[(int)targetType].UserCharacterBattleDecks.ToList();
        }

        // 해당 타입의 배틀 덱이 있는지 확인
        public bool CheckUserCharacterBattleDeckList(InGameType targetType)
        {
            if (userCharacterGroup.UserCharacterBattleDeckDic.ContainsKey((int)targetType) == false)
                userCharacterGroup.UserCharacterBattleDeckDic.Add((int)targetType, new UserCharacterBattleDeckList());

            return userCharacterGroup.UserCharacterBattleDeckDic[(int)targetType].UserCharacterBattleDecks.Count > 0;
        }

        public void ChangeNickname(string nickname)
        {
            UserBasicData.Nickname = nickname;

            SaveUserBasic();
        }

        public void SetCharacterLevel(int characterID, int level)
        {
            if (UserCharacterDic.ContainsKey(characterID))
            {
                UserCharacterDic[characterID].Level = level;

                OnUserCharacterChanged?.Invoke(UserCharacterDic[characterID]);

                SaveCharacterGroup();
            }
        }

        public void IncreaseCharacterLevel(int characterID, int level)
        {
            if (UserCharacterDic.ContainsKey(characterID))
            {
                UserCharacterDic[characterID].Level += level;

                OnUserCharacterChanged?.Invoke(UserCharacterDic[characterID]);

                SaveCharacterGroup();
            }
        }

        public void SetTranscendenceLevel(int characterID, int transcendenceLevel)
        {
            if (UserCharacterDic.ContainsKey(characterID))
            {
                UserCharacterDic[characterID].TranscendenceLevel = transcendenceLevel;

                OnUserCharacterChanged?.Invoke(UserCharacterDic[characterID]);

                SaveCharacterGroup();
            }
        }

        public void IncreaseTranscendenceLevel(int characterID, int transcendenceLevel)
        {
            if (UserCharacterDic.ContainsKey(characterID))
            {
                UserCharacterDic[characterID].TranscendenceLevel += transcendenceLevel;

                OnUserCharacterChanged?.Invoke(UserCharacterDic[characterID]);

                SaveCharacterGroup();
            }
        }

        public void IncreaseKnightPieceCount(int characterID, int pieceCount)
        {
            if (UserCharacterDic.ContainsKey(characterID))
            {
                UserCharacterDic[characterID].CharacterPiece += pieceCount;

                SaveCharacterGroup();
            }
        }

        public void DecreaseKnightPieceCount(int characterID, int pieceCount)
        {
            if (UserCharacterDic.ContainsKey(characterID))
            {
                if (UserCharacterDic[characterID].CharacterPiece < pieceCount) return;

                UserCharacterDic[characterID].CharacterPiece -= pieceCount;
                UserCharacterDic[characterID].CharacterPiece = Mathf.Max(0, UserCharacterDic[characterID].CharacterPiece);

                SaveCharacterGroup();
            }
        }

        // 캐릭터 획득 (조각으로 인한 획득 처리x)
        public void AddNewCharacter(int characterID)
        {
            if (UserCharacterDic.ContainsKey(characterID))
            {
                UserCharacterDic[characterID].Level = 1; // 0: 미획득, 1 이상: 획득

                OnUserCharacterChanged?.Invoke(UserCharacterDic[characterID]);

                SaveCharacterGroup();
            }
        }

        public void ForceAddNewCharacter(int characterID)
        {
            if(SpecDataManager.Instance.GetCharacterData(characterID) == null) return;
            if (UserCharacterDic.ContainsKey(characterID))
            {
                AddNewCharacter(characterID);
                return;
            }
            userCharacterGroup.UserCharacters.Add(characterID, new UserCharacter
            {
                CharacterId = characterID,
                Level = 1,
                Exp = 0,
                StarLevel = SpecDataManager.Instance.GetCharacterData(characterID).init_star,
                CharacterPiece = 0,
                TranscendenceLevel = 0,
            });
        }


        // 보유한 캐릭터 인지 확인용
        public bool IsHaveCharacter(int characterID)
        {
            return UserCharacterDic.ContainsKey(characterID) && UserCharacterDic[characterID].Level > 0;
        }

        public List<UserCharacter> GetAllUserCharacterList()
        {
            // SpecDataManager에 없는 캐릭터 제거
            // RemoveInvalidCharacters();

            var result = UserCharacterDic.Values.ToList().FindAll(data => data.Level > 0);

            foreach (var data in result)
            {
                if (SpecDataManager.Instance.GetCharacterData(data.CharacterId) == null)
                {
                    result.Remove(data);
                }
            }

            return result;
        }

        /// <summary>
        /// SpecDataManager에 존재하지 않는 캐릭터를 UserCharacterDic에서 제거합니다.
        /// </summary>
        public void RemoveInvalidCharacters()
        {
            if (userCharacterGroup == null || userCharacterGroup.UserCharacters == null)
                return;

            var characterIdsToRemove = new List<int>();

            foreach (var kvp in userCharacterGroup.UserCharacters)
            {
                if (SpecDataManager.Instance.GetCharacterData(kvp.Key) == null)
                {
                    characterIdsToRemove.Add(kvp.Key);
                }
            }

            foreach (var characterId in characterIdsToRemove)
            {
                userCharacterGroup.UserCharacters.Remove(characterId);
            }

            if (characterIdsToRemove.Count > 0)
            {
                SaveCharacterGroup();
            }
        }

        public List<UserCharacter> GetAllNotHaveUserCharacterList()
        {
            return UserCharacterDic.Values.ToList().FindAll(data => data.Level == 0);
        }

        public UserCharacter GetUserCharacter(int characterID)
        {
            UserCharacter resultData = null;
            if (userCharacterGroup.UserCharacters.TryGetValue(characterID, out resultData)) return resultData;

            return null;
        }

        public UserCharacter GetUserCharacterByIndex(int index)
        {
            return userCharacterGroup.UserCharacters[index];
        }

        public int GetCharacterCount()
        {
            return userCharacterGroup.UserCharacters.Count;
        }

        public void SaveCharacterGroup()
        {
            QueueSave(DataCategory.UserCharacterGroup.ToCategoryString(), userCharacterGroup);
        }
    }
}