using System.Linq;
using Cookapps.Autobattleproject.V1;
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
            if (data == null)
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

        [ClearFunc]
        private void Clear_Deck()
        {
            userDeck = null;
        }

        public UserDeck GetUserTeam()
        {
            return userDeck;
        }

        public void AddCharacterInTeam(int characterId)
        {
            int count = userDeck.LineCharacters.Sum(x => x.CharacterIds.Count);
            if (count >= SpecOptionCache.DeckMaxSize)
            {
                return;
            }

            SpecCharacter specCharacter = SpecDataManager.Instance.SpecCharacter.Get(characterId);
            if (userDeck.LineCharacters[specCharacter.GetLineIndex()].CharacterIds.Count >= SpecOptionCache.DeckLineMaxSize)
            {
                return;
            }

            userDeck.LineCharacters[specCharacter.GetLineIndex()].CharacterIds.Add(characterId);
        }

        public void RemoveCharacterInTeam(int characterId)
        {
            int lineIndex = SpecDataManager.Instance.SpecCharacter.Get(characterId).GetLineIndex();
            userDeck.LineCharacters[lineIndex].CharacterIds.Remove(characterId);
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
