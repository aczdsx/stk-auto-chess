using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookApps.TeamBattle.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class TableView : ScrollRect
    {
        public struct CellGroup
        {
            public Rect rect;
            public List<Vector2> itemSizes;
            public RectTransform[] items;
            public int startIdx;
            public int endIdx;
            public bool isShown;
        }

        public enum ItemVerticalAlign
        {
            Top,
            Center,
            Bottom,
        }

        public enum ItemHorizontalAlign
        {
            Left,
            Center,
            Right,
        }

        #region Events
        /// <summary>
        /// RefreshAll 호출될 때 호출됩니다. 리스트 내부의 총 아이템 갯수를 리턴하세요.
        /// </summary>
        public event Func<int> OnGetTotalCellItemCount;

        /// <summary>
        /// RefreshAll 호출될 때 호출됩니다. 리스트 아이템들의 크기를 리턴하세요.
        /// </summary>
        public event Func<int, Vector2> OnGetCellItemSize;

        /// <summary>
        /// 인덱스에 맞는 리스트 아이템 오브젝트를 리턴하세요.
        /// </summary>
        public event Func<int, GameObject> OnGetCellItem;

        /// <summary>
        /// 사용이 끝난 아이템 오브젝트를 재활용하세요.
        /// </summary>
        public event Action<int, GameObject> OnReleaseCellItem;

        /// <summary>
        /// 스크롤 시에 호출됩니다. 인자값은 content rect의 position입니다.
        /// </summary>
        public event Action<Vector2> OnScrolling;
        #endregion

        [SerializeField] protected ItemVerticalAlign itemVerticalAlign;
        [SerializeField] protected ItemHorizontalAlign itemHorizontalAlign;
        [SerializeField] protected Vector2 spacing;
        [SerializeField] protected bool loadAll;

        #region Properties
        public ItemVerticalAlign VerticalAlign => itemVerticalAlign;
        public ItemHorizontalAlign HorizontalAlign => itemHorizontalAlign;
        public Vector2 Spacing => spacing;
        #endregion

        protected RectTransform rectTr;
        protected Vector4 viewportMargin; // x: left, y: right, z: top, w: bottom
        protected Vector2[] itemSizes;
        protected List<CellGroup> groups;

        private bool isInitialized;

        private ITableViewCalcImpl calcImpl;

        #region MonoBehaviour Override
        protected override void Awake()
        {
            base.Awake();

            CheckInitialize();
        }

        private void CheckInitialize()
        {
            if (isInitialized)
            {
                return;
            }

            if ((horizontal && vertical) || (!horizontal && !vertical))
            {
                Debug.LogError("Table view must have only one scroll direction!");
                return;
            }

            if (vertical && itemVerticalAlign == ItemVerticalAlign.Top)
            {
                if (!Mathf.Approximately(content.anchorMax.x, 0.5f) ||
                    !Mathf.Approximately(content.anchorMin.y, 1f) ||
                    !Mathf.Approximately(content.anchorMin.x, 0.5f) ||
                    !Mathf.Approximately(content.anchorMax.y, 1f) ||
                    !Mathf.Approximately(content.pivot.x, 0.5f) ||
                    !Mathf.Approximately(content.pivot.y, 1f))
                {
                    Debug.LogError("Vertical with VerticalAlign.Top must Content set center-top anchored!");
                    return;
                }
            }

            if (vertical && itemVerticalAlign == ItemVerticalAlign.Bottom)
            {
                if (!Mathf.Approximately(content.anchorMax.x, 0.5f) ||
                    !Mathf.Approximately(content.anchorMin.y, 0f) ||
                    !Mathf.Approximately(content.anchorMin.x, 0.5f) ||
                    !Mathf.Approximately(content.anchorMax.y, 0f) ||
                    !Mathf.Approximately(content.pivot.x, 0.5f) ||
                    !Mathf.Approximately(content.pivot.y, 0f))
                {
                    Debug.LogError("Vertical with VerticalAlign.Bottom must Content set center-bottom anchored!");
                    return;
                }
            }

            if (vertical && itemVerticalAlign == ItemVerticalAlign.Center)
            {
                Debug.LogError("Vertical with VerticalAlign.Center is not allowed!");
                return;
            }

            if (horizontal && itemHorizontalAlign == ItemHorizontalAlign.Left)
            {
                if (!Mathf.Approximately(content.anchorMin.x, 0f) ||
                    !Mathf.Approximately(content.anchorMin.y, 0.5f) ||
                    !Mathf.Approximately(content.anchorMax.x, 0f) ||
                    !Mathf.Approximately(content.anchorMax.y, 0.5f) ||
                    !Mathf.Approximately(content.pivot.x, 0f) ||
                    !Mathf.Approximately(content.pivot.y, 0.5f))
                {
                    Debug.LogError("Horizontal with HorizontalAlign.Left must Content set left-center anchored!");
                    return;
                }
            }

            if (horizontal && itemHorizontalAlign == ItemHorizontalAlign.Right)
            {
                if (!Mathf.Approximately(content.anchorMin.x, 1f) ||
                    !Mathf.Approximately(content.anchorMin.y, 0.5f) ||
                    !Mathf.Approximately(content.anchorMax.x, 1f) ||
                    !Mathf.Approximately(content.anchorMax.y, 0.5f) ||
                    !Mathf.Approximately(content.pivot.x, 1f) ||
                    !Mathf.Approximately(content.pivot.y, 0.5f))
                {
                    Debug.LogError("Horizontal with HorizontalAlign.Right must Content set right-center anchored!");
                    return;
                }
            }

            if (horizontal && itemHorizontalAlign == ItemHorizontalAlign.Center)
            {
                Debug.LogError("Horizontal with HorizontalAlign.Center is not allowed!");
                return;
            }

            calcImpl = TableViewCalcImplFactory.Create(vertical, horizontal, itemHorizontalAlign, itemVerticalAlign);

            rectTr = GetComponent<RectTransform>();
            viewportMargin = Vector4.zero;
            if (viewport != null)
            {
                (Vector2 rectSize, Vector2 viewportSize, Vector3 rectTrPos, Vector3 viewportPos, Vector3 rectTrLossyScale, Vector3 viewportLossyScale, Vector2 rectTrPivot, Vector2 viewportPivot) = (rectTr.rect.size * rectTr.localScale, viewport.rect.size * viewport.localScale, rectTr.position, viewport.position, rectTr.lossyScale, viewport.lossyScale, rectTr.pivot, viewport.pivot);
                Vector2 sizeDiff = rectSize - viewportSize;
                var posDiff = new Vector2(
                    (rectTrPos.x / rectTrLossyScale.x) - ((rectTrPivot.x - 0.5f) * rectSize.x) - ((viewportPos.x / viewportLossyScale.x) - ((viewportPivot.x - 0.5f) * viewportSize.x)),
                    (rectTrPos.y / rectTrLossyScale.y) - ((rectTrPivot.y - 0.5f) * rectSize.y) - ((viewportPos.y / viewportLossyScale.y) - ((viewportPivot.y - 0.5f) * viewportSize.y)));
                viewportMargin.x = (sizeDiff.x / 2f) - posDiff.x;
                viewportMargin.y = (sizeDiff.x / 2f) + posDiff.x;
                viewportMargin.z = (sizeDiff.y / 2f) + posDiff.y;
                viewportMargin.w = (sizeDiff.y / 2f) - posDiff.y;
            }

            groups = new List<CellGroup>();
            onValueChanged.AddListener(OnScroll);

            isInitialized = true;
        }

        private void OnScroll(Vector2 normalPos)
        {
            if (loadAll)
            {
                return;
            }

            OnScrolling?.Invoke(content.anchoredPosition);
            if (groups == null || groups.Count == 0)
            {
                return;
            }

            GetContentStartEndPosition(out Vector2 contentStartPos, out Vector2 contentEndPos, out int frontGroupIndex, out int backGroupIndex);
            // 릴리즈 먼저하고 채우기
            for (var i = 0; i < groups.Count; i++)
            {
                if (frontGroupIndex > i || i > backGroupIndex)
                {
                    if (groups[i].isShown)
                    {
                        ReleaseGroup(i);
                    }
                }
            }

            for (var i = 0; i < groups.Count; i++)
            {
                if (frontGroupIndex <= i && i <= backGroupIndex)
                {
                    if (!groups[i].isShown)
                    {
                        FillGroup(i);
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        public virtual void ClearAllCells()
        {
            CheckInitialize();
            if (OnReleaseCellItem == null)
            {
                return;
            }

            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i].items != null)
                {
                    for (var j = 0; j < groups[i].items.Length; j++)
                    {
                        OnReleaseCellItem.Invoke(groups[i].startIdx + j, groups[i].items[j].gameObject);
                    }
                }
            }

            groups.Clear();
        }

        public virtual void RefreshAll(bool resetPos = false, int focusIdx = 0, float focusPosRate = 0f, float focusDuration = 0f)
        {
            CheckInitialize();
            ClearAllCells();
            int totalCount = OnGetTotalCellItemCount?.Invoke() ?? 0;
            itemSizes = new Vector2[totalCount];
            for (var i = 0; i < totalCount; i++)
            {
                itemSizes[i] = OnGetCellItemSize?.Invoke(i) ?? Vector2.zero;
            }

            Vector2 contentSize = Vector2.zero;
            if (itemSizes.Length > 0)
            {
                Vector2 viewRectSize = viewport.rect.size;
                contentSize = calcImpl.CalcContentSizeAndFillGroups(viewRectSize, spacing, groups, itemSizes);
            }

            content.sizeDelta = contentSize;
            if (resetPos)
            {
                FocusItem(focusIdx, focusPosRate, focusDuration);
                return;
            }

            int frontGroupIndex;
            int backGroupIndex;
            if (loadAll)
            {
                frontGroupIndex = 0;
                backGroupIndex = groups.Count - 1;
            }
            else
            {
                GetContentStartEndPosition(out Vector2 contentStartPos, out Vector2 contentEndPos, out frontGroupIndex, out backGroupIndex);
            }

            for (var i = 0; i < groups.Count; i++)
            {
                if (frontGroupIndex <= i && i <= backGroupIndex)
                {
                    FillGroup(i);
                }
            }
        }

        public virtual void FocusItem(int index, float posRate = 0f, float duration = 1f)
        {
            CheckInitialize();
            posRate = Mathf.Min(1, Mathf.Max(0, posRate));
            if (groups.Count == 0)
            {
                return;
            }

            var groupIndex = 0;
            for (; groupIndex < groups.Count; groupIndex++)
            {
                if (index < groups[groupIndex].startIdx)
                {
                    break;
                }

                if (index <= groups[groupIndex].endIdx)
                {
                    break;
                }
            }

            groupIndex = Mathf.Clamp(groupIndex, 0, groups.Count - 1);

            CellGroup group = groups[groupIndex];
            Vector2 targetPos = calcImpl.GetFocusTargetPosition(viewRect.rect, content.rect.size, posRate, group);

            StopMovement();
            if (Mathf.Approximately(duration, 0f))
            {
                content.anchoredPosition = targetPos;
                OnScroll(Vector2.zero);
            }
            else
            {
                StartCoroutine(MoveContent(targetPos, duration));
            }
        }

        private IEnumerator MoveContent(Vector2 destPos, float duration)
        {
            Vector2 srcPos = content.anchoredPosition;
            float elapsedTime = 0;
            while (duration >= elapsedTime)
            {
                yield return null;
                elapsedTime += Time.deltaTime;
                Vector2 pos = Vector2.zero;
                float rate = elapsedTime / duration;
                pos.x = Mathf.SmoothStep(srcPos.x, destPos.x, rate);
                pos.y = Mathf.SmoothStep(srcPos.y, destPos.y, rate);
                content.anchoredPosition = pos;
                OnScroll(Vector2.zero);
            }
        }

        public Rect GetItemRect(int index)
        {
            if (groups.Count == 0)
            {
                return default;
            }

            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i].startIdx <= index && index <= groups[i].endIdx)
                {
                    int itemIdx = index - groups[i].startIdx;
                    return calcImpl.GetItemRect(groups[i], itemIdx, spacing);
                }
            }

            return default;
        }

        public Rect GetGroupRect(int index)
        {
            if (groups.Count == 0)
            {
                return default;
            }

            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i].startIdx <= index && index <= groups[i].endIdx)
                {
                    return groups[i].rect;
                }
            }

            return groups[^1].rect;
        }

        public Rect GetRangeGroupRect(int startIdx, int endIdx = -1)
        {
            if (groups.Count == 0)
            {
                return default;
            }

            int startGroupIdx = -1;
            int endGroupIdx = endIdx != -1 ? -1 : groups.Count - 1;
            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i].startIdx <= startIdx && startIdx <= groups[i].endIdx)
                {
                    startGroupIdx = i;
                }

                if (groups[i].startIdx <= endIdx && endIdx <= groups[i].endIdx)
                {
                    endGroupIdx = i;
                }

                if (startGroupIdx != -1 && endGroupIdx != -1)
                {
                    break;
                }
            }

            if (startGroupIdx == -1 || endGroupIdx == -1)
            {
                throw new ArgumentOutOfRangeException();
            }

            Rect ret = calcImpl.GetRangeGroupRect(groups, startGroupIdx, endGroupIdx);
            return ret;
        }

        public RectTransform GetListItem(int index)
        {
            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i].startIdx <= index && index <= groups[i].endIdx)
                {
                    if (groups[i].items == null)
                    {
                        return null;
                    }

                    return groups[i].items[index - groups[i].startIdx];
                }
            }

            return null;
        }

        public List<(int index, RectTransform item)> GetAllDisplayedItems()
        {
            var res = new List<(int, RectTransform)>();
            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i].items == null)
                {
                    continue;
                }

                for (var j = 0; j < groups[i].items.Length; j++)
                {
                    res.Add((groups[i].startIdx + j, groups[i].items[j]));
                }
            }

            return res;
        }

        /// <summary>
        /// 하나의 리스트 아이템 사이즈만 변경하고 싶을 경우 호출
        /// </summary>
        /// <param name="itemIdx"></param>
        public void UpdateItemSize(int itemIdx)
        {
            var isShift = false;
            Vector2 shiftPos = Vector2.zero;
            for (var i = 0; i < groups.Count; i++)
            {
                if (isShift)
                {
                    CellGroup group = groups[i];
                    calcImpl.ShiftPosition(ref group, shiftPos);
                    groups[i] = group;
                }

                if (groups[i].startIdx <= itemIdx && itemIdx <= groups[i].endIdx)
                {
                    Vector2 itemSize = OnGetCellItemSize?.Invoke(itemIdx) ?? Vector2.zero;
                    CellGroup group = groups[i];
                    (isShift, shiftPos) = calcImpl.ReCalcGroupSize(ref group, itemIdx, itemSize);
                    groups[i] = group;
                    if (groups[i].isShown)
                    {
                        FillGroup(i);
                    }

                    if (!isShift)
                    {
                        return;
                    }
                }
            }

            if (isShift)
            {
                content.sizeDelta += shiftPos;
            }
        }
        #endregion

        #region Protected Methods
        protected virtual void FillGroup(int groupIdx)
        {
            CellGroup group = groups[groupIdx];
            int cellItemCount = group.endIdx - group.startIdx + 1;
            if (group.items != null)
            {
                for (var i = 0; i < group.items.Length; i++)
                {
                    OnReleaseCellItem?.Invoke(group.startIdx + i, group.items[i].gameObject);
                }
            }

            group.items = new RectTransform[cellItemCount];
            var cellItemRects = new Rect[cellItemCount];
            for (int i = group.startIdx; i <= group.endIdx; i++)
            {
                GameObject go = OnGetCellItem?.Invoke(i);
                var cellItem = go.GetComponent<RectTransform>();
                int idx = i - group.startIdx;
                group.items[idx] = cellItem;
                cellItemRects[idx] = cellItem.rect;
                (cellItem.anchorMin, cellItem.anchorMax, cellItem.pivot) = calcImpl.GetItemAnchorAndPivot();
            }

            calcImpl.ArrangeItems(ref group, spacing);

            group.isShown = true;
            groups[groupIdx] = group;
        }

        protected virtual void ReleaseGroup(int groupIdx)
        {
            CellGroup group = groups[groupIdx];
            if (group.items != null)
            {
                for (var i = 0; i < group.items.Length; i++)
                {
                    OnReleaseCellItem?.Invoke(group.startIdx + i, group.items[i].gameObject);
                }

                group.items = null;
                group.isShown = false;
            }

            groups[groupIdx] = group;
        }

        private void GetContentStartEndPosition(out Vector2 startPos, out Vector2 endPos, out int frontGroupIndex, out int backGroupIndex)
        {
            Vector2 viewRectSize = rectTr.rect.size;
            startPos = content.anchoredPosition;
            endPos = startPos;
            calcImpl.CalcContentStartEndPosition(ref startPos, ref endPos, viewportMargin, viewRectSize);

            frontGroupIndex = -1;
            backGroupIndex = -1;

            for (var i = 0; i < groups.Count; i++)
            {
                CellGroup group = groups[i];

                // find front
                if (frontGroupIndex == -1)
                {
                    if (IsContainGroup(group, startPos, endPos))
                    {
                        frontGroupIndex = i;
                    }
                }
                else
                {
                    if (!IsContainGroup(group, startPos, endPos))
                    {
                        break;
                    }
                }

                backGroupIndex = i;
            }
        }

        private bool IsContainGroup(CellGroup group, Vector2 startPos, Vector2 endPos)
        {
            return calcImpl.IsContainGroup(group, startPos, endPos, spacing);
        }
        #endregion

        #region Drag Interface
        private bool routeToParent;

        private void DoForParents<T>(Action<T> action) where T : IEventSystemHandler
        {
            Transform parent = transform.parent;
            while (parent != null)
            {
                foreach (Component component in parent.GetComponents<Component>())
                {
                    if (component is T)
                    {
                        action((T) (IEventSystemHandler) component);
                    }
                }

                parent = parent.parent;
            }
        }

        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            DoForParents<IInitializePotentialDragHandler>((parent) => { parent.OnInitializePotentialDrag(eventData); });
            base.OnInitializePotentialDrag(eventData);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (!horizontal && Math.Abs(eventData.delta.x) > Math.Abs(eventData.delta.y))
            {
                routeToParent = true;
            }
            else if (!vertical && Math.Abs(eventData.delta.x) < Math.Abs(eventData.delta.y))
            {
                routeToParent = true;
            }
            else
            {
                routeToParent = false;
            }

            if (routeToParent)
            {
                DoForParents<IBeginDragHandler>((parent) => { parent.OnBeginDrag(eventData); });
            }
            else
            {
                base.OnBeginDrag(eventData);
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (routeToParent)
            {
                DoForParents<IDragHandler>((parent) => { parent.OnDrag(eventData); });
            }
            else
            {
                base.OnDrag(eventData);
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (routeToParent)
            {
                DoForParents<IEndDragHandler>((parent) => { parent.OnEndDrag(eventData); });
            }
            else
            {
                base.OnEndDrag(eventData);
            }

            routeToParent = false;
        }
        #endregion
    }
}
