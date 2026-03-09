using System;
using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    [Obsolete("SafeAreaInset(edge=Right, mode=Outward)으로 교체하세요.")]
    public class SafeAreaRightReverseMargin : SafeAreaMarginBase
    {
        private static float? marginRight;
        private static Rect processedSafeArea;
        private static Vector2 processedResolution;
        private static ScreenOrientation processedOrientation;
        private static bool hasProcessed;

        public float MarginRight => marginRight ?? 0;

        protected override float? StoredMargin { get => marginRight; set => marginRight = value; }
        protected override Rect ProcessedSafeArea { get => processedSafeArea; set => processedSafeArea = value; }
        protected override Vector2 ProcessedResolution { get => processedResolution; set => processedResolution = value; }
        protected override ScreenOrientation ProcessedOrientation { get => processedOrientation; set => processedOrientation = value; }
        protected override bool HasProcessed { get => hasProcessed; set => hasProcessed = value; }

        protected override float ComputeRawMargin(Rect safeArea, Vector2 resolution) => resolution.x - (safeArea.x + safeArea.width);
        protected override float MarginRatio => SafeArea.MarginRatio.right;

        protected override void ApplyMargin(float margin)
        {
            if (Extend)
            {
                RectTr.sizeDelta = OriginSizeDelta + new Vector2(margin, 0f);
                RectTr.anchoredPosition = OriginAnchoredPosition + new Vector2(margin * RectTr.pivot.x, 0f);
            }
            else
            {
                RectTr.anchoredPosition = OriginAnchoredPosition + new Vector2(margin, 0f);
            }
        }
    }
}
