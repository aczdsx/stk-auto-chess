using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;

namespace CookApps.SampleTeamBattle
{
    public partial class UserDataManager
    {
        private UserStageGroup userStageGroupData;

        [Initialize(DataCategory.UserStageGroup, 1)]
        private void Initialize_StageGroup(string data)
        {
            userStageGroupData = MessageUtility.FromBase64String<UserStageGroup>(data);
        }

        public UserStage GetUserStage(int stageId)
        {
            if (userStageGroupData.UserStages.TryGetValue(stageId, out UserStage userStage))
            {
                return userStage;
            }

            return new UserStage {StageId = stageId, StarCount = 0};
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
