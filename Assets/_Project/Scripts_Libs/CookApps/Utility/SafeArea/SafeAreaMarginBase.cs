using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class SafeAreaMarginBase : MonoBehaviour
    {
        private RectTransform rectTr;
        private CanvasScaler canvasScaler;
        private RectTransform canvasScalerRectTr;
        private Vector2 originSizeDelta;
        private Vector2 originAnchoredPosition;
        private bool hasOrigin;

        protected RectTransform RectTr => rectTr;
        protected Vector2 OriginSizeDelta => originSizeDelta;
        protected Vector2 OriginAnchoredPosition => originAnchoredPosition;

        protected abstract float? StoredMargin { get; set; }
        protected abstract Rect ProcessedSafeArea { get; set; }
        protected abstract Vector2 ProcessedResolution { get; set; }
        protected abstract bool HasProcessed { get; set; }

        protected abstract float ComputeRawMargin(Rect safeArea, Vector2 resolution);
        protected abstract float MarginRatio { get; }
        protected abstract void ApplyMargin(float margin);

        protected virtual void Start()
        {
            Refresh();
        }

        public void Refresh(bool forceRecalculate = false)
        {
            CacheOriginIfNeeded();

            var safeArea = Screen.safeArea;
            var resolution = Screen.fullScreen ? new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) : new Vector2(Screen.width, Screen.height);
            if (forceRecalculate || !HasProcessed || ProcessedSafeArea != safeArea || ProcessedResolution != resolution)
            {
                ProcessedSafeArea = safeArea;
                ProcessedResolution = resolution;
                HasProcessed = true;

                var rawMargin = ComputeRawMargin(safeArea, resolution);
                var resolutionRatio = CalculateResolutionRatio(resolution);
                StoredMargin = rawMargin * resolutionRatio * MarginRatio;
            }

            ApplyMargin(StoredMargin ?? 0f);
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

            rectTr = GetComponent<RectTransform>();
            canvasScaler = GetComponentInParent<CanvasScaler>();
            canvasScalerRectTr = canvasScaler.GetComponent<RectTransform>();
            originSizeDelta = rectTr.sizeDelta;
            originAnchoredPosition = rectTr.anchoredPosition;
            hasOrigin = true;
        }
    }
}
