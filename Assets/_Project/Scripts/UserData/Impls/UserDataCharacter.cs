using System;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;
using Google.Protobuf.Collections;

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
                    userCharacterGroup.UserCharacters.Add(character.prefab_id, new UserCharacter
                    {
                        CharacterId = character.prefab_id,
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

        public void IncreaseKnightPieceCount(int prefabID, int pieceCount)
        {
            if (UserCharacterDic.ContainsKey(prefabID))
            {
                UserCharacterDic[prefabID].CharacterPiece += pieceCount;

                SaveCharacterGroup();
            }
        }

        public void DecreaseKnightPieceCount(int prefabID, int pieceCount)
        {
            if (UserCharacterDic.ContainsKey(prefabID))
            {
                UserCharacterDic[prefabID].CharacterPiece -= pieceCount;

                SaveCharacterGroup();
            }
        }

        // 캐릭터 획득 (조각으로 인한 획득 처리x)
        public void AddNewCharacter(int prefabID)
        {
            if (UserCharacterDic.ContainsKey(prefabID))
            {
                UserCharacterDic[prefabID].Level = 1;   // 0: 미획득, 1 이상: 획득

                OnUserCharacterChanged?.Invoke(UserCharacterDic[prefabID]);

                SaveCharacterGroup();
            }
        }

        public List<UserCharacter> GetAllUserCharacters()
        {
            return userCharacterGroup.UserCharacters.Values.ToList();
        }

        public UserCharacter GetUserCharacter(int prefabID)
        {
            UserCharacter resultData = null;
            if (userCharacterGroup.UserCharacters.TryGetValue(prefabID, out resultData))
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
