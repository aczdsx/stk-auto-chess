using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;

namespace CookApps.SampleTeamBattle
{
    // userStage에 쉽게 접근하기위해
    public partial class UserDataManager
    {
        public static UserDataStage UserStage => Get<UserDataStage>(DataCategory.UserStageGroup);
    }

    public class UserDataStage : IUserData
    {
        DataCategory IUserData.DataCategory => DataCategory.UserStageGroup;
        int IUserData.Priority => 1;

        private UserStageGroup userStageGroupData;

        void IUserData.Initialize(string data)
        {
            userStageGroupData = MessageUtility.FromBase64String<UserStageGroup>(data);
        }

        public UserStage GetUserStage(int stageId)
        {
            return userStageGroupData.UserStages[stageId];
        }

        public int GetCurrentStageId()
        {
            return userStageGroupData.CurrentStageId;
        }

        public int GetLastStageId()
        {
            return userStageGroupData.LastStageId;
        }
    }
}
