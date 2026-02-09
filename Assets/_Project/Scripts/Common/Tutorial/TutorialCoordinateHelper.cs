using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 튜토리얼 좌표 변환 유틸리티.
    /// 월드/UI 좌표 → UV 좌표 변환, 홀 반지름 애니메이션,
    /// 드래그 오브젝트/화살표 위치 업데이트를 중앙화합니다.
    /// </summary>
    public static class TutorialCoordinateHelper
    {
        private static readonly Vector2 DefaultFallback = new(0.5f, 0.5f);

        /// <summary>
        /// 3D 월드 좌표를 마스크 UV 좌표로 변환
        /// </summary>
        public static Vector2 CalculateWorldPositionUV(
            Vector3 worldPosition,
            RectTransform canvasRect,
            Camera camera = null,
            Vector2? fallback = null)
        {
            Camera cam = camera != null ? camera : MainCameraHolder.MainCamera;
            Vector2 fb = fallback ?? DefaultFallback;

            if (cam == null) return fb;

            Vector3 screenPosition = cam.WorldToScreenPoint(worldPosition);
            if (screenPosition.z < 0) return fb;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosition, null, out var localPoint);

            return new Vector2(
                (localPoint.x + (canvasRect.rect.width * 0.5f)) / canvasRect.rect.width,
                (localPoint.y + (canvasRect.rect.height * 0.5f)) / canvasRect.rect.height);
        }

        /// <summary>
        /// UI RectTransform의 중심 위치를 UV 좌표로 변환
        /// </summary>
        public static Vector2 CalculateUIPositionUV(
            RectTransform targetRect,
            RectTransform canvasRect,
            Camera uiCamera = null,
            Vector2? fallback = null)
        {
            Vector2 fb = fallback ?? DefaultFallback;

            if (targetRect == null || canvasRect == null) return fb;

            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) / 2f;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, uiCamera, out var localPoint);

            return new Vector2(
                (localPoint.x + (canvasRect.rect.width * 0.5f)) / canvasRect.rect.width,
                (localPoint.y + (canvasRect.rect.height * 0.5f)) / canvasRect.rect.height);
        }

        /// <summary>
        /// 홀 반지름 성장 애니메이션 (SmoothStep)
        /// </summary>
        public static bool UpdateHoleRadius(
            Material maskMaterial,
            float targetRadius,
            ref float holeRadiusAnimTime,
            ref bool holeGrown,
            float growDuration = 0.5f)
        {
            if (holeGrown) return true;

            holeRadiusAnimTime += Time.deltaTime;
            float t = Mathf.Clamp01(holeRadiusAnimTime / growDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            float currentRadius = Mathf.Lerp(0f, targetRadius, t);
            maskMaterial.SetFloat(TutorialShaderHelper.HoleRadius, currentRadius);

            if (t >= 1f) holeGrown = true;
            return holeGrown;
        }

        /// <summary>
        /// 드래그 오브젝트 위치를 UV 좌표 기반으로 업데이트
        /// </summary>
        public static void UpdateDragObjPosition(
            GameObject dragObj,
            RectTransform canvasRect,
            Vector2 uvPosition)
        {
            if (dragObj == null) return;

            var dragRect = dragObj.GetComponent<RectTransform>();
            if (dragRect == null || canvasRect == null) return;

            float localX = (uvPosition.x - 0.5f) * canvasRect.rect.width;
            float localY = (uvPosition.y - 0.5f) * canvasRect.rect.height;
            dragRect.localPosition = new Vector3(localX, localY, 0f);
        }

        /// <summary>
        /// 화살표 위치를 월드 좌표 기반으로 업데이트
        /// </summary>
        public static void UpdateArrowPosition(
            RectTransform arrowRect,
            RectTransform canvasRect,
            Vector3 worldTargetPosition,
            float yOffset = 0f)
        {
            if (arrowRect == null || canvasRect == null) return;

            Camera cam = MainCameraHolder.MainCamera;
            if (cam == null) return;

            Vector3 screenPosition = cam.WorldToScreenPoint(worldTargetPosition);
            if (screenPosition.z < 0) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosition, null, out var localPoint);

            arrowRect.localPosition = new Vector3(localPoint.x, localPoint.y + yOffset, 0f);
        }
    }
}
