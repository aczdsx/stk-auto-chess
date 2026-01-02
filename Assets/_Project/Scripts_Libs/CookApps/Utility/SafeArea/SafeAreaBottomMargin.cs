using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    public class SafeAreaBottomMargin : SafeAreaMarginBase
    {
        private static float? marginBottom;
        private static Rect processedSafeArea;
        private static Vector2 processedResolution;
        private static bool hasProcessed;

        public float MarginBottom => marginBottom ?? 0;

        protected override float? StoredMargin { get => marginBottom; set => marginBottom = value; }
        protected override Rect ProcessedSafeArea { get => processedSafeArea; set => processedSafeArea = value; }
        protected override Vector2 ProcessedResolution { get => processedResolution; set => processedResolution = value; }
        protected override bool HasProcessed { get => hasProcessed; set => hasProcessed = value; }

        protected override float ComputeRawMargin(Rect safeArea, Vector2 resolution) => safeArea.y;
        protected override float MarginRatio => SafeArea.MarginRatio.bottom;

        protected override void ApplyMargin(float margin)
        {
            // WARNING! scale이 변경되었을 경우(by self or parent) 로직 수정 필요
            if (Extend)
            {
                RectTr.sizeDelta = OriginSizeDelta + new Vector2(0f, margin);
                RectTr.anchoredPosition = OriginAnchoredPosition + new Vector2(0f, margin * RectTr.pivot.y);
            }
            else
            {
                RectTr.anchoredPosition = OriginAnchoredPosition + new Vector2(0f, margin);
            }
        }
    }
}
