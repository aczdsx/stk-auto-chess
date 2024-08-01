using System;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;
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
        void Initialize_CharacterGroup(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userCharacterGroup = new UserCharacterGroup();

                // 전체 캐릭터 리스트 생성
                var allCharacterList = SpecDataManager.Instance.GetCharacterListByCharacterType(CharacterType.CHARACTER);
                foreach (var character in allCharacterList)
                {
                    userCharacterGroup.UserCharacters.Add(character.character_id, new UserCharacter
                    {
                        CharacterId = character.character_id,
                        Level = 0, // 0: 미획득, 1 이상: 획득
                        Exp = 0,
                        StarLevel = character.init_star,
                        CharacterPiece = 0,
                        TranscendenceLevel = 0,
                    });
                }

                return;
            }

            userCharacterGroup = MessageUtility.FromBase64String<UserCharacterGroup>(data);
        }

        [Clear]
        void Clear_CharacterGroup()
        {
            userCharacterGroup = null;
        }

        public int[] GetAllCharacterIds()
        {
            return userCharacterGroup.UserCharacters.Keys.ToArray();
        }

        // 해당 전투 덱의 전투력을 계산 (일반)
        public int GetDeckBattlePower(UserCharacterBattleDeckList targetDeckList)
        {
            double battlePower = 0;

            foreach (var deckCharacter in targetDeckList.UserCharacterBattleDecks)
            {
                var userCharacterData = GetUserCharacter(deckCharacter.CharacterId);
                if (userCharacterData != null)
                {
                    var characterStat = new CharacterStatData(userCharacterData.CharacterId, userCharacterData.Level,
                        GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());
                    
                    battlePower += characterStat.GetAttrValue();
                }
            }

            return (int)battlePower;
        }
        
        // 해당 전투 덱의 전투력을 계산 (PVP)
        public int GetPVPDeckBattlePower(bool isDefenseDeck)
        {
            double battlePower = 0;

            UserPVPBattleDeckList targetDeckList = isDefenseDeck ? UserPVP.MyPvpDefenseDeckList : UserPVP.MyPvpAttackDeckList;
            
            if (targetDeckList == null || targetDeckList.PvpCharacterDecks == null) return 0;
            
            foreach (var deckCharacter in targetDeckList.PvpCharacterDecks)
            {
                var userCharacterData = GetUserCharacter(deckCharacter.Id);
                if (userCharacterData != null)
                {
                    var characterStat = new CharacterStatData(userCharacterData.CharacterId, userCharacterData.Level,
                        GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());
                    
                    battlePower += characterStat.GetAttrValue();
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
                    int transcendenceLevel = UserCharacterDic[characterID].TranscendenceLevel;
                    var specTranscendenceData = SpecDataManager.Instance.GetCharacterTranscendenceData(specCharacterData.element_type, specCharacterData.grade_type, transcendenceLevel);

                    return specTranscendenceData.max_lv;
                }
            }

            return 0;
        }

        public void SetUserCharaceterBattleDeckList(InGameType targetType, List<CookApps.BattleSystem.CharacterController> characterList)
        {
            if (characterList == null || characterList.Count <= 0) return;

            if (userCharacterGroup.UserCharacterBattleDeckDic.ContainsKey((int)targetType) == false)
            {
                userCharacterGroup.UserCharacterBattleDeckDic.Add((int)targetType, new UserCharacterBattleDeckList());
            }

            userCharacterGroup.UserCharacterBattleDeckDic[(int)targetType].UserCharacterBattleDecks.Clear();

            foreach (var character in characterList)
            {
                UserCharacterBattleDeck newUserBattleDeck = new UserCharacterBattleDeck();

                newUserBattleDeck.CharacterId = character.CharacterId;
                newUserBattleDeck.PositionTileX = character.CurrentTile.X;
                newUserBattleDeck.PositionTileY = character.CurrentTile.Y;

                userCharacterGroup.UserCharacterBattleDeckDic[(int) targetType].UserCharacterBattleDecks.Add(newUserBattleDeck);
            }

            SaveCharacterGroup();
        }

        public List<UserCharacterBattleDeck> GetUserCharacterBattleDeckList(InGameType targetType)
        {
            if (userCharacterGroup.UserCharacterBattleDeckDic.ContainsKey((int)targetType) == false)
            {
                userCharacterGroup.UserCharacterBattleDeckDic.Add((int)targetType, new UserCharacterBattleDeckList());
            }

            return userCharacterGroup.UserCharacterBattleDeckDic[(int)targetType].UserCharacterBattleDecks.ToList();
        }
        
        // 해당 타입의 배틀 덱이 있는지 확인
        public bool CheckUserCharacterBattleDeckList(InGameType targetType)
        {
            if (userCharacterGroup.UserCharacterBattleDeckDic.ContainsKey((int)targetType) == false)
            {
                userCharacterGroup.UserCharacterBattleDeckDic.Add((int)targetType, new UserCharacterBattleDeckList());
            }

            return userCharacterGroup.UserCharacterBattleDeckDic[(int)targetType].UserCharacterBattleDecks.Count > 0;
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

                SaveCharacterGroup();
            }
        }

        // 캐릭터 획득 (조각으로 인한 획득 처리x)
        public void AddNewCharacter(int characterID)
        {
            if (UserCharacterDic.ContainsKey(characterID))
            {
                UserCharacterDic[characterID].Level = 1;   // 0: 미획득, 1 이상: 획득

                OnUserCharacterChanged?.Invoke(UserCharacterDic[characterID]);

                SaveCharacterGroup();
            }
        }

        // 보유한 캐릭터 인지 확인용
        public bool IsHaveCharacter(int characterID)
        {
            return UserCharacterDic.ContainsKey(characterID) && UserCharacterDic[characterID].Level > 0;
        }

        public List<UserCharacter> GetAllUserCharacterList()
        {
            return UserCharacterDic.Values.ToList().FindAll(data => data.Level > 0);
        }

        public List<UserCharacter> GetAllNotHaveUserCharacterList()
        {
            return UserCharacterDic.Values.ToList().FindAll(data => data.Level == 0);
        }

        public UserCharacter GetUserCharacter(int characterID)
        {
            UserCharacter resultData = null;
            if (userCharacterGroup.UserCharacters.TryGetValue(characterID, out resultData))
            {
                return resultData;
            }

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
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserCharacterGroup.ToCategoryString(), userCharacterGroup);
        }
    }
}
