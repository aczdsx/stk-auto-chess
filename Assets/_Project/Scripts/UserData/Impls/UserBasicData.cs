using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;

namespace CookApps.SampleTeamBattle
{
    public partial class UserDataManager
    {
        private UserBasicData userBasicData;

        [Initialize(DataCategory.UserData)]
        private void Initialize_BasicData(string data)
        {
            if (data == null)
            {
                userBasicData = new UserBasicData
                {
                    Uid = 0,
                    Level = 1,
                    Exp = 0,
                };
                return;
            }

            userBasicData = MessageUtility.FromBase64String<UserBasicData>(data);
        }

        [ClearFunc]
        private void Clear_BasicData()
        {
            userBasicData = null;
        }
    }
}
