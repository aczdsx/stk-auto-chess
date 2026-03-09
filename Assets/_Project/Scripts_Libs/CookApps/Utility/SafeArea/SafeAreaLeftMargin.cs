using System;
using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    [Obsolete("SafeAreaInset(edge=Left, mode=Inward)으로 교체하세요.")]
    public class SafeAreaLeftMargin : SafeAreaMarginBase
    {
        private static float? marginLeft;
        private static Rect processedSafeArea;
        private static Vector2 processedResolution;
        private static ScreenOrientation processedOrientation;
        private static bool hasProcessed;

        public float MarginLeft => marginLeft ?? 0;

        protected override float? StoredMargin { get => marginLeft; set => marginLeft = value; }
        protected override Rect ProcessedSafeArea { get => processedSafeArea; set => processedSafeArea = value; }
        protected override Vector2 ProcessedResolution { get => processedResolution; set => processedResolution = value; }
        protected override ScreenOrientation ProcessedOrientation { get => processedOrientation; set => processedOrientation = value; }
        protected override bool HasProcessed { get => hasProcessed; set => hasProcessed = value; }

        protected override float ComputeRawMargin(Rect safeArea, Vector2 resolution) => safeArea.x;
        protected override float MarginRatio => SafeArea.MarginRatio.left;

        protected override void ApplyMargin(float margin)
        {
            // WARNING! scale이 변경되었을 경우(by self or parent) 로직 수정 필요
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
