using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 서버 데이터 관리 매니저
    /// 델타 업데이트 방식으로 데이터를 효율적으로 관리
    /// Reflection 없이 명시적 등록으로 타입 안전성 보장
    /// </summary>
    public class ServerDataManager : Singleton<ServerDataManager>
    {
        private const int INITIAL_CAPACITY = 32;

        // 카테고리별 데이터 저장소
        private readonly Dictionary<string, IDataModel> _dataStore;

        // 데이터 변경 추적
        private readonly Dictionary<string, int> _versionTracker;

        // 데이터 팩토리 (카테고리별 기본 인스턴스 생성)
        private readonly Dictionary<string, Func<IDataModel>> _factories;

        // 이벤트 버스
        private DataEventBus EventBus => DataEventBus.Instance;

        public ServerDataManager()
        {
            _dataStore = new Dictionary<string, IDataModel>(INITIAL_CAPACITY);
            _versionTracker = new Dictionary<string, int>(INITIAL_CAPACITY);
            _factories = new Dictionary<string, Func<IDataModel>>(INITIAL_CAPACITY);
        }

        /// <summary>
        /// 데이터 모델 팩토리 등록
        /// </summary>
        public void RegisterFactory<T>(string categoryKey, Func<T> factory) where T : IDataModel
        {
            if (string.IsNullOrEmpty(categoryKey))
            {
                Debug.LogError("[ServerDataManager] CategoryKey cannot be null or empty");
                return;
            }

            if (factory == null)
            {
                Debug.LogError($"[ServerDataManager] Factory cannot be null for {categoryKey}");
                return;
            }

            _factories[categoryKey] = () => factory();
        }

        /// <summary>
        /// 데이터 가져오기 (타입 안전)
        /// </summary>
        public T GetData<T>(string categoryKey) where T : class, IDataModel
        {
            if (_dataStore.TryGetValue(categoryKey, out var data))
            {
                return data as T;
            }

            // 데이터가 없으면 팩토리로 생성
            if (_factories.TryGetValue(categoryKey, out var factory))
            {
                var newData = factory() as T;
                if (newData != null)
                {
                    _dataStore[categoryKey] = newData;
                    _versionTracker[categoryKey] = 0;
                    return newData;
                }
            }

            Debug.LogWarning($"[ServerDataManager] Data not found for category: {categoryKey}");
            return null;
        }

        /// <summary>
        /// 데이터 존재 여부 확인
        /// </summary>
        public bool HasData(string categoryKey)
        {
            return _dataStore.ContainsKey(categoryKey);
        }

        /// <summary>
        /// 데이터 버전 가져오기
        /// </summary>
        public int GetVersion(string categoryKey)
        {
            return _versionTracker.TryGetValue(categoryKey, out var version) ? version : 0;
        }

        /// <summary>
        /// 서버로부터 받은 데이터로 전체 교체
        /// </summary>
        public void SetData<T>(string categoryKey, T newData) where T : class, IDataModel
        {
            if (newData == null)
            {
                Debug.LogError($"[ServerDataManager] Cannot set null data for {categoryKey}");
                return;
            }

            var oldData = _dataStore.TryGetValue(categoryKey, out var existing) ? existing : null;

            _dataStore[categoryKey] = newData;
            _versionTracker[categoryKey] = newData.Version;

            // 변경 이벤트 발행
            var changeEvent = new DataChangeEvent(
                categoryKey,
                oldData == null ? DataChangeFlags.Created : DataChangeFlags.Updated,
                oldData,
                newData
            );

            EventBus.Publish(changeEvent);
        }

        /// <summary>
        /// 델타 업데이트 적용 (변경된 부분만)
        /// </summary>
        public void ApplyDelta<T>(string categoryKey, T deltaData) where T : class, IDataModel
        {
            if (deltaData == null)
            {
                Debug.LogError($"[ServerDataManager] Cannot apply null delta for {categoryKey}");
                return;
            }

            T currentData = GetData<T>(categoryKey);
            if (currentData == null)
            {
                // 데이터가 없으면 새로 생성
                SetData(categoryKey, deltaData);
                return;
            }

            // 버전 체크 (중복 적용 방지)
            if (deltaData.Version <= currentData.Version)
            {
                Debug.LogWarning($"[ServerDataManager] Delta version mismatch for {categoryKey}. Current: {currentData.Version}, Delta: {deltaData.Version}");
                return;
            }

            var oldData = currentData;

            // 델타 적용
            currentData.ApplyDelta(deltaData);
            _versionTracker[categoryKey] = deltaData.Version;

            // 변경 이벤트 발행
            var changeEvent = new DataChangeEvent(
                categoryKey,
                DataChangeFlags.Updated,
                oldData,
                currentData
            );

            EventBus.Publish(changeEvent);
        }

        /// <summary>
        /// 데이터 삭제
        /// </summary>
        public void RemoveData(string categoryKey)
        {
            if (_dataStore.TryGetValue(categoryKey, out var data))
            {
                _dataStore.Remove(categoryKey);
                _versionTracker.Remove(categoryKey);

                var changeEvent = new DataChangeEvent(
                    categoryKey,
                    DataChangeFlags.Deleted,
                    data,
                    null
                );

                EventBus.Publish(changeEvent);
            }
        }

        /// <summary>
        /// 배치 업데이트 (여러 데이터를 한 번에 처리)
        /// 이벤트는 큐에 넣고 나중에 일괄 처리
        /// </summary>
        public void BeginBatchUpdate()
        {
            // 배치 업데이트 시작 (이벤트 큐 사용)
        }

        /// <summary>
        /// 배치 업데이트 완료 (큐에 있는 이벤트 일괄 처리)
        /// </summary>
        public void EndBatchUpdate()
        {
            EventBus.ProcessQueue();
        }

        /// <summary>
        /// 모든 데이터 초기화
        /// </summary>
        public void ClearAll()
        {
            _dataStore.Clear();
            _versionTracker.Clear();
            EventBus.Clear();
        }

        /// <summary>
        /// 데이터 유효성 검증 (디버그용)
        /// </summary>
        public bool ValidateAll()
        {
            bool allValid = true;

            foreach (var kvp in _dataStore)
            {
                if (!kvp.Value.Validate())
                {
                    Debug.LogError($"[ServerDataManager] Validation failed for {kvp.Key}");
                    allValid = false;
                }
            }

            return allValid;
        }

        /// <summary>
        /// 현재 저장된 데이터 개수
        /// </summary>
        public int DataCount => _dataStore.Count;

        /// <summary>
        /// 모든 카테고리 키 가져오기
        /// </summary>
        public void GetAllCategoryKeys(List<string> output)
        {
            if (output == null) return;

            output.Clear();
            foreach (var key in _dataStore.Keys)
            {
                output.Add(key);
            }
        }
    }
}