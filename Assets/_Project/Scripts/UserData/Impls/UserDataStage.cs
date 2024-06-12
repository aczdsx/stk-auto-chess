using System.Collections.Generic;
using System.Linq;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;
using Google.Protobuf.Collections;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserStageGroup userStageGroup;

        public UserStageGroup UserStageGroup => userStageGroup;

        // Cached Data
        private Dictionary<int, Dictionary<int, UserStage>> _chapterUserStageDic = new(); // chapter_id, stage_id, UserStage
        private Dictionary<DifficultyType, Dictionary<int, UserStage>> _difficultyUserStageDic = new(); // difficulty_type, stage_id, UserStage

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

            UpdateAllCacheData();
        }

        [Clear]
        private void Clear_StageGroup()
        {
            userStageGroup = null;
        }

        // 최근 플레이한 스테이지 ID 저장
        public void SetLastPlayStageID(int stageID, bool needSave)
        {
            userStageGroup.LastPlayStageId = stageID;

            if (needSave)
            {
                SaveUserStage();
            }
        }

        // 최근 플레이한 스테이지 ID 저장
        public int GetLastPlayStageID()
        {
            return userStageGroup.LastPlayStageId;
        }

        public void SetUserStage(int stageID, int starCount)
        {
            // 유저 데이터 저장
            if (userStageGroup.UserStages.TryGetValue(stageID, out UserStage userStage))
            {
                userStage.StarCount = starCount;
            }
            else
            {
                userStageGroup.UserStages.Add(stageID, new UserStage {StageId = stageID, StarCount = starCount});
            }

            // 캐시 데이터 업데이트
            UpdateTargetCacheData(stageID, starCount);

            SaveUserStage();
        }

        public UserStage GetUserStage(int stageId)
        {
            if (userStageGroup.UserStages.TryGetValue(stageId, out UserStage userStage))
            {
                return userStage;
            }

            return null;
        }

        // 스테이지 별 누적 보상 상태 데이터 저장
        public void SetStageAccRewardState(int chapterID, DifficultyType difficultyType, int targetAccCount)
        {
            if (userStageGroup.UserStageAccRewards.ContainsKey(chapterID) == false)
            {
                userStageGroup.UserStageAccRewards.Add(chapterID, new UserStageAccRewardDic());

                if (userStageGroup.UserStageAccRewards[chapterID].StageAccRewardDic.ContainsKey((int) difficultyType) == false)
                {
                    userStageGroup.UserStageAccRewards[chapterID].StageAccRewardDic.Add((int)difficultyType, new UserStageAccRewardList());
                }
            }

            userStageGroup.UserStageAccRewards[chapterID].StageAccRewardDic[(int)difficultyType].StageAccRewardList.Add(targetAccCount);

            SaveUserStage();
        }

        // 스테이지 별 누적 보상 획득 여부 체크
        public bool IsGetStageAccReward(int chapterID, DifficultyType difficultyType, int targetAccCount)
        {
            if (userStageGroup.UserStageAccRewards.ContainsKey(chapterID) == false)
            {
                return false;
            }

            if (userStageGroup.UserStageAccRewards[chapterID].StageAccRewardDic.ContainsKey((int) difficultyType) == false)
            {
                return false;
            }

            return userStageGroup.UserStageAccRewards[chapterID].StageAccRewardDic[(int)difficultyType].StageAccRewardList.Contains(targetAccCount);
        }

        public UserStage GetLastUserStageByChapter(int chapterID)
        {
            if (_chapterUserStageDic.TryGetValue(chapterID, out Dictionary<int, UserStage> stageDic))
            {
                return stageDic.Values.Last();
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
        public bool IsChapterOpen(int chapterID, DifficultyType type)
        {
            if (chapterID <= 1) return true;

            int prevChapterID = chapterID - 1;  // 이전 챕터 ID

            var lastStageData = SpecDataManager.Instance.GetLastStageData(prevChapterID, type);
            if (lastStageData != null)
            {
                if (_chapterUserStageDic.ContainsKey(lastStageData.chapter_id))
                {
                    if (_chapterUserStageDic[lastStageData.chapter_id].ContainsKey(lastStageData.id))
                    {
                        return _chapterUserStageDic[lastStageData.chapter_id][lastStageData.id].StarCount > 0;
                    }
                }
            }

            return false;
        }

        // 스테이지 개방 여부 확인
        public bool IsStageOpen(int stageID)
        {
            var specData = SpecDataManager.Instance.SpecStage.Get(stageID);
            if (specData == null) return false;

            if (specData.stage_number == 1) return true;    // 1스테이지는 무조건 개방

            int prevStageNumber = specData.stage_number - 1;
            var prevSpecData = SpecDataManager.Instance.GetStageData(specData.chapter_id, prevStageNumber, specData.difficulty_type);
            if (prevSpecData != null)
            {
                if (_chapterUserStageDic.ContainsKey(prevSpecData.chapter_id))
                {
                    if (_chapterUserStageDic[prevSpecData.chapter_id].ContainsKey(prevSpecData.id))
                    {
                        return _chapterUserStageDic[prevSpecData.chapter_id][prevSpecData.id].StarCount > 0;
                    }
                }
            }

            return false;
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

        public void SaveUserStage()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserStageGroup.ToCategoryString(), userStageGroup);
        }


        /////////////////////////////////////////////////////////////

        // 캐시 데이터 전체 업데이트
        private void UpdateAllCacheData()
        {
            foreach (var stageDic in UserStageGroup.UserStages)
            {
                var specData = SpecDataManager.Instance.SpecStage.Get(stageDic.Value.StageId);
                if (specData == null) continue;

                // 챕터 캐시 데이터 업데이트
                if (_chapterUserStageDic.ContainsKey(specData.chapter_id) == false)
                {
                    _chapterUserStageDic.Add(specData.chapter_id, new Dictionary<int, UserStage>());
                }

                if (_chapterUserStageDic[specData.chapter_id].ContainsKey(specData.id) == false)
                {
                    _chapterUserStageDic[specData.chapter_id].Add(specData.id, stageDic.Value);
                }
                else
                {
                    _chapterUserStageDic[specData.chapter_id][specData.id] = stageDic.Value;
                }

                // 난이도 캐시 데이터 업데이트
                if (_difficultyUserStageDic.ContainsKey(specData.difficulty_type) == false)
                {
                    _difficultyUserStageDic.Add(specData.difficulty_type, new Dictionary<int, UserStage>());
                }

                if (_difficultyUserStageDic[specData.difficulty_type].ContainsKey(specData.id) == false)
                {
                    _difficultyUserStageDic[specData.difficulty_type].Add(specData.id, stageDic.Value);
                }
                else
                {
                    _difficultyUserStageDic[specData.difficulty_type][specData.id] = stageDic.Value;
                }
            }
        }

        // 특정 캐시 데이터 업데이트
        private void UpdateTargetCacheData(int targetStageID, int targetStarCount)
        {
            var specData = SpecDataManager.Instance.SpecStage.Get(targetStageID);
            if (specData == null) return;

            UserStage newStageData = new UserStage();
            newStageData.StageId = targetStageID;
            newStageData.StarCount = targetStarCount;

            // 챕터 캐시 데이터 업데이트
            if (_chapterUserStageDic.ContainsKey(specData.chapter_id) == false)
            {
                _chapterUserStageDic.Add(specData.chapter_id, new Dictionary<int, UserStage>());
            }

            if (_chapterUserStageDic[specData.chapter_id].ContainsKey(specData.id) == false)
            {
                _chapterUserStageDic[specData.chapter_id].Add(specData.id, newStageData);
            }
            else
            {
                _chapterUserStageDic[specData.chapter_id][specData.id] = newStageData;
            }

            // 난이도 캐시 데이터 업데이트
            if (_difficultyUserStageDic.ContainsKey(specData.difficulty_type) == false)
            {
                _difficultyUserStageDic.Add(specData.difficulty_type, new Dictionary<int, UserStage>());
            }

            if (_difficultyUserStageDic[specData.difficulty_type].ContainsKey(specData.id) == false)
            {
                _difficultyUserStageDic[specData.difficulty_type].Add(specData.id, newStageData);
            }
            else
            {
                _difficultyUserStageDic[specData.difficulty_type][specData.id] = newStageData;
            }
        }
    }
}
