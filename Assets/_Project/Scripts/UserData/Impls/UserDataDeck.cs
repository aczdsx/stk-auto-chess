using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;
using Google.Protobuf.Collections;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserDeck userDeck;

        [Initialize(DataCategory.UserDeck)]
        private void Initialize_Deck(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userDeck = new UserDeck
                {
                    LineCharacters =
                    {
                        new UserDeckLine(),
                        new UserDeckLine(),
                        new UserDeckLine(),
                    },
                };
                return;
            }

            userDeck = MessageUtility.FromBase64String<UserDeck>(data);
        }

        [Clear]
        private void Clear_Deck()
        {
            userDeck = null;
        }

        public UserDeck GetUserTeam()
        {
            return userDeck;
        }

        public bool IsDeployed(int characterId)
        {
            foreach (UserDeckLine userDeckLine in userDeck.LineCharacters)
            {
                if (userDeckLine.CharacterIds.Contains(characterId))
                {
                    return true;
                }
            }

            return false;
        }

        public RepeatedField<int> GetFront()
        {
            return userDeck.LineCharacters[0].CharacterIds;
        }

        public RepeatedField<int> GetMid()
        {
            return userDeck.LineCharacters[1].CharacterIds;
        }

        public RepeatedField<int> GetBack()
        {
            return userDeck.LineCharacters[2].CharacterIds;
        }

        public void SaveUserDeck()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserDeck.ToCategoryString(), userDeck);
        }
    }
}
