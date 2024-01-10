using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaTopReverseMargin : MonoBehaviour
    {
        public float MarginTop { get; private set; }

        private void Awake()
        {
            var rectTr = GetComponent<RectTransform>();
            var originSizeDelta = rectTr.sizeDelta;
            var originAnchoredPosition = rectTr.anchoredPosition;
            var canvasScaler = GetComponentInParent<CanvasScaler>();
            var canvasScalerRectTr = canvasScaler.GetComponent<RectTransform>();

            // var isTopAnchored = Mathf.Approximately(cachedRectTr.anchorMin.y, cachedRectTr.anchorMax.y) &&
            //                     Mathf.Approximately(cachedRectTr.anchorMin.y, 1);
            // Debug.Assert(isTopAnchored, $"{name} is not top anchored RectTransform");
            var safeArea = Screen.safeArea;
            var resolution = Screen.fullScreen ? new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) : new Vector2(Screen.width, Screen.height);

            var marginTop = resolution.y - (safeArea.y + safeArea.height);
            float resolutionRatio;
            if (Mathf.Approximately(canvasScalerRectTr.rect.size.x, canvasScaler.referenceResolution.x))
            {
                resolutionRatio = canvasScaler.referenceResolution.x / resolution.x;
            }
            else
            {
                resolutionRatio = canvasScaler.referenceResolution.y / resolution.y;
            }

            MarginTop = marginTop * resolutionRatio * SafeArea.MarginRatio.top;
            rectTr.sizeDelta = originSizeDelta + new Vector2(0f, MarginTop);
            rectTr.anchoredPosition = originAnchoredPosition + new Vector2(0f, MarginTop * (1f - rectTr.pivot.y));
        }
    }
}
