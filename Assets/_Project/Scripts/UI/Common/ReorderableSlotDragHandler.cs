using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 슬롯 드래그 앤 드롭 순서 변경 컴포넌트.
    /// ScrollRect 내부의 VerticalLayoutGroup 자식에 부착하여 사용한다.
    /// 롱탭 후 드래그하면 Placeholder로 놓일 자리를 프리뷰하며, Insert(삽입+밀림) 방식으로 순서를 변경한다.
    /// 짧은 드래그는 ScrollRect 스크롤로 동작한다.
    /// </summary>
    public class ReorderableSlotDragHandler : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float _longPressThreshold = 0.4f;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _autoScrollEdgeRatio = 0.15f;
        [SerializeField] private float _autoScrollMaxSpeed = 600f;

        private ScrollRect _scrollRect;
        private RectTransform _rectTransform;
        private Canvas _canvas;
        private LayoutElement _layoutElement;

        private bool _isLongPressed;
        private bool _isReordering;
        private bool _scrollRectDragStarted;
        private float _pointerDownTime;
        private int _originalSiblingIndex;
        private Vector2 _dragOffset;

        private bool _isDraggable = true;

        private GameObject _placeholder;

        /// <summary>(fromIndex, toIndex) 순서 변경 시 호출</summary>
        public event Action<int, int> OnReordered;

        /// <summary>드래그 가능 여부 설정 (빈 슬롯은 false)</summary>
        public void SetDraggable(bool draggable)
        {
            _isDraggable = draggable;
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            // LayoutElement 확보 (ignoreLayout 토글용)
            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
                _layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isLongPressed = false;
            _pointerDownTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isLongPressed = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isReordering = false;
            _scrollRectDragStarted = false;

            _scrollRect = GetComponentInParent<ScrollRect>();
            _canvas = GetComponentInParent<Canvas>();

            // 일단 ScrollRect 드래그로 시작
            if (_scrollRect != null)
            {
                ExecuteEvents.Execute(_scrollRect.gameObject, eventData, ExecuteEvents.beginDragHandler);
                _scrollRectDragStarted = true;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isReordering)
            {
                HandleReorderDrag(eventData);
                return;
            }

            // 롱탭 체크
            float elapsed = Time.unscaledTime - _pointerDownTime;
            if (!_isLongPressed && elapsed >= _longPressThreshold && _isDraggable)
            {
                _isLongPressed = true;
                EnterReorderMode(eventData);
                return;
            }

            // 아직 롱탭 아님 → ScrollRect 스크롤 계속
            if (_scrollRect != null && _scrollRectDragStarted)
            {
                ExecuteEvents.Execute(_scrollRect.gameObject, eventData, ExecuteEvents.dragHandler);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isReordering)
            {
                ExitReorderMode();
            }
            else if (_scrollRectDragStarted && _scrollRect != null)
            {
                ExecuteEvents.Execute(_scrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);
            }

            _scrollRectDragStarted = false;
            _isReordering = false;
            _isLongPressed = false;
            _scrollRect = null;
        }

        private void EnterReorderMode(PointerEventData eventData)
        {
            _isReordering = true;
            _originalSiblingIndex = transform.GetSiblingIndex();

            // ScrollRect 드래그 중단
            if (_scrollRect != null && _scrollRectDragStarted)
            {
                ExecuteEvents.Execute(_scrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);
                _scrollRect.StopMovement();
                _scrollRectDragStarted = false;
            }

            // 드래그 오프셋 계산
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform, eventData.position, eventData.pressEventCamera, out _dragOffset);

            // Placeholder 생성: 원래 위치에 같은 높이의 빈 오브젝트 삽입
            CreatePlaceholder();

            // 레이아웃에서 제외 → 렌더링 최상단
            _layoutElement.ignoreLayout = true;
            transform.SetAsLastSibling();

            // 시각적 피드백
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0.7f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            transform.localScale = new Vector3(1.03f, 1.03f, 1f);
        }

        private void CreatePlaceholder()
        {
            _placeholder = new GameObject("ReorderPlaceholder");
            _placeholder.transform.SetParent(transform.parent, false);
            _placeholder.transform.SetSiblingIndex(_originalSiblingIndex);

            var rt = _placeholder.AddComponent<RectTransform>();
            rt.sizeDelta = _rectTransform.sizeDelta;

            var le = _placeholder.AddComponent<LayoutElement>();
            le.preferredHeight = LayoutUtility.GetPreferredHeight(_rectTransform);
            le.flexibleWidth = 1f;
        }

        private void ExitReorderMode()
        {
            int dropIndex = _placeholder != null ? _placeholder.transform.GetSiblingIndex() : _originalSiblingIndex;

            // Placeholder 제거
            if (_placeholder != null)
            {
                DestroyImmediate(_placeholder);
                _placeholder = null;
            }

            // 레이아웃 복귀 + 최종 위치 설정
            _layoutElement.ignoreLayout = false;
            transform.SetSiblingIndex(dropIndex);

            // 시각적 복원
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }

            transform.localScale = Vector3.one;

            // 순서가 변경되었으면 콜백
            if (dropIndex != _originalSiblingIndex)
            {
                OnReordered?.Invoke(_originalSiblingIndex, dropIndex);
            }
        }

        private void HandleReorderDrag(PointerEventData eventData)
        {
            // Y축 이동 (손가락 추적)
            if (_canvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)transform.parent, eventData.position, eventData.pressEventCamera, out var localPoint);

                var pos = _rectTransform.localPosition;
                pos.y = localPoint.y - _dragOffset.y;
                _rectTransform.localPosition = pos;
            }

            AutoScrollIfNeeded(eventData);
            UpdatePlaceholderPosition();
        }

        private void AutoScrollIfNeeded(PointerEventData eventData)
        {
            if (_scrollRect == null) return;

            var scrollRectTransform = (RectTransform)_scrollRect.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                scrollRectTransform, eventData.position, eventData.pressEventCamera, out var localPoint);

            var rect = scrollRectTransform.rect;
            float edgeSize = rect.height * _autoScrollEdgeRatio;

            // localPoint.y 는 rect 중심 기준 → rect 하단/상단 거리 계산
            float distFromTop = rect.yMax - localPoint.y;
            float distFromBottom = localPoint.y - rect.yMin;

            float scrollDelta = 0f;
            if (distFromTop < edgeSize)
            {
                // 위쪽 가장자리 → 위로 스크롤 (normalizedPosition 증가)
                scrollDelta = (1f - distFromTop / edgeSize) * _autoScrollMaxSpeed;
            }
            else if (distFromBottom < edgeSize)
            {
                // 아래쪽 가장자리 → 아래로 스크롤 (normalizedPosition 감소)
                scrollDelta = -(1f - distFromBottom / edgeSize) * _autoScrollMaxSpeed;
            }

            if (Mathf.Approximately(scrollDelta, 0f)) return;

            var content = _scrollRect.content;
            var pos = content.anchoredPosition;
            pos.y -= scrollDelta * Time.unscaledDeltaTime;

            // 스크롤 범위 클램프
            float contentHeight = content.rect.height;
            float viewportHeight = scrollRectTransform.rect.height;
            float maxY = Mathf.Max(0f, contentHeight - viewportHeight);
            pos.y = Mathf.Clamp(pos.y, 0f, maxY);

            content.anchoredPosition = pos;
        }

        private void UpdatePlaceholderPosition()
        {
            if (_placeholder == null) return;

            var parent = transform.parent;
            if (parent == null) return;

            float dragCenterY = _rectTransform.localPosition.y;
            int childCount = parent.childCount;

            // 기본값: 드래그 슬롯 바로 앞 (레이아웃 기준 맨 끝)
            int targetIndex = transform.GetSiblingIndex();
            bool found = false;

            for (int i = 0; i < childCount; i++)
            {
                var child = parent.GetChild(i);

                // 드래그 중인 슬롯 자체, placeholder, 비활성 슬롯 제외
                if (child == transform) continue;
                if (child.gameObject == _placeholder) continue;
                if (!child.gameObject.activeSelf) continue;

                var childRect = (RectTransform)child;
                float childMidY = childRect.localPosition.y - childRect.rect.height * 0.5f;

                // VerticalLayoutGroup: 위쪽이 Y값 큼
                // 드래그 슬롯 중심이 이 슬롯의 중간 지점보다 위에 있으면 → 이 슬롯 앞에 배치
                if (dragCenterY > childMidY)
                {
                    targetIndex = child.GetSiblingIndex();
                    found = true;
                    break;
                }
            }

            // PH가 타겟보다 앞에 있으면 PH 제거 시 인덱스가 1 줄어드므로 보정
            int phIndex = _placeholder.transform.GetSiblingIndex();
            if (phIndex < targetIndex)
                targetIndex--;

            if (phIndex != targetIndex)
            {
                _placeholder.transform.SetSiblingIndex(targetIndex);
            }
        }
    }
}
