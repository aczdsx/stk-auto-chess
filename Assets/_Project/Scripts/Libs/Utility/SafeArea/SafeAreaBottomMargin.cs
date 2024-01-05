using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaBottomMargin : MonoBehaviour
    {
        [SerializeField] private bool extend;

        public float MarginBottom { get; private set; }
        public bool IsExtend => extend;

        private void Awake()
        {
            var rectTr = GetComponent<RectTransform>();
            var originSizeDelta = rectTr.sizeDelta;
            var originAnchoredPosition = rectTr.anchoredPosition;
            var canvasScaler = GetComponentInParent<CanvasScaler>();
            var canvasScalerRectTr = canvasScaler.GetComponent<RectTransform>();

            // var isTopAnchored = Mathf.Approximately(cachedRectTr.anchorMin.y, cachedRectTr.anchorMax.y) &&
            //                     Mathf.Approximately(cachedRectTr.anchorMin.y, 0);
            // Debug.Assert(isTopAnchored, $"{name} is not bottom anchored RectTransform");
            var safeArea = Screen.safeArea;
            var resolution = Screen.fullScreen ? new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) : new Vector2(Screen.width, Screen.height);

            var marginBottom = safeArea.y;
            float resolutionRatio;
            if (Mathf.Approximately(canvasScalerRectTr.rect.size.x, canvasScaler.referenceResolution.x))
            {
                resolutionRatio = canvasScaler.referenceResolution.x / resolution.x;
            }
            else
            {
                resolutionRatio = canvasScaler.referenceResolution.y / resolution.y;
            }

            MarginBottom = marginBottom * resolutionRatio * SafeArea.MarginRatio.bottom;
            // WARNING! scale이 변경되었을 경우(by self or parent) 로직 수정 필요
            if (extend)
            {
                rectTr.sizeDelta = originSizeDelta + new Vector2(0f, MarginBottom);
                rectTr.anchoredPosition = originAnchoredPosition + new Vector2(0f, MarginBottom * rectTr.pivot.y);
            }
            else
            {
                rectTr.anchoredPosition = originAnchoredPosition + new Vector2(0f, MarginBottom);
            }
        }
    }
}
