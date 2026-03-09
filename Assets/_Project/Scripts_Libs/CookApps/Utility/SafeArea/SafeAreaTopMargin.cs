using System;
using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    [Obsolete("SafeAreaInset(edge=Top, mode=Inward)으로 교체하세요.")]
    public class SafeAreaTopMargin : SafeAreaMarginBase
    {
        private static float? marginTop;
        private static Rect processedSafeArea;
        private static Vector2 processedResolution;
        private static ScreenOrientation processedOrientation;
        private static bool hasProcessed;

        public static float MarginTop => marginTop ?? 0;

        protected override float? StoredMargin { get => marginTop; set => marginTop = value; }
        protected override Rect ProcessedSafeArea { get => processedSafeArea; set => processedSafeArea = value; }
        protected override Vector2 ProcessedResolution { get => processedResolution; set => processedResolution = value; }
        protected override ScreenOrientation ProcessedOrientation { get => processedOrientation; set => processedOrientation = value; }
        protected override bool HasProcessed { get => hasProcessed; set => hasProcessed = value; }

        protected override float ComputeRawMargin(Rect safeArea, Vector2 resolution) => resolution.y - (safeArea.y + safeArea.height);
        protected override float MarginRatio => SafeArea.MarginRatio.top;

        protected override void ApplyMargin(float margin)
        {
            // WARNING! scale이 변경되었을 경우(by self or parent) 로직 수정 필요
            if (Extend)
            {
                RectTr.sizeDelta = OriginSizeDelta + new Vector2(0f, margin);
                RectTr.anchoredPosition = OriginAnchoredPosition - new Vector2(0f, margin * (1f - RectTr.pivot.y));
            }
            else
            {
                RectTr.anchoredPosition = OriginAnchoredPosition - new Vector2(0f, margin);
            }
        }
    }
}
