using System;
using System.Collections.Generic;
using CookApps.TeamBattle;

namespace CookApps.AutoBattler.Data
{
    /// <summary>
    /// 데이터 변경 이벤트를 관리하는 이벤트 버스
    /// Reflection 없이 Dictionary 기반으로 직접 관리
    /// 메모리 효율성을 위해 구독자 수 제한
    /// </summary>
    public class DataEventBus : Singleton<DataEventBus>
    {
        private const int MAX_SUBSCRIBERS_PER_CATEGORY = 32;

        // 카테고리별 구독자 목록
        private readonly Dictionary<string, List<Action<DataChangeEvent>>> _subscribers;

        // 전역 구독자 (모든 데이터 변경 감지)
        private readonly List<Action<DataChangeEvent>> _globalSubscribers;

        // 이벤트 큐 (배치 처리용)
        private readonly Queue<DataChangeEvent> _eventQueue;

        // 재사용 가능한 리스트 풀
        private readonly Stack<List<Action<DataChangeEvent>>> _listPool;

        public DataEventBus()
        {
            _subscribers = new Dictionary<string, List<Action<DataChangeEvent>>>(32);
            _globalSubscribers = new List<Action<DataChangeEvent>>(16);
            _eventQueue = new Queue<DataChangeEvent>(64);
            _listPool = new Stack<List<Action<DataChangeEvent>>>(8);
        }

        /// <summary>
        /// 특정 카테고리 구독
        /// </summary>
        public void Subscribe(string categoryKey, Action<DataChangeEvent> handler)
        {
            if (handler == null) return;

            if (!_subscribers.TryGetValue(categoryKey, out var handlers))
            {
                handlers = GetOrCreateList();
                _subscribers[categoryKey] = handlers;
            }

            if (handlers.Count >= MAX_SUBSCRIBERS_PER_CATEGORY)
            {
                UnityEngine.Debug.LogWarning($"[DataEventBus] Max subscribers reached for category: {categoryKey}");
                return;
            }

            // 중복 방지 (for문 사용, Linq 지양)
            for (int i = 0; i < handlers.Count; i++)
            {
                if (handlers[i] == handler)
                    return;
            }

            handlers.Add(handler);
        }

        /// <summary>
        /// 특정 카테고리 구독 해제
        /// </summary>
        public void Unsubscribe(string categoryKey, Action<DataChangeEvent> handler)
        {
            if (handler == null) return;

            if (_subscribers.TryGetValue(categoryKey, out var handlers))
            {
                handlers.Remove(handler);

                if (handlers.Count == 0)
                {
                    _subscribers.Remove(categoryKey);
                    ReturnList(handlers);
                }
            }
        }

        /// <summary>
        /// 전역 구독 (모든 데이터 변경)
        /// </summary>
        public void SubscribeGlobal(Action<DataChangeEvent> handler)
        {
            if (handler == null) return;

            // 중복 방지
            for (int i = 0; i < _globalSubscribers.Count; i++)
            {
                if (_globalSubscribers[i] == handler)
                    return;
            }

            _globalSubscribers.Add(handler);
        }

        /// <summary>
        /// 전역 구독 해제
        /// </summary>
        public void UnsubscribeGlobal(Action<DataChangeEvent> handler)
        {
            if (handler == null) return;
            _globalSubscribers.Remove(handler);
        }

        /// <summary>
        /// 이벤트 발행 (즉시 실행)
        /// </summary>
        public void Publish(DataChangeEvent changeEvent)
        {
            // 카테고리별 구독자에게 알림
            if (_subscribers.TryGetValue(changeEvent.CategoryKey, out var handlers))
            {
                for (int i = 0; i < handlers.Count; i++)
                {
                    try
                    {
                        handlers[i]?.Invoke(changeEvent);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[DataEventBus] Error in subscriber: {ex.Message}");
                    }
                }
            }

            // 전역 구독자에게 알림
            for (int i = 0; i < _globalSubscribers.Count; i++)
            {
                try
                {
                    _globalSubscribers[i]?.Invoke(changeEvent);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[DataEventBus] Error in global subscriber: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 이벤트 큐에 추가 (배치 처리용)
        /// </summary>
        public void Enqueue(DataChangeEvent changeEvent)
        {
            _eventQueue.Enqueue(changeEvent);
        }

        /// <summary>
        /// 큐에 있는 모든 이벤트 처리
        /// </summary>
        public void ProcessQueue()
        {
            while (_eventQueue.Count > 0)
            {
                var changeEvent = _eventQueue.Dequeue();
                Publish(changeEvent);
            }
        }

        /// <summary>
        /// 모든 구독 해제
        /// </summary>
        public void Clear()
        {
            foreach (var list in _subscribers.Values)
            {
                list.Clear();
                ReturnList(list);
            }
            _subscribers.Clear();
            _globalSubscribers.Clear();
            _eventQueue.Clear();
        }

        /// <summary>
        /// 리스트 풀에서 가져오기 (메모리 최적화)
        /// </summary>
        private List<Action<DataChangeEvent>> GetOrCreateList()
        {
            if (_listPool.Count > 0)
            {
                var list = _listPool.Pop();
                list.Clear();
                return list;
            }
            return new List<Action<DataChangeEvent>>(8);
        }

        /// <summary>
        /// 리스트 풀에 반환 (메모리 최적화)
        /// </summary>
        private void ReturnList(List<Action<DataChangeEvent>> list)
        {
            if (_listPool.Count < 8)
            {
                list.Clear();
                _listPool.Push(list);
            }
        }
    }
}