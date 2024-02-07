using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;

namespace CookApps.SampleTeamBattle
{
    public class UserBasicData : IUserData
    {
        DataCategory IUserData.DataCategory => DataCategory.UserData;
        int IUserData.Priority => 0;

        private UserData userBasicData;

        void IUserData.SetDataFromServer(string data)
        {
            userBasicData = MessageUtility.FromBase64String<UserData>(data);
        }
    }
}
