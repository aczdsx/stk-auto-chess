using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using MemoryPack;
using R3;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 클라이언트 데이터 매니저
    /// 서버의 PlayerData 서비스를 통해 저장/조회되는 클라이언트 측 데이터 관리
    /// MemoryPack으로 직렬화/역직렬화, 역직렬화된 객체는 캐싱
    /// LateUpdate에서 Dirty 데이터 자동 저장
    /// </summary>
    public class ClientDataManager : SingletonMonoBehaviour<ClientDataManager>
    {
        // 카테고리별 원본 바이트 데이터 (Category -> byte[])
        private readonly Dictionary<string, byte[]> _dataByCategory = new();

        // 카테고리별 역직렬화된 객체 캐시 (Category -> ClientDataBase)
        private readonly Dictionary<string, ClientDataBase> _cachedObjects = new();

        // Dirty 상태인 데이터 목록
        private readonly HashSet<ClientDataBase> _dirtySet = new();

        // 저장 중 여부
        private bool _isSaving;

        // 저장 중 추가된 Dirty 데이터 (다음 프레임에 저장)
        private readonly HashSet<ClientDataBase> _pendingDirtySet = new();

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<string> OnCategoryUpdated = new();

        private void LateUpdate()
        {
            if (_dirtySet.Count > 0 && !_isSaving)
            {
                FlushDirtyAsync().Forget();
            }
        }

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _dataByCategory.Clear();
            _cachedObjects.Clear();
            _dirtySet.Clear();
            _pendingDirtySet.Clear();
            _isSaving = false;
            OnChanged.OnNext(Unit.Default);
        }

        #region 데이터 조회

        /// <summary>
        /// 카테고리별 데이터 조회 (캐시된 객체 반환, 없으면 역직렬화 후 캐싱, 데이터 없으면 새 인스턴스 생성)
        /// </summary>
        public T GetData<T>(string category) where T : ClientDataBase, new()
        {
            // 캐시된 객체가 있으면 반환
            if (_cachedObjects.TryGetValue(category, out var cached))
            {
                if (category == ClientStatisticsData.CategoryName)
                    Debug.Log($"[GetData] {category} → HIT CACHE");
                return cached as T ?? CreateAndCache<T>(category);
            }

            // 원본 바이트 데이터 확인
            if (!_dataByCategory.TryGetValue(category, out var bytes) || bytes == null || bytes.Length == 0)
            {
                if (category == ClientStatisticsData.CategoryName)
                    Debug.Log($"[GetData] {category} → NO DATA, CreateAndCache");
                return CreateAndCache<T>(category);
            }

            try
            {
                T deserialized = MemoryPackSerializer.Deserialize<T>(bytes);
                _cachedObjects[category] = deserialized;
                return deserialized;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ClientDataManager] Failed to deserialize data for category '{category}': {e.Message}");
                return CreateAndCache<T>(category);
            }
        }

        private T CreateAndCache<T>(string category) where T : ClientDataBase, new()
        {
            var newInstance = new T();
            _cachedObjects[category] = newInstance;
            return newInstance;
        }

        #endregion

        #region Dirty 관리 및 자동 저장

        /// <summary>
        /// 데이터를 Dirty로 마킹
        /// </summary>
        internal void MarkDirty(ClientDataBase data)
        {
            if (data == null) return;

            if (_isSaving)
            {
                // 저장 중이면 pending에 추가
                _pendingDirtySet.Add(data);
            }
            else
            {
                _dirtySet.Add(data);
            }
        }

        /// <summary>
        /// Dirty 데이터 즉시 저장
        /// </summary>
        private async UniTask FlushDirtyAsync()
        {
            if (_dirtySet.Count == 0 || _isSaving) return;

            _isSaving = true;

            using var _1 = ListPool<ClientDataBase>.Get(out var dirtyList);
            dirtyList.AddRange(_dirtySet);
            _dirtySet.Clear();

            using var _2 = DictionaryPool<string, byte[]>.Get(out var categoryData);

            for (int i = 0; i < dirtyList.Count; i++)
            {
                var data = dirtyList[i];
                try
                {
                    var bytes = MemoryPackSerializer.Serialize(data.GetType(), data);
                    categoryData[data.Category] = bytes;
                    _dataByCategory[data.Category] = bytes;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ClientDataManager] Failed to serialize data for category '{data.Category}': {e.Message}");
                }
            }

            if (categoryData.Count > 0)
            {
                try
                {
                    Debug.Log($"[ClientDataManager] Saving {categoryData.Count} categories: {string.Join(", ", categoryData.Keys)}");
                    await NetManager.Instance.ClientData.SetAsync(categoryData);
                    Debug.Log("[ClientDataManager] Save success");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ClientDataManager] Failed to save dirty data: {e.Message}");

                    // 저장 실패 시 다시 Dirty로 마킹
                    for (int i = 0; i < dirtyList.Count; i++)
                    {
                        _dirtySet.Add(dirtyList[i]);
                    }
                }
            }

            _isSaving = false;

            // 저장 중 추가된 Dirty 데이터를 메인 셋으로 이동
            if (_pendingDirtySet.Count > 0)
            {
                foreach (var data in _pendingDirtySet)
                {
                    _dirtySet.Add(data);
                }
                _pendingDirtySet.Clear();
            }
        }

        #endregion

        #region 내부용 메서드

        /// <summary>
        /// 서버에서 받은 바이트 데이터 설정 (캐시 무효화)
        /// </summary>
        internal void SetData(string category, byte[] data)
        {
            if (string.IsNullOrEmpty(category))
            {
                Debug.LogError("[ClientDataManager] Category cannot be null or empty");
                return;
            }

            _dataByCategory[category] = data;
            _cachedObjects.Remove(category); // 캐시 무효화
            OnCategoryUpdated.OnNext(category);
            OnChanged.OnNext(Unit.Default);
        }

        #endregion
    }
}
