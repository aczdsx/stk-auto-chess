using System.Linq;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserQuest userQuest;

        public UserQuest UserQuest => userQuest;

        [Initialize(DataCategory.UserQuest, 9)]
        private void Initialize_QuestData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userQuest = new UserQuest
                {

                };

                return;
            }

            userQuest = MessageUtility.FromBase64String<UserQuest>(data);
        }

        [Clear]
        private void Clear_QuestData()
        {
            userQuest = null;
        }

        public void SaveUserQuestData()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserQuest.ToCategoryString(), userQuest);
        }
    }
}
