using System.Linq;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserEvent userEvent;

        public UserEvent UserEvent => userEvent;

        [Initialize(DataCategory.UserEvent, 10)]
        private void Initialize_EventData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userEvent = new UserEvent();

                UpdateRecentEventData();
                UpdateAllEventTimeData(true);

                return;
            }

            userEvent = MessageUtility.FromBase64String<UserEvent>(data);

            UpdateRecentEventData();
            UpdateAllEventTimeData(false);
        }

        [Clear]
        private void Clear_EventData()
        {
            userEvent = null;
        }

        public void SaveUserEventData()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserEvent.ToCategoryString(), userEvent);
        }

        public UserEventData GetUserEventData(int eventID)
        {
            if (UserEvent.UserEventDatas.ContainsKey(eventID))
            {
                return UserEvent.UserEventDatas[eventID];
            }

            return null;
        }

        public UserEventConditionData GetUserEventConditionData(int eventID, int eventConditionID)
        {
            if (UserEvent.UserEventDatas.ContainsKey(eventID))
            {
                if (UserEvent.UserEventDatas[eventID].UserEventConditionDatas.ContainsKey(eventConditionID))
                {
                    return UserEvent.UserEventDatas[eventID].UserEventConditionDatas[eventConditionID];
                }
            }

            return null;
        }

        public List<UserEventConditionData> GetUserEventConditionDataList(int eventID)
        {
            if (UserEvent.UserEventDatas.ContainsKey(eventID))
            {
                return UserEvent.UserEventDatas[eventID].UserEventConditionDatas.Values.ToList();
            }

            return null;
        }

        public void SetUserEventActionCount(int eventID, int actionValue, bool needSave)
        {
            if (UserEvent.UserEventDatas.ContainsKey(eventID))
            {
                UserEvent.UserEventDatas[eventID].ActionCount += actionValue;

                // 조건 충족 시 보상 수령 가능 상태로 condition State 변경
                UpdateUserEventConditionState(eventID);

                if (needSave)
                {
                    SaveUserEventData();
                }
            }
        }

        public void SetUserEventConditionState(int eventID, int eventConditionID, EventStateType stateType, bool needSave)
        {
            var specEventConditionData = SpecDataManager.Instance.GetSpecEventConditionData(eventID, eventConditionID);

            if (UserEvent.UserEventDatas.ContainsKey(eventID))
            {
                if (UserEvent.UserEventDatas[eventID].UserEventConditionDatas.ContainsKey(eventConditionID))
                {
                    UserEvent.UserEventDatas[eventID].UserEventConditionDatas[eventConditionID].EventStateType = (int)stateType;

                    if (needSave)
                    {
                        SaveUserEventData();
                    }
                }
            }
        }

        // 현재 이벤트 기간에 맞춰 유저 이벤트 데이터를 갱신
        public void UpdateRecentEventData()
        {
            // 서비스 중인 동안 계속 반복 되거나, 운영 기간에 해당하는 이벤트 데이터를 세팅
            var currentSpecEventDataList = SpecDataManager.Instance.GetCurrentSpecEventList();
            foreach (var currentEventData in currentSpecEventDataList)
            {
                // 이벤트가 없다면 생성
                if (UserEvent.UserEventDatas.ContainsKey(currentEventData.event_id) == false)
                {
                    UserEventData newUserEventData = new UserEventData();
                    newUserEventData.EventId = currentEventData.event_id;

                    UserEvent.UserEventDatas.Add(currentEventData.event_id, newUserEventData);

                    var specEventConditionDataList = SpecDataManager.Instance.GetSpecEventConditionList(currentEventData.event_id);
                    foreach (var specEventConditionData in specEventConditionDataList)
                    {
                        UserEventConditionData newUserEventConditionData = new UserEventConditionData
                        {
                            EventConditionId = specEventConditionData.event_condition_id,
                            EventStateType = (int)EventStateType.WAIT,
                        };

                        UserEvent.UserEventDatas[currentEventData.event_id].UserEventConditionDatas.Add(specEventConditionData.event_condition_id, newUserEventConditionData);
                    }
                }
            }

            SaveUserEventData();
        }

        // 특정 이벤트 갱신 시간 업데이트
        public void UpdateEventTimeData(int eventID)
        {
            // 서비스 중인 동안 계속 반복 되거나, 운영 기간에 해당하는 이벤트 데이터를 세팅
            var currentEventData = SpecDataManager.Instance.GetSpecEventData(eventID);
            if (UserEvent.UserEventDatas.ContainsKey(currentEventData.event_id))
            {
                // 출석 이벤트 처리
                if (currentEventData.event_type == EventType.ATTENDANCE)
                {
                    long extraRefreshTimestamp = TimeManager.Instance.TommorrowTimeStamp();

                    UserEvent.UserEventDatas[currentEventData.event_id].EventExtraRefreshTimestamp = extraRefreshTimestamp;
                }

                // 나머지 기간제 이벤트 처리
                if (currentEventData.term_type == TermType.DAILY)
                {
                    UserEvent.UserEventDatas[currentEventData.event_id].EventRefreshTimestamp = TimeManager.Instance.TommorrowTimeStamp();
                }
                else if (currentEventData.term_type == TermType.WEEKLY)
                {
                    UserEvent.UserEventDatas[currentEventData.event_id].EventRefreshTimestamp = TimeManager.Instance.NextMondayTimeStamp();
                }
            }

            SaveUserEventData();
        }

        // 전체 벤트 갱신 시간 업데이트
        public void UpdateAllEventTimeData(bool isFirstInit)
        {
            // 서비스 중인 동안 계속 반복 되거나, 운영 기간에 해당하는 이벤트 데이터를 세팅
            var currentSpecEventDataList = SpecDataManager.Instance.GetCurrentSpecEventList();
            foreach (var currentEventData in currentSpecEventDataList)
            {
                if (UserEvent.UserEventDatas.ContainsKey(currentEventData.event_id) == false) continue;

                // 출석 이벤트 처리
                if (currentEventData.event_type == EventType.ATTENDANCE)
                {
                    long extraRefreshTimestamp = TimeManager.Instance.TommorrowTimeStamp();
                    if (isFirstInit)
                    {
                        extraRefreshTimestamp = TimeManager.Instance.DefaultTimeStamp();
                    }

                    UserEvent.UserEventDatas[currentEventData.event_id].EventExtraRefreshTimestamp = extraRefreshTimestamp;
                }

                // 나머지 기간제 이벤트 처리
                if (currentEventData.term_type == TermType.DAILY)
                {
                    UserEvent.UserEventDatas[currentEventData.event_id].EventRefreshTimestamp = TimeManager.Instance.TommorrowTimeStamp();
                }
                else if (currentEventData.term_type == TermType.WEEKLY)
                {
                    UserEvent.UserEventDatas[currentEventData.event_id].EventRefreshTimestamp = TimeManager.Instance.NextMondayTimeStamp();
                }
            }

            SaveUserEventData();
        }

        // 이벤트 데이터 기준에 맞춰 condition State 업데이트
        private void UpdateUserEventConditionState(int eventID)
        {
            if (UserEvent.UserEventDatas.ContainsKey(eventID))
            {
                var eventConditionList = SpecDataManager.Instance.GetSpecEventConditionList(eventID);
                foreach (var eventCondition in eventConditionList)
                {
                    if (UserEvent.UserEventDatas[eventID].UserEventConditionDatas.ContainsKey(eventCondition.event_condition_id))
                    {
                        if (UserEvent.UserEventDatas[eventID].ActionCount >= eventCondition.need_count
                            && UserEvent.UserEventDatas[eventID].UserEventConditionDatas[eventCondition.event_condition_id].EventStateType != (int)EventStateType.REWARD
                            && UserEvent.UserEventDatas[eventID].UserEventConditionDatas[eventCondition.event_condition_id].EventStateType != (int)EventStateType.CLEAR)
                        {
                            UserEvent.UserEventDatas[eventID].UserEventConditionDatas[eventCondition.event_condition_id].EventStateType = (int)EventStateType.REWARD;
                        }
                    }
                }
            }
        }
    }
}
