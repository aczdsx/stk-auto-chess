using System;
using System.Linq;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserCharacterGroup userCharacterGroup;
        public static event Action<UserCharacter> OnUserCharacterChanged;

        [Initialize(DataCategory.UserCharacterGroup)]
        void Initialize_CharacterGroup(string data)
        {
            if (data == null)
            {
                userCharacterGroup = new UserCharacterGroup();
                return;
            }

            userCharacterGroup = MessageUtility.FromBase64String<UserCharacterGroup>(data);
        }

        [ClearFunc]
        void Clear_CharacterGroup()
        {
            userCharacterGroup = null;
        }

        public int[] GetAllCharacterIds()
        {
            return userCharacterGroup.UserCharacters.Select(x => x.CharacterId).ToArray();
        }

        public void AddCharacter(int characterId, int cardCount)
        {
            SpecCharacter specCharacter = SpecDataManager.Instance.SpecCharacter.Get(characterId);
            UserCharacter userCharacter = null;
            foreach (UserCharacter user in userCharacterGroup.UserCharacters)
            {
                if (user.CharacterId == characterId)
                {
                    userCharacter = user;
                    break;
                }
            }

            if (userCharacter == null)
            {
                userCharacter = new UserCharacter
                {
                    CharacterId = characterId,
                    StarLevel = specCharacter.init_star,
                    StarExp = cardCount,
                };
                userCharacterGroup.UserCharacters.Add(userCharacter);
            }
            else
            {
                userCharacter.StarExp += cardCount;
            }

            OnUserCharacterChanged?.Invoke(userCharacter);
        }

        public UserCharacter GetUserCharacter(int characterId)
        {
            foreach (UserCharacter user in userCharacterGroup.UserCharacters)
            {
                if (user.CharacterId == characterId)
                {
                    return user;
                }
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
