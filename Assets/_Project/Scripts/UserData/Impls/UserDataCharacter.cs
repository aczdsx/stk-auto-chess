using System;
using System.Collections.Generic;
using System.Linq;
using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Common;
using CookApps.gRPC.Universal;
using Google.Protobuf.Collections;

namespace CookApps.SampleTeamBattle
{
    public partial class UserDataManager
    {
        public static UserDataCharacter UserCharacter => Get<UserDataCharacter>(DataCategory.UserCharacterGroup);
    }

    public class UserDataCharacter : IUserData
    {
        DataCategory IUserData.DataCategory => DataCategory.UserCharacterGroup;
        int IUserData.Priority => 0;

        private UserCharacterGroup userCharacterGroup;

        public static event Action<UserCharacter> OnUserCharacterChanged;

        void IUserData.SetDataFromServer(string data)
        {
            userCharacterGroup = MessageUtility.FromBase64String<UserCharacterGroup>(data);
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

        public void Save()
        {
            CommonGrpcManager.Instance.SetUserDataAsync(DataCategory.UserCharacterGroup.ToCategoryString(), userCharacterGroup);
        }
    }
}
