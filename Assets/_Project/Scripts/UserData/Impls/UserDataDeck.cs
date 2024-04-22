using System.Linq;
using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Common;
using CookApps.gRPC.Universal;
using Google.Protobuf.Collections;

namespace CookApps.SampleTeamBattle
{
    public partial class UserDataManager
    {
        public static UserDataDeck UserDeck => Get<UserDataDeck>(DataCategory.UserDeck);
    }

    public class UserDataDeck : IUserData
    {
        DataCategory IUserData.DataCategory => DataCategory.UserDeck;
        int IUserData.Priority => 0;

        private UserDeck userDeckData;

        void IUserData.SetDataFromServer(string data)
        {
            userDeckData = MessageUtility.FromBase64String<UserDeck>(data);
        }

        public UserDeck GetUserTeam()
        {
            return userDeckData;
        }

        public void AddCharacterInTeam(int characterId)
        {
            int count = userDeckData.LineCharacters.Sum(x => x.CharacterIds.Count);
            if (count >= SpecOptionCache.DeckMaxSize)
            {
                return;
            }

            SpecCharacter specCharacter = SpecDataManager.Instance.SpecCharacter.Get(characterId);
            if (userDeckData.LineCharacters[specCharacter.GetLineIndex()].CharacterIds.Count >= SpecOptionCache.DeckLineMaxSize)
            {
                return;
            }

            userDeckData.LineCharacters[specCharacter.GetLineIndex()].CharacterIds.Add(characterId);
        }

        public void RemoveCharacterInTeam(int characterId)
        {
            int lineIndex = SpecDataManager.Instance.SpecCharacter.Get(characterId).GetLineIndex();
            userDeckData.LineCharacters[lineIndex].CharacterIds.Remove(characterId);
        }

        public bool IsDeployed(int characterId)
        {
            foreach (UserDeckLine userDeckLine in userDeckData.LineCharacters)
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
            return userDeckData.LineCharacters[0].CharacterIds;
        }

        public RepeatedField<int> GetMid()
        {
            return userDeckData.LineCharacters[1].CharacterIds;
        }

        public RepeatedField<int> GetBack()
        {
            return userDeckData.LineCharacters[2].CharacterIds;
        }

        public void Save()
        {
            CommonGrpcManager.Instance.SetUserDataAsync(DataCategory.UserDeck.ToCategoryString(), userDeckData);
        }
    }
}
