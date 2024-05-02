using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;

namespace CookApps.SampleTeamBattle
{
    public partial class UserDataManager
    {
        private UserData userBasicData;

        [Initialize(DataCategory.UserData)]
        private void Initialize_BasicData(string data)
        {
            userBasicData = MessageUtility.FromBase64String<UserData>(data);
        }
    }
}
