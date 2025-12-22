using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace CookApps.TeamBattle.UI
{
    /// <summary>
    /// Fluent Builder 패턴으로 다중 타입 TableView를 간단하게 설정
    /// </summary>
    public class MultiTypeTableViewController<TData> where TData : class
    {
        private readonly TableView tableView;
        private List<TData> dataList = new();

        // 타입별 Pool 관리
        private readonly Dictionary<int, ObjectPool<GameObject>> poolsByType = new();
        private readonly Dictionary<GameObject, int> cellTypeMap = new();

        // 설정 값들
        private Func<TData, int, int> viewTypeProvider;
        private readonly Dictionary<int, Func<GameObject>> cellFactories = new();
        private readonly Dictionary<int, Func<TData, int, Vector2>> cellSizeProviders = new();
        private readonly Dictionary<int, Action<GameObject, TData, int>> bindCallbacks = new();
        private readonly Dictionary<int, Action<GameObject>> onCellCreatedCallbacks = new();
        private readonly Dictionary<int, Action<GameObject>> onCellRecycledCallbacks = new();
        private readonly Dictionary<int, Action<GameObject>> onCellReleasedCallbacks = new();

        private bool isAttached = false;

        private MultiTypeTableViewController(TableView tableView)
        {
            this.tableView = tableView;
        }

        /// <summary>
        /// TableView에 대한 Controller 생성 (Fluent API 시작점)
        /// </summary>
        public static MultiTypeTableViewController<TData> Create(TableView tableView)
        {
            if (tableView == null)
            {
                Debug.LogError("TableView cannot be null");
                return null;
            }

            return new MultiTypeTableViewController<TData>(tableView);
        }

        /// <summary>
        /// 데이터 리스트 설정
        /// </summary>
        public MultiTypeTableViewController<TData> WithData(List<TData> data)
        {
            dataList = data ?? new List<TData>();
            return this;
        }

        /// <summary>
        /// 데이터로부터 셀 타입을 결정하는 함수 설정 (필수)
        /// </summary>
        public MultiTypeTableViewController<TData> WithViewTypeProvider(Func<TData, int, int> provider)
        {
            viewTypeProvider = provider;
            return this;
        }

        /// <summary>
        /// 특정 타입의 셀 Prefab 설정
        /// </summary>
        public MultiTypeTableViewController<TData> RegisterCellType(
            int viewType,
            GameObject prefab,
            Vector2 cellSize,
            Action<GameObject, TData, int> onBind = null)
        {
            if (prefab == null)
            {
                Debug.LogError($"Cell prefab for type {viewType} cannot be null");
                return this;
            }

            cellFactories[viewType] = () => UnityEngine.Object.Instantiate(prefab);
            cellSizeProviders[viewType] = (data, index) => cellSize;
            if (onBind != null)
            {
                bindCallbacks[viewType] = onBind;
            }

            return this;
        }

        /// <summary>
        /// 특정 타입의 셀 설정 (동적 크기)
        /// </summary>
        public MultiTypeTableViewController<TData> RegisterCellType(
            int viewType,
            GameObject prefab,
            Func<TData, int, Vector2> sizeProvider,
            Action<GameObject, TData, int> onBind = null)
        {
            if (prefab == null)
            {
                Debug.LogError($"Cell prefab for type {viewType} cannot be null");
                return this;
            }

            cellFactories[viewType] = () => UnityEngine.Object.Instantiate(prefab);
            cellSizeProviders[viewType] = sizeProvider;
            if (onBind != null)
            {
                bindCallbacks[viewType] = onBind;
            }

            return this;
        }

        /// <summary>
        /// 특정 타입의 셀 팩토리 함수로 설정
        /// </summary>
        public MultiTypeTableViewController<TData> RegisterCellType(
            int viewType,
            Func<GameObject> cellFactory,
            Func<TData, int, Vector2> sizeProvider,
            Action<GameObject, TData, int> onBind = null)
        {
            cellFactories[viewType] = cellFactory;
            cellSizeProviders[viewType] = sizeProvider;
            if (onBind != null)
            {
                bindCallbacks[viewType] = onBind;
            }

            return this;
        }

        /// <summary>
        /// 특정 타입의 셀 생성 시 콜백
        /// </summary>
        public MultiTypeTableViewController<TData> OnCellCreated(int viewType, Action<GameObject> callback)
        {
            onCellCreatedCallbacks[viewType] = callback;
            return this;
        }

        /// <summary>
        /// 특정 타입의 셀 재사용 시 콜백
        /// </summary>
        public MultiTypeTableViewController<TData> OnCellRecycled(int viewType, Action<GameObject> callback)
        {
            onCellRecycledCallbacks[viewType] = callback;
            return this;
        }

        /// <summary>
        /// 특정 타입의 셀 반환 시 콜백
        /// </summary>
        public MultiTypeTableViewController<TData> OnCellReleased(int viewType, Action<GameObject> callback)
        {
            onCellReleasedCallbacks[viewType] = callback;
            return this;
        }

        /// <summary>
        /// 설정 완료 및 TableView 연결
        /// </summary>
        public MultiTypeTableViewController<TData> Build()
        {
            // 필수 설정 검증
            if (viewTypeProvider == null)
            {
                Debug.LogError("ViewTypeProvider must be set using WithViewTypeProvider()");
                return this;
            }

            if (cellFactories.Count == 0)
            {
                Debug.LogError("At least one cell type must be registered using RegisterCellType()");
                return this;
            }

            // 이미 연결되어 있으면 경고
            if (isAttached)
            {
                Debug.LogWarning("Controller is already attached to TableView.");
                return this;
            }

            // TableView 이벤트 연결
            tableView.OnGetTotalCellItemCount += GetItemCount;
            tableView.OnGetCellItemSize += GetItemSize;
            tableView.OnGetCellItem += OnGetCellItemInternal;
            tableView.OnReleaseCellItem += OnReleaseCellItemInternal;

            isAttached = true;

            // 초기 새로고침
            Refresh();

            return this;
        }

        /// <summary>
        /// 데이터 설정 및 새로고침
        /// </summary>
        public void SetData(List<TData> data)
        {
            dataList = data ?? new List<TData>();
            Refresh();
        }

        /// <summary>
        /// 데이터 추가
        /// </summary>
        public void AddData(TData data)
        {
            dataList.Add(data);
            Refresh();
        }

        /// <summary>
        /// 데이터 제거
        /// </summary>
        public bool RemoveData(TData data)
        {
            bool removed = dataList.Remove(data);
            if (removed) Refresh();
            return removed;
        }

        /// <summary>
        /// 전체 데이터 클리어
        /// </summary>
        public void ClearData()
        {
            dataList.Clear();
            Refresh();
        }

        /// <summary>
        /// TableView 새로고침
        /// </summary>
        public void Refresh()
        {
            if (isAttached && tableView != null)
            {
                tableView.RefreshAll();
            }
        }

        /// <summary>
        /// 현재 데이터 리스트 반환
        /// </summary>
        public List<TData> GetDataList() => dataList;

        /// <summary>
        /// Controller 분리 및 정리
        /// </summary>
        public void Detach()
        {
            if (!isAttached) return;

            // 이벤트 해제
            if (tableView != null)
            {
                tableView.OnGetTotalCellItemCount -= GetItemCount;
                tableView.OnGetCellItemSize -= GetItemSize;
                tableView.OnGetCellItem -= OnGetCellItemInternal;
                tableView.OnReleaseCellItem -= OnReleaseCellItemInternal;
            }

            // 모든 Pool 정리
            foreach (var pool in poolsByType.Values)
            {
                pool?.Dispose();
            }
            poolsByType.Clear();
            cellTypeMap.Clear();

            isAttached = false;
        }

        // ===== 내부 구현 =====

        private ObjectPool<GameObject> GetOrCreatePool(int viewType)
        {
            if (!poolsByType.ContainsKey(viewType))
            {
                poolsByType[viewType] = new ObjectPool<GameObject>(
                    createFunc: () => CreateCellInternalForType(viewType),
                    actionOnGet: obj => obj.SetActive(true),
                    actionOnRelease: obj => obj.SetActive(false),
                    actionOnDestroy: UnityEngine.Object.Destroy,
                    collectionCheck: false
                );
            }
            return poolsByType[viewType];
        }

        private GameObject CreateCellInternalForType(int viewType)
        {
            if (!cellFactories.TryGetValue(viewType, out var factory))
            {
                Debug.LogError($"No cell factory registered for view type {viewType}");
                return null;
            }

            GameObject go = factory?.Invoke();

            if (go != null)
            {
                // 타입 매핑 저장
                cellTypeMap[go] = viewType;

                if (tableView != null)
                {
                    go.transform.SetParent(tableView.content, false);
                }

                // 생성 콜백
                if (onCellCreatedCallbacks.TryGetValue(viewType, out var callback))
                {
                    callback?.Invoke(go);
                }
            }

            return go;
        }

        private int GetItemCount()
        {
            return dataList.Count;
        }

        private Vector2 GetItemSize(int index)
        {
            if (index < 0 || index >= dataList.Count)
                return Vector2.zero;

            TData data = dataList[index];
            int viewType = viewTypeProvider(data, index);

            if (cellSizeProviders.TryGetValue(viewType, out var sizeProvider))
            {
                return sizeProvider(data, index);
            }

            Debug.LogWarning($"No size provider for view type {viewType}");
            return Vector2.zero;
        }

        private GameObject OnGetCellItemInternal(int index)
        {
            if (index < 0 || index >= dataList.Count)
                return null;

            TData data = dataList[index];
            int viewType = viewTypeProvider(data, index);

            // 타입별 Pool에서 가져오기
            ObjectPool<GameObject> pool = GetOrCreatePool(viewType);
            GameObject go = pool.Get();

            if (go == null) return null;

            // 재사용 콜백
            if (onCellRecycledCallbacks.TryGetValue(viewType, out var recycledCallback))
            {
                recycledCallback?.Invoke(go);
            }

            // 바인딩 콜백
            if (bindCallbacks.TryGetValue(viewType, out var bindCallback))
            {
                bindCallback?.Invoke(go, data, index);
            }

            return go;
        }

        private void OnReleaseCellItemInternal(int index, GameObject obj)
        {
            if (obj == null) return;

            // 셀의 타입 확인
            if (cellTypeMap.TryGetValue(obj, out int viewType))
            {
                // 반환 콜백
                if (onCellReleasedCallbacks.TryGetValue(viewType, out var releasedCallback))
                {
                    releasedCallback?.Invoke(obj);
                }

                // 타입별 Pool에 반환
                if (poolsByType.TryGetValue(viewType, out var pool))
                {
                    pool.Release(obj);
                }
            }
        }
    }

    /// <summary>
    /// TableView Extension Methods for MultiType
    /// </summary>
    public static class MultiTypeTableViewExtensions
    {
        /// <summary>
        /// Multi-Type Fluent Builder 시작 (편의 메서드)
        /// </summary>
        public static MultiTypeTableViewController<TData> CreateMultiTypeController<TData>(this TableView tableView)
            where TData : class
        {
            return MultiTypeTableViewController<TData>.Create(tableView);
        }
    }
}
