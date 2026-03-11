using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle.Utility
{
    public enum SafeAreaEdge
    {
        Top,
        Bottom,
        Left,
        Right
    }

    public enum SafeAreaInsetMode
    {
        /// <summary>safe area 안쪽으로 밀기 (노치/홈 인디케이터 회피)</summary>
        Inward,
        /// <summary>safe area 바깥으로 밀기 (노치/홈 인디케이터 영역까지 채우기)</summary>
        Outward
    }

    /// <summary>
    /// Safe area inset 값만큼 UI 요소의 위치/크기를 조정하는 통합 컴포넌트.
    /// 기존 SafeAreaTopMargin, SafeAreaTopReverseMargin 등 8개 클래스를 대체합니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaInset : CachedMonoBehaviour
    {
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private RectTransform canvasScalerRectTr;
        [SerializeField] private SafeAreaEdge edge;
        [SerializeField] private SafeAreaInsetMode mode;
        [Tooltip("false = position만 조정, true = size도 함께 조정 (배경 패널 등)")]
        [SerializeField] private bool extend;

        private Vector2 originSizeDelta;
        private Vector2 originAnchoredPosition;
        private bool hasOrigin;

        private Rect processedSafeArea;
        private Vector2 processedResolution;
        private ScreenOrientation processedOrientation;
        private bool hasProcessed;
        private float storedMargin;

        private void Start()
        {
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            CacheOriginIfNeeded();

            var safeArea = Screen.safeArea;
            var resolution = Screen.fullScreen ? new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) : new Vector2(Screen.width, Screen.height);

            var orientation = Screen.orientation;

            if (hasProcessed && processedSafeArea == safeArea && processedResolution == resolution && processedOrientation == orientation)
            {
                return;
            }

            processedSafeArea = safeArea;
            processedResolution = resolution;
            processedOrientation = orientation;
            hasProcessed = true;

            var rawMargin = ComputeRawMargin(safeArea, resolution);
            var resolutionRatio = CalculateResolutionRatio(resolution);
            var marginRatio = GetMarginRatio();
            storedMargin = rawMargin * resolutionRatio * marginRatio;

            ApplyMargin(storedMargin);
        }

        private float ComputeRawMargin(Rect safeArea, Vector2 resolution)
        {
            return edge switch
            {
                SafeAreaEdge.Top => resolution.y - (safeArea.y + safeArea.height),
                SafeAreaEdge.Bottom => safeArea.y,
                SafeAreaEdge.Left => safeArea.x,
                SafeAreaEdge.Right => resolution.x - (safeArea.x + safeArea.width),
                _ => 0f
            };
        }

        private float GetMarginRatio()
        {
            var ratio = SafeArea.MarginRatio;
            return edge switch
            {
                SafeAreaEdge.Top => ratio.top,
                SafeAreaEdge.Bottom => ratio.bottom,
                SafeAreaEdge.Left => ratio.left,
                SafeAreaEdge.Right => ratio.right,
                _ => 1f
            };
        }

        private void ApplyMargin(float margin)
        {
            bool isVertical = edge is SafeAreaEdge.Top or SafeAreaEdge.Bottom;
            bool isPositiveDirection = (edge, mode) switch
            {
                // Inward: safe area 안쪽으로 → 노치 반대 방향
                (SafeAreaEdge.Top, SafeAreaInsetMode.Inward) => false,     // 아래로
                (SafeAreaEdge.Bottom, SafeAreaInsetMode.Inward) => true,   // 위로
                (SafeAreaEdge.Left, SafeAreaInsetMode.Inward) => true,     // 오른쪽으로
                (SafeAreaEdge.Right, SafeAreaInsetMode.Inward) => false,   // 왼쪽으로
                // Outward: safe area 바깥으로 → 노치 방향
                (SafeAreaEdge.Top, SafeAreaInsetMode.Outward) => true,     // 위로
                (SafeAreaEdge.Bottom, SafeAreaInsetMode.Outward) => false, // 아래로
                (SafeAreaEdge.Left, SafeAreaInsetMode.Outward) => false,   // 왼쪽으로
                (SafeAreaEdge.Right, SafeAreaInsetMode.Outward) => true,   // 오른쪽으로
                _ => true
            };

            float sign = isPositiveDirection ? 1f : -1f;

            if (isVertical)
            {
                if (extend)
                {
                    CachedRectTr.sizeDelta = originSizeDelta + new Vector2(0f, margin);
                    float pivotOffset = isPositiveDirection ? CachedRectTr.pivot.y : (1f - CachedRectTr.pivot.y);
                    CachedRectTr.anchoredPosition = originAnchoredPosition + new Vector2(0f, sign * margin * pivotOffset);
                }
                else
                {
                    CachedRectTr.anchoredPosition = originAnchoredPosition + new Vector2(0f, sign * margin);
                }
            }
            else
            {
                if (extend)
                {
                    CachedRectTr.sizeDelta = originSizeDelta + new Vector2(margin, 0f);
                    float pivotOffset = isPositiveDirection ? CachedRectTr.pivot.x : (1f - CachedRectTr.pivot.x);
                    CachedRectTr.anchoredPosition = originAnchoredPosition + new Vector2(sign * margin * pivotOffset, 0f);
                }
                else
                {
                    CachedRectTr.anchoredPosition = originAnchoredPosition + new Vector2(sign * margin, 0f);
                }
            }
        }

        private float CalculateResolutionRatio(Vector2 resolution)
        {
            if (Mathf.Approximately(canvasScalerRectTr.rect.size.x, canvasScaler.referenceResolution.x))
            {
                return canvasScaler.referenceResolution.x / resolution.x;
            }

            return canvasScaler.referenceResolution.y / resolution.y;
        }

        private void CacheOriginIfNeeded()
        {
            if (hasOrigin)
                return;

            originSizeDelta = CachedRectTr.sizeDelta;
            originAnchoredPosition = CachedRectTr.anchoredPosition;
            hasOrigin = true;
        }
    }
}
