using System.Collections.Generic;
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
        }

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
