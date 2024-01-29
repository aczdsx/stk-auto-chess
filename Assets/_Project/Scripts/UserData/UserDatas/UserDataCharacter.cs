using System;
using System.Collections.Generic;
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

        void IUserData.Initialize(string data)
        {
            userCharacterGroup = MessageUtility.FromBase64String<UserCharacterGroup>(data);
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
