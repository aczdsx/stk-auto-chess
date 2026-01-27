using System.Collections.Generic;
using CookApps.TeamBattle;
using R3;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 이벤트 데이터 모델
    /// 진행 중인 이벤트 정보를 관리
    /// </summary>
    public class EventModel
    {
        // 이벤트 데이터 목록 (key: event_id)
        private readonly Dictionary<uint, EventData> _events = new();

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<uint> OnEventUpdated = new();

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _events.Clear();
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 이벤트 목록 설정
        /// </summary>
        internal void SetEvents(IEnumerable<EventData> events)
        {
            _events.Clear();

            if (events != null)
            {
                foreach (var eventData in events)
                {
                    _events[eventData.EventId] = eventData;
                }
            }

            OnChanged.OnNext(Unit.Default);
            RefreshEventBadges();
        }

        /// <summary>
        /// 단일 이벤트 데이터 업데이트
        /// </summary>
        internal void UpdateEvent(EventData eventData)
        {
            if (eventData == null) return;

            _events[eventData.EventId] = eventData;

            OnEventUpdated.OnNext(eventData.EventId);
            OnChanged.OnNext(Unit.Default);
            RefreshEventBadges();
        }

        #region 뱃지 갱신

        /// <summary>
        /// 이벤트 관련 뱃지 갱신
        /// </summary>
        private void RefreshEventBadges()
        {
            RefreshConsumeAPBadge();
            RefreshSessionTimeBadge();
        }

        /// <summary>
        /// Event/ConsumeAP 뱃지 갱신
        /// </summary>
        private void RefreshConsumeAPBadge()
        {
            const string path = "Event/ConsumeAP";

            if (HasClaimableEventReward(EventType.USE_AP))
            {
                BadgeManager.Instance.AddBadge(BadgeType.RedDot, path);
            }
            else
            {
                BadgeManager.Instance.RemoveBadge(BadgeType.RedDot, path);
            }
        }

        /// <summary>
        /// Event/SessionTime 뱃지 갱신
        /// </summary>
        private void RefreshSessionTimeBadge()
        {
            var specEvent = SpecDataManager.Instance.GetCurrentSpecEvent(EventType.ACC_PLAY_TIME);
            if (specEvent == null)
                return;

            var eventData = GetEvent(specEvent.event_id);
            if (eventData == null)
                return;

            var specConditions = SpecDataManager.Instance.GetSpecEventConditionList(specEvent.event_id);
            if (specConditions == null || specConditions.Count == 0)
                return;

            foreach (var specCondition in specConditions)
            {
                var path = $"Event/SessionTime/{specCondition.event_condition_id}";
                var conditionData = GetEventConditionData(eventData, (uint)specCondition.event_condition_id);
                var isClaimable = eventData.CurrentCount >= specCondition.need_count && (conditionData == null || !conditionData.IsRewarded);

                if (isClaimable)
                {
                    BadgeManager.Instance.AddBadge(BadgeType.RedDot, path);
                }
                else
                {
                    BadgeManager.Instance.RemoveBadge(BadgeType.RedDot, path);
                }
            }
        }

        /// <summary>
        /// 특정 이벤트 타입에 받을 수 있는 보상이 있는지 확인
        /// </summary>
        private bool HasClaimableEventReward(EventType eventType)
        {
            // 이벤트 기간 유효성 검증
            var specEvent = SpecDataManager.Instance.GetCurrentSpecEvent(eventType);
            if (specEvent == null)
            {
                return false;
            }

            // 서버 이벤트 데이터 조회
            var eventData = GetEvent(specEvent.event_id);
            if (eventData == null)
            {
                return false;
            }

            // 이벤트 조건 목록 조회
            var specConditions = SpecDataManager.Instance.GetSpecEventConditionList(specEvent.event_id);
            if (specConditions == null || specConditions.Count == 0)
            {
                return false;
            }

            // 받을 수 있는 보상이 있는지 확인
            for (int i = 0; i < specConditions.Count; i++)
            {
                var specCondition = specConditions[i];

                // 현재 카운트가 필요 카운트 이상인지 확인
                if (eventData.CurrentCount < specCondition.need_count)
                {
                    continue;
                }

                // 이미 보상을 받았는지 확인
                var conditionData = GetEventConditionData(eventData, (uint)specCondition.event_condition_id);
                if (conditionData != null && conditionData.IsRewarded)
                {
                    continue;
                }

                // 받을 수 있는 보상이 있음
                return true;
            }

            return false;
        }

        /// <summary>
        /// 이벤트 데이터에서 특정 조건 데이터 조회 (뱃지용)
        /// </summary>
        private EventConditionData GetEventConditionData(EventData eventData, uint conditionId)
        {
            for (int i = 0; i < eventData.Conditions.Count; i++)
            {
                if (eventData.Conditions[i].EventConditionId == conditionId)
                {
                    return eventData.Conditions[i];
                }
            }
            return null;
        }

        #endregion

        #region 조회 메서드

        /// <summary>
        /// 이벤트 데이터 조회
        /// </summary>
        public EventData GetEvent(uint eventId)
        {
            return _events.TryGetValue(eventId, out var data) ? data : null;
        }

        /// <summary>
        /// 이벤트 데이터 조회 (int)
        /// </summary>
        public EventData GetEvent(int eventId)
        {
            return GetEvent((uint)eventId);
        }

        /// <summary>
        /// 모든 이벤트 데이터 조회
        /// </summary>
        public IEnumerable<EventData> GetAllEvents()
        {
            return _events.Values;
        }

        /// <summary>
        /// 이벤트 존재 여부 확인
        /// </summary>
        public bool HasEvent(uint eventId)
        {
            return _events.ContainsKey(eventId);
        }

        /// <summary>
        /// 이벤트 조건 데이터 조회
        /// </summary>
        public EventConditionData GetEventCondition(uint eventId, uint eventConditionId)
        {
            var eventData = GetEvent(eventId);
            if (eventData == null) return null;

            for (int i = 0; i < eventData.Conditions.Count; i++)
            {
                if (eventData.Conditions[i].EventConditionId == eventConditionId)
                {
                    return eventData.Conditions[i];
                }
            }

            return null;
        }

        #endregion
    }
}
