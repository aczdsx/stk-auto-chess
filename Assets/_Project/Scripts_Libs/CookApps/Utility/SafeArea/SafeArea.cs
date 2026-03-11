using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : CachedMonoBehaviour
    {
        [SerializeField] private bool isControlTop = true;
        [SerializeField] private bool isControlBottom = true;
        [SerializeField] private bool isControlLeft = true;
        [SerializeField] private bool isControlRight = true;

        private Rect processedSafeArea;
        private Vector2 processedResolution;
        private ScreenOrientation processedOrientation;

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
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            var safeArea = Screen.safeArea;
            var resolution = Screen.fullScreen ? new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) : new Vector2(Screen.width, Screen.height);

            var orientation = Screen.orientation;

            if (processedSafeArea == safeArea && processedResolution == resolution && processedOrientation == orientation)
                return;
            processedSafeArea = safeArea;
            processedResolution = resolution;
            processedOrientation = orientation;

            var leftAnchorDiff = safeArea.x / resolution.x * MarginRatio.left;
            var bottomAnchorDiff = safeArea.y / resolution.y * MarginRatio.bottom;

            CachedRectTr.anchorMin = new Vector2(
                isControlLeft ? leftAnchorDiff: CachedRectTr.anchorMin.x,
                isControlBottom ? bottomAnchorDiff : CachedRectTr.anchorMin.y
            );

            var rightAnchorDiff = (1f - ((safeArea.x + safeArea.width) / resolution.x)) * MarginRatio.right;
            var topAnchorDiff = (1f - ((safeArea.y + safeArea.height) / resolution.y)) * MarginRatio.top;

            CachedRectTr.anchorMax = new Vector2(
                isControlRight ? 1f - rightAnchorDiff : CachedRectTr.anchorMax.x,
                isControlTop ? 1f - topAnchorDiff : CachedRectTr.anchorMax.y
            );
        }
    }
}
