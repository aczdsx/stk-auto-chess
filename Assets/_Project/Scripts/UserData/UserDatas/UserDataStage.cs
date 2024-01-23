using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;

namespace CookApps.SampleTeamBattle
{
    public class UserDataStage : IUserData
    {
        DataCategory IUserData.DataCategory => DataCategory.UserStage;
        int IUserData.Priority => 1;

        private UserStage userStageData;

        void IUserData.Initialize(string data)
        {
            userStageData = MessageUtility.FromBase64String<UserStage>(data);
        }
    }
}
