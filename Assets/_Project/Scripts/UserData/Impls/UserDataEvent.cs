using System.Linq;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserEvent userEvent;

        public UserEvent UserEvent => userEvent;

        [Initialize(DataCategory.UserEvent, 10)]
        private void Initialize_EventData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userEvent = new UserEvent();

                return;
            }

            userEvent = MessageUtility.FromBase64String<UserEvent>(data);
        }

        [Clear]
        private void Clear_EventData()
        {
            userEvent = null;
        }

        public void SaveUserEventData()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserEvent.ToCategoryString(), userEvent);
        }
    }
}
