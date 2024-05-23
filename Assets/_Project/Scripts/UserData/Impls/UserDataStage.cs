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
                    CurrentSelectedChapterId = 1,
                    CurrentNormalStageId = 1,
                    CurrentHardStageId = 0,
                    LastNormalStageId = 2,
                    LastHardStageId = 0,
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

        public int GetTotalChapterStarCount(int chapterID, DifficultyType type)
        {
            int totalStarCount = 0;

            foreach (var userStage in userStageGroup.UserStages.Values)
            {
                var specStage = SpecDataManager.Instance.Stage.Get(userStage.StageId);
                if (specStage != null)
                {
                    if (specStage.chapter_id == chapterID && specStage.difficulty == type)
                    {
                        totalStarCount += userStage.StarCount;
                    }
                }
            }

            return totalStarCount;
        }

        // 현재 진행 중인 스테이지 ID 반환 (현재 선택중인 챕터 기반)
        public int GetCurrentStageId()
        {
            var chapterSpecData = SpecDataManager.Instance.Chapter.Get(userStageGroup.CurrentSelectedChapterId);

            if (chapterSpecData != null)
            {
                switch (chapterSpecData.difficulty)
                {
                    case DifficultyType.NORMAL:
                        return userStageGroup.CurrentNormalStageId;
                    case DifficultyType.HARD:
                        return userStageGroup.CurrentHardStageId;
                }
            }

            return 0;
        }

        // 진행 가능한 최대 스테이지 ID 반환
        public int GetLastStageId(DifficultyType type)
        {
            switch (type)
            {
                case DifficultyType.NORMAL:
                    return userStageGroup.LastNormalStageId;
                case DifficultyType.HARD:
                    return userStageGroup.LastHardStageId;
            }

            return 0;
        }

        // 챕터 개방 여부 확인
        // public bool IsChapterOpen(int chapterID, DifficultyType type)
        // {
        //     int lastStageId = 0;
        //
        //     switch (type)
        //     {
        //         case DifficultyType.NORMAL:
        //             lastStageId = userStageGroup.LastNormalStageId;
        //             break;
        //         case DifficultyType.HARD:
        //             lastStageId = userStageGroup.LastHardStageId;
        //             break;
        //     }
        //
        //     var specStageData = SpecDataManager.Instance.Stage.Get(lastStageId);
        //     if (specStageData != null)
        //     {
        //
        //     }
        // }

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
