using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Universal;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserStageGroup userStageGroup;

        public UserStageGroup UserStageGroup => userStageGroup;

        [Initialize(DataCategory.UserStageGroup, 1)]
        private void Initialize_StageGroup(string data)
        {
            if (string.IsNullOrEmpty(data))
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

        public void SetUserStage(int stageID, int starCount)
        {
            if (userStageGroup.UserStages.TryGetValue(stageID, out UserStage userStage))
            {
                userStage.StarCount = starCount;
            }
            else
            {
                userStageGroup.UserStages.Add(stageID, new UserStage {StageId = stageID, StarCount = starCount});
            }
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

        // 해당 스테이지 클리어 여부 확인
        public bool IsClearStage(int stageID)
        {
            if (userStageGroup.UserStages.TryGetValue(stageID, out UserStage userStage))
            {
                return userStage.StarCount > 0;
            }

            return false;
        }
    }
}
