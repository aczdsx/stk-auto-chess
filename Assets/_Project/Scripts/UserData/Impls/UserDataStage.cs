using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;

namespace CookApps.SampleTeamBattle
{
    public partial class UserDataManager
    {
        private UserStageGroup userStageGroup;

        [Initialize(DataCategory.UserStageGroup, 1)]
        private void Initialize_StageGroup(string data)
        {
            if (data == null)
            {
                userStageGroup = new UserStageGroup
                {
                    CurrentStageId = 1,
                    LastStageId = 1,
                };
                return;
            }
            userStageGroup = MessageUtility.FromBase64String<UserStageGroup>(data);
        }

        [ClearFunc]
        private void Clear_StageGroup()
        {
            userStageGroup = null;
        }

        public UserStage GetUserStage(int stageId)
        {
            if (userStageGroup.UserStages.TryGetValue(stageId, out UserStage userStage))
            {
                return userStage;
            }

            return new UserStage {StageId = stageId, StarCount = 0};
        }

        public int GetCurrentStageId()
        {
            return userStageGroup.CurrentStageId;
        }

        public int GetLastStageId()
        {
            return userStageGroup.LastStageId;
        }
    }
}
