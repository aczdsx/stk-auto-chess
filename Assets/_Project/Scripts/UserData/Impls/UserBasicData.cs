using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Universal;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserBasicData userBasicData;

        public UserBasicData UserBasicData => userBasicData;

        [Initialize(DataCategory.UserData)]
        private void Initialize_BasicData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userBasicData = new UserBasicData
                {
                    Uid = 0,
                    Level = 1,
                    Exp = 0,
                    Nickname = "New User",
                    UserIconId = 40101,

                    TotalGachaCount = 0,
                };
                return;
            }

            userBasicData = MessageUtility.FromBase64String<UserBasicData>(data);
        }

        [Clear]
        private void Clear_BasicData()
        {
            userBasicData = null;
        }
    }
}
