using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        [SerializeField] private bool isControlTop = true;
        [SerializeField] private bool isControlBottom = true;
        [SerializeField] private bool isControlLeft = true;
        [SerializeField] private bool isControlRight = true;

        private Rect processedSafeArea;
        private Vector2 processedResolution;
        private RectTransform rectTr;

        public static (float left, float right, float top, float bottom) MarginRatio
        {
            get
            {
                var settings = SafeAreaSettings.Active;
                if (settings != null)
                {
                    return (settings.left, settings.right, settings.top, settings.bottom);
                }

                return (1f, 1f, 1f, 1f);
            }
        }

        private void Start()
        {
            rectTr = GetComponent<RectTransform>();
            Refresh();
        }

        public void Refresh(bool forceRecalculate = false)
        {
            if (rectTr == null)
                rectTr = GetComponent<RectTransform>();

            var safeArea = Screen.safeArea;
            var resolution = Screen.fullScreen ? new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) : new Vector2(Screen.width, Screen.height);

            if (!forceRecalculate && processedSafeArea == safeArea && processedResolution == resolution)
                return;
            processedSafeArea = safeArea;
            processedResolution = resolution;

            var leftAnchorDiff = safeArea.x / resolution.x * MarginRatio.left;
            var bottomAnchorDiff = safeArea.y / resolution.y * MarginRatio.bottom;

            rectTr.anchorMin = new Vector2(
                isControlLeft ? leftAnchorDiff: rectTr.anchorMin.x,
                isControlBottom ? bottomAnchorDiff : rectTr.anchorMin.y
            );

            var rightAnchorDiff = (1f - ((safeArea.x + safeArea.width) / resolution.x)) * MarginRatio.right;
            var topAnchorDiff = (1f - ((safeArea.y + safeArea.height) / resolution.y)) * MarginRatio.top;

            rectTr.anchorMax = new Vector2(
                isControlRight ? 1f - rightAnchorDiff : rectTr.anchorMax.x,
                isControlTop ? 1f - topAnchorDiff : rectTr.anchorMax.y
            );
        }
    }
}
