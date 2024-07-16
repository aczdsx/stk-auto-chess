using System.Linq;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserQuest userQuest;

        public UserQuest UserQuest => userQuest;

        [Initialize(DataCategory.UserQuest, 9)]
        private void Initialize_QuestData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userQuest = new UserQuest();

                // 전체 퀘스트 리스트 생성 (일일)
                var allDailyQuestList = SpecDataManager.Instance.GetSpecQuestList(TermType.DAILY, true);
                foreach (var questData in allDailyQuestList)
                {
                    userQuest.UserQuestDailyDatas.Add(questData.quest_id, new UserQuestData
                    {
                        QuestId = questData.quest_id,
                        ActionCount = 0,
                        QuestStateType = (int)QuestStateType.WAIT,
                    });
                }

                // 전체 퀘스트 리스트 생성 (주간)
                var allweeklyQuestList = SpecDataManager.Instance.GetSpecQuestList(TermType.WEEKLY, true);
                foreach (var questData in allweeklyQuestList)
                {
                    userQuest.UserQuestWeeklyDatas.Add(questData.quest_id, new UserQuestData
                    {
                        QuestId = questData.quest_id,
                        ActionCount = 0,
                        QuestStateType = (int)QuestStateType.WAIT,
                    });
                }

                return;
            }

            userQuest = MessageUtility.FromBase64String<UserQuest>(data);

            UpdateQuestClearCount(false);
        }

        [Clear]
        private void Clear_QuestData()
        {
            userQuest = null;
        }

        public void SaveUserQuestData()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserQuest.ToCategoryString(), userQuest);
        }

        public void UpdateLastQuestRefreshTimeStamp(bool needSave)
        {
            userQuest.LastQuestRefreshTimestamp = TimeManager.Instance.UtcNowTimeStamp();

            if (needSave)
            {
                SaveUserQuestData();
            }
        }

        public void UpdateNextRefreshTimeStamp(TermType termType, bool needSave)
        {
            if (termType == TermType.DAILY)
            {
                userQuest.NextDailyRefreshTimestamp = TimeManager.Instance.TommorrowTimeStamp();
            }
            else if (termType == TermType.WEEKLY)
            {
                userQuest.NextWeeklyRefreshTimestamp = TimeManager.Instance.NextMondayTimeStamp();
            }

            if (needSave)
            {
                SaveUserQuestData();
            }
        }

        // 유저 퀘스트 데이터 세팅 (행동 횟수) - 퀘스트 타입
        public void SetUserQuestActionCount(QuestType questType, int actionValue, bool isAdd, bool needSave)
        {
            var specQuestDataList = SpecDataManager.Instance.GetSpecQuestList(questType);

            foreach (var questData in specQuestDataList)
            {
                if (questData.term_type == TermType.DAILY)
                {
                    if (userQuest.UserQuestDailyDatas.ContainsKey(questData.quest_id))
                    {
                        if (isAdd)
                        {
                            userQuest.UserQuestDailyDatas[questData.quest_id].ActionCount += actionValue;
                        }
                        else
                        {
                            userQuest.UserQuestDailyDatas[questData.quest_id].ActionCount = actionValue;
                        }

                        // 조건 충족 시 보상 수령 가능 상태로 변경
                        if (userQuest.UserQuestDailyDatas[questData.quest_id].ActionCount >= questData.need_count
                            && userQuest.UserQuestDailyDatas[questData.quest_id].QuestStateType != (int)QuestStateType.CLEAR)
                        {
                            userQuest.UserQuestDailyDatas[questData.quest_id].QuestStateType = (int)QuestStateType.REWARD;
                        }
                    }
                }
                else if (questData.term_type == TermType.WEEKLY)
                {
                    if (userQuest.UserQuestWeeklyDatas.ContainsKey(questData.quest_id))
                    {
                        if (isAdd)
                        {
                            userQuest.UserQuestWeeklyDatas[questData.quest_id].ActionCount += actionValue;
                        }
                        else
                        {
                            userQuest.UserQuestWeeklyDatas[questData.quest_id].ActionCount = actionValue;
                        }

                        // 조건 충족 시 보상 수령 가능 상태로 변경
                        if (userQuest.UserQuestWeeklyDatas[questData.quest_id].ActionCount >= questData.need_count
                            && userQuest.UserQuestWeeklyDatas[questData.quest_id].QuestStateType != (int)QuestStateType.CLEAR)
                        {
                            userQuest.UserQuestWeeklyDatas[questData.quest_id].QuestStateType = (int)QuestStateType.REWARD;
                        }
                    }
                }
            }

            if (needSave)
            {
                SaveUserQuestData();
            }
        }

        // 유저 퀘스트 데이터 세팅 (행동 횟수) - 퀘스트 ID
        public void SetUserQuestActionCount(int questID, int actionValue, bool isAdd, bool needSave)
        {
            SpecQuest specQuestData = SpecDataManager.Instance.GetSpecQuestData(questID);

            if (specQuestData.term_type == TermType.DAILY)
            {
                if (userQuest.UserQuestDailyDatas.ContainsKey(questID))
                {
                    if (isAdd)
                    {
                        userQuest.UserQuestDailyDatas[specQuestData.quest_id].ActionCount += actionValue;
                    }
                    else
                    {
                        userQuest.UserQuestDailyDatas[specQuestData.quest_id].ActionCount = actionValue;
                    }

                    // 조건 충족 시 보상 수령 가능 상태로 변경
                    if (userQuest.UserQuestDailyDatas[questID].ActionCount >= specQuestData.need_count
                        && userQuest.UserQuestDailyDatas[questID].QuestStateType != (int)QuestStateType.CLEAR)
                    {
                        userQuest.UserQuestDailyDatas[questID].QuestStateType = (int)QuestStateType.REWARD;
                    }
                }
            }
            else if (specQuestData.term_type == TermType.WEEKLY)
            {
                if (userQuest.UserQuestWeeklyDatas.ContainsKey(questID))
                {
                    if (isAdd)
                    {
                        userQuest.UserQuestWeeklyDatas[specQuestData.quest_id].ActionCount += actionValue;
                    }
                    else
                    {
                        userQuest.UserQuestWeeklyDatas[specQuestData.quest_id].ActionCount = actionValue;
                    }

                    // 조건 충족 시 보상 수령 가능 상태로 변경
                    if (userQuest.UserQuestWeeklyDatas[questID].ActionCount >= specQuestData.need_count
                        && userQuest.UserQuestWeeklyDatas[questID].QuestStateType != (int)QuestStateType.CLEAR)
                    {
                        userQuest.UserQuestWeeklyDatas[questID].QuestStateType = (int)QuestStateType.REWARD;
                    }
                }
            }

            if (needSave)
            {
                SaveUserQuestData();
            }
        }

        // 유저 퀘스트 데이터 세팅 (현재 퀘스트 상태) - 퀘스트 ID
        public void SetUserQuestState(int questID, QuestStateType stateType, bool needSave)
        {
            SpecQuest specQuestData = SpecDataManager.Instance.GetSpecQuestData(questID);

            if (specQuestData.term_type == TermType.DAILY)
            {
                if (userQuest.UserQuestDailyDatas.ContainsKey(questID))
                {
                    userQuest.UserQuestDailyDatas[questID].QuestStateType = (int)stateType;
                }
            }
            else if (specQuestData.term_type == TermType.WEEKLY)
            {
                if (userQuest.UserQuestWeeklyDatas.ContainsKey(questID))
                {
                    userQuest.UserQuestWeeklyDatas[questID].QuestStateType = (int)stateType;
                }
            }

            // 완료 요청 처리가 왔을 경우 퀘스트 클리어 마일스톤 업데이트
            if (stateType == QuestStateType.CLEAR)
            {
                UpdateQuestClearCount(!needSave);   // needSave가 true일 경우 함수외부에서 이미 저장 처리를 하기 때문에 false로 처리
            }

            if (needSave)
            {
                SaveUserQuestData();
            }
        }

        public UserQuestData GetUserQuestData(int questID)
        {
            SpecQuest specQuestData = SpecDataManager.Instance.GetSpecQuestData(questID);

            if (specQuestData.term_type == TermType.DAILY)
            {
                if (userQuest.UserQuestDailyDatas.TryGetValue(questID, out var resultData))
                {
                    return resultData;
                }
            }
            else if (specQuestData.term_type == TermType.WEEKLY)
            {
                if (userQuest.UserQuestWeeklyDatas.TryGetValue(questID, out var resultData))
                {
                    return resultData;
                }
            }

            return null;
        }

        // 클리어 한 퀘스트 갯수 반환 (마일스톤 제외)
        public int GetQuestClearCount(TermType termType)
        {
            int resultCount = 0;

            // 일일 퀘스트 체크
            if (termType == TermType.DAILY)
            {
                foreach (var questData in userQuest.UserQuestDailyDatas)
                {
                    var specQuestData = SpecDataManager.Instance.GetSpecQuestData(questData.Key);
                    if (specQuestData.quest_type == QuestType.CLEAR_DAILY_QUEST) continue;

                    if (questData.Value.QuestStateType == (int)QuestStateType.CLEAR)
                    {
                        resultCount++;
                    }
                }
            }
            else if (termType == TermType.WEEKLY)
            {
                // 주간 퀘스트 체크
                foreach (var questData in userQuest.UserQuestWeeklyDatas)
                {
                    var specQuestData = SpecDataManager.Instance.GetSpecQuestData(questData.Key);
                    if (specQuestData.quest_type == QuestType.CLEAR_WEEKLY_QUEST) continue;

                    if (questData.Value.QuestStateType == (int)QuestStateType.CLEAR)
                    {
                        resultCount++;
                    }
                }
            }

            return resultCount;
        }

        public void ResetQuestDataList(TermType termType)
        {
            if (termType == TermType.DAILY)
            {
                foreach (var questData in userQuest.UserQuestDailyDatas)
                {
                    userQuest.UserQuestDailyDatas[questData.Key].ActionCount = 0;
                    userQuest.UserQuestDailyDatas[questData.Key].QuestStateType = (int)QuestStateType.WAIT;
                }
            }
            else if (termType == TermType.WEEKLY)
            {
                foreach (var questData in userQuest.UserQuestWeeklyDatas)
                {
                    userQuest.UserQuestWeeklyDatas[questData.Key].ActionCount = 0;
                    userQuest.UserQuestWeeklyDatas[questData.Key].QuestStateType = (int)QuestStateType.WAIT;
                }
            }
        }

        // 일일/주간 퀘스트 클리어 마일스톤 데이터 업데이트
        private void UpdateQuestClearCount(bool needSave)
        {
            // 일일 퀘스트 체크
            int dailyClearCount = 0;
            foreach (var questData in userQuest.UserQuestDailyDatas)
            {
                var specQuestData = SpecDataManager.Instance.GetSpecQuestData(questData.Key);
                if (specQuestData.quest_type == QuestType.CLEAR_DAILY_QUEST) continue;

                if (questData.Value.QuestStateType == (int)QuestStateType.CLEAR)
                {
                    dailyClearCount++;
                }
            }
            SetUserQuestActionCount(QuestType.CLEAR_DAILY_QUEST, dailyClearCount, false, needSave);

            // 주간 퀘스트 체크
            int weeklyClearCount = 0;
            foreach (var questData in userQuest.UserQuestWeeklyDatas)
            {
                var specQuestData = SpecDataManager.Instance.GetSpecQuestData(questData.Key);
                if (specQuestData.quest_type == QuestType.CLEAR_WEEKLY_QUEST) continue;

                if (questData.Value.QuestStateType == (int)QuestStateType.CLEAR)
                {
                    weeklyClearCount++;
                }
            }
            SetUserQuestActionCount(QuestType.CLEAR_WEEKLY_QUEST, weeklyClearCount, false, needSave);
        }
    }
}
