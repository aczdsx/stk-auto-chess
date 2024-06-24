using System;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Autobattleproject.V1;
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

        public void SetUserCharaceterBattleDeckList(List<CookApps.BattleSystem.CharacterController> characterList)
        {
            if (characterList == null || characterList.Count <= 0) return;

            userCharacterGroup.UserCharacterBattleDecks.Clear();

            foreach (var character in characterList)
            {
                userCharacterGroup.UserCharacterBattleDecks.Add(new UserCharacterBattleDeck
                {
                    CharacterId = character.CharacterId,
                    PositionTileX = character.CurrentTile.X,
                    PositionTileY = character.CurrentTile.Y,
                });
            }

            SaveCharacterGroup();
        }

        public List<UserCharacterBattleDeck> GetUserCharacterBattleDeckList()
        {
            return userCharacterGroup.UserCharacterBattleDecks.ToList();
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
