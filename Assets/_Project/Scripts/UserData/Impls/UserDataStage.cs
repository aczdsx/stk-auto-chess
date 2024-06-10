using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserStageGroup userStageGroup;

        public UserStageGroup UserStageGroup => userStageGroup;

        // Cached Data
        private Dictionary<int, Dictionary<int, List<UserStage>>> _cachedUserStageDic = new(); // ChapterID -> DifficultyType -> UserStage List

        [Initialize(DataCategory.UserStageGroup, 1)]
        private void Initialize_StageGroup(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userStageGroup = new UserStageGroup
                {
                    LastPlayStageId = 1,
                };
                return;
            }

            userStageGroup = MessageUtility.FromBase64String<UserStageGroup>(data);
        }

        [Clear]
        private void Clear_StageGroup()
        {
            userStageGroup = null;
        }

        public void SetLastPlayStageID(int stageID, bool needSave)
        {
            userStageGroup.LastPlayStageId = stageID;

            if (needSave)
            {
                SaveUserStage();
            }
        }

        public int GetLastPlayStageID()
        {
            return userStageGroup.LastPlayStageId;
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

            return null;
        }

        public int GetTotalChapterStarCount(int chapterID, DifficultyType type)
        {
            int totalStarCount = 0;

            foreach (var userStage in userStageGroup.UserStages.Values)
            {
                var specStage = SpecDataManager.Instance.SpecStage.Get(userStage.StageId);
                if (specStage != null)
                {
                    if (specStage.chapter_id == chapterID && specStage.difficulty_type == type)
                    {
                        totalStarCount += userStage.StarCount;
                    }
                }
            }

            return totalStarCount;
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

        public void SaveUserStage()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserStageGroup.ToCategoryString(), userStageGroup);
        }
    }
}
