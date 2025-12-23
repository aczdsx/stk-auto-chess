using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace CookApps.TeamBattle.UI
{
    /// <summary>
    /// Fluent Builder 패턴으로 TableView를 간단하게 설정할 수 있는 Controller
    /// </summary>
    public class TableViewController<TData, TCell> where TCell : Component
    {
        private readonly TableView tableView;
        private List<TData> dataList = new();
        private ObjectPool<GameObject> pool;

        // 설정 값들
        private Func<GameObject> cellFactory;
        private Func<int, TData, Vector2> cellSizeProvider;
        private Action<TCell, TData, int> bindCallback;
        private Action<TCell> onCellCreatedCallback;
        private Action<TCell> onCellRecycledCallback;
        private Action<int, TCell> onCellReleasedCallback;

        private bool isAttached = false;

        private TableViewController(TableView tableView)
        {
            this.tableView = tableView;
        }

        /// <summary>
        /// TableView에 대한 Controller 생성 (Fluent API 시작점)
        /// </summary>
        internal static TableViewController<TData, TCell> Create(TableView tableView)
        {
            if (tableView == null)
            {
                Debug.LogError("TableView cannot be null");
                return null;
            }

            return new TableViewController<TData, TCell>(tableView);
        }

        /// <summary>
        /// 데이터 리스트 설정
        /// </summary>
        public TableViewController<TData, TCell> WithData(List<TData> data)
        {
            dataList = data ?? new List<TData>();
            return this;
        }

        /// <summary>
        /// 셀 Prefab 설정
        /// </summary>
        public TableViewController<TData, TCell> WithCellPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Cell prefab cannot be null");
                return this;
            }

            cellFactory = () => UnityEngine.Object.Instantiate(prefab);
            return this;
        }

        /// <summary>
        /// 셀 생성 팩토리 함수 설정
        /// </summary>
        public TableViewController<TData, TCell> WithCellFactory(Func<GameObject> factory)
        {
            cellFactory = factory;
            return this;
        }

        /// <summary>
        /// 고정 셀 크기 설정
        /// </summary>
        public TableViewController<TData, TCell> WithCellSize(Vector2 size)
        {
            cellSizeProvider = (index, data) => size;
            return this;
        }

        /// <summary>
        /// 동적 셀 크기 설정 (데이터 기반)
        /// </summary>
        public TableViewController<TData, TCell> WithCellSize(Func<TData, Vector2> sizeProvider)
        {
            cellSizeProvider = (index, data) => sizeProvider(data);
            return this;
        }

        /// <summary>
        /// 동적 셀 크기 설정 (데이터 + 인덱스 기반)
        /// </summary>
        public TableViewController<TData, TCell> WithCellSize(Func<TData, int, Vector2> sizeProvider)
        {
            cellSizeProvider = (index, data) => sizeProvider(data, index);
            return this;
        }

        /// <summary>
        /// 셀 바인딩 콜백 설정
        /// </summary>
        public TableViewController<TData, TCell> OnBind(Action<TCell, TData, int> bindCallback)
        {
            this.bindCallback = bindCallback;
            return this;
        }

        /// <summary>
        /// 셀 생성 시 콜백 설정
        /// </summary>
        public TableViewController<TData, TCell> OnCellCreated(Action<TCell> callback)
        {
            onCellCreatedCallback = callback;
            return this;
        }

        /// <summary>
        /// 셀 재사용 시 콜백 설정
        /// </summary>
        public TableViewController<TData, TCell> OnCellRecycled(Action<TCell> callback)
        {
            onCellRecycledCallback = callback;
            return this;
        }

        /// <summary>
        /// 셀 반환 시 콜백 설정
        /// </summary>
        public TableViewController<TData, TCell> OnCellReleased(Action<int, TCell> callback)
        {
            onCellReleasedCallback = callback;
            return this;
        }

        /// <summary>
        /// 설정 완료 및 TableView 연결
        /// </summary>
        public TableViewController<TData, TCell> Build()
        {
            // 필수 설정 검증
            if (cellFactory == null)
            {
                Debug.LogError("Cell factory must be set using WithCellPrefab() or WithCellFactory()");
                return this;
            }

            if (cellSizeProvider == null)
            {
                Debug.LogWarning("Cell size not set. Using default size (100, 100)");
                cellSizeProvider = (index, data) => new Vector2(100, 100);
            }

            // 이미 연결되어 있으면 경고
            if (isAttached)
            {
                Debug.LogWarning("Controller is already attached to TableView.");
                return this;
            }

            // Pool 초기화
            pool = new ObjectPool<GameObject>(
                createFunc: CreateCellInternal,
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: UnityEngine.Object.Destroy,
                collectionCheck: false
            );

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

            // Pool 정리
            pool?.Dispose();
            pool = null;

            isAttached = false;
        }

        // ===== 내부 구현 =====

        private GameObject CreateCellInternal()
        {
            GameObject go = cellFactory?.Invoke();

            if (go != null && tableView != null)
            {
                go.transform.SetParent(tableView.content, false);

                TCell cell = go.GetComponent<TCell>();
                if (cell != null)
                {
                    onCellCreatedCallback?.Invoke(cell);
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

            return cellSizeProvider?.Invoke(index, dataList[index]) ?? Vector2.zero;
        }

        private GameObject OnGetCellItemInternal(int index)
        {
            if (index < 0 || index >= dataList.Count)
                return null;

            GameObject go = pool.Get();
            if (go == null) return null;

            TCell cell = go.GetComponent<TCell>();
            if (cell != null)
            {
                onCellRecycledCallback?.Invoke(cell);
                bindCallback?.Invoke(cell, dataList[index], index);
            }

            return go;
        }

        private void OnReleaseCellItemInternal(int index, GameObject obj)
        {
            if (obj == null) return;

            TCell cell = obj.GetComponent<TCell>();
            if (cell != null)
            {
                onCellReleasedCallback?.Invoke(index, cell);
            }

            pool.Release(obj);
        }
    }

    /// <summary>
    /// TableView Extension Methods
    /// </summary>
    public static class TableViewExtensions
    {
        /// <summary>
        /// Fluent Builder 시작 (편의 메서드)
        /// </summary>
        public static TableViewController<TData, TCell> CreateController<TData, TCell>(this TableView tableView)
            where TCell : Component
        {
            return TableViewController<TData, TCell>.Create(tableView);
        }
    }
}