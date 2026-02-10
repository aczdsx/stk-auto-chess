using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 튜토리얼 마스크 셰이더 관련 유틸리티.
    /// 셰이더 프로퍼티 ID와 홀 판정 로직을 중앙화합니다.
    /// </summary>
    public static class TutorialShaderHelper
    {
        public static readonly int HoleCenter = Shader.PropertyToID("_HoleCenter");
        public static readonly int HoleRadius = Shader.PropertyToID("_HoleRadius");
        public static readonly int HoleCenter2 = Shader.PropertyToID("_HoleCenter2");
        public static readonly int HoleRadius2 = Shader.PropertyToID("_HoleRadius2");
        public static readonly int AspectRatio = Shader.PropertyToID("_AspectRatio");
        public static readonly int MaskAlpha = Shader.PropertyToID("_MaskAlpha");

        /// <summary>
        /// 스크린 좌표를 캔버스 UV 좌표(0~1)로 변환
        /// </summary>
        public static Vector2 ScreenPointToUV(RectTransform canvasRect, Vector2 screenPoint, Camera camera = null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, camera, out Vector2 localPoint);

            float u = (localPoint.x + (canvasRect.rect.width * 0.5f)) / canvasRect.rect.width;
            float v = (localPoint.y + (canvasRect.rect.height * 0.5f)) / canvasRect.rect.height;

            return new Vector2(u, v);
        }

        /// <summary>
        /// UV 좌표가 구멍 영역 안에 있는지 확인 (MaskHole 셰이더와 동일한 로직)
        /// </summary>
        public static bool IsPointInHole(Material maskMaterial, Vector2 uv)
        {
            float aspectRatio = maskMaterial.GetFloat(AspectRatio);
            if (aspectRatio <= 0) aspectRatio = (float)Screen.width / Screen.height;

            // 첫 번째 구멍 체크
            Vector4 holeCenter1 = maskMaterial.GetVector(HoleCenter);
            float holeRadius1 = maskMaterial.GetFloat(HoleRadius);

            if (holeRadius1 > 0)
            {
                Vector2 adjustedUV = new Vector2(uv.x, uv.y / aspectRatio);
                Vector2 adjustedCenter1 = new Vector2(holeCenter1.x, holeCenter1.y / aspectRatio);
                float dist1 = Vector2.Distance(adjustedUV, adjustedCenter1);

                if (dist1 < holeRadius1)
                    return true;
            }

            // 두 번째 구멍 체크
            Vector4 holeCenter2 = maskMaterial.GetVector(HoleCenter2);
            float holeRadius2 = maskMaterial.GetFloat(HoleRadius2);

            if (holeRadius2 > 0)
            {
                Vector2 adjustedUV = new Vector2(uv.x, uv.y / aspectRatio);
                Vector2 adjustedCenter2 = new Vector2(holeCenter2.x, holeCenter2.y / aspectRatio);
                float dist2 = Vector2.Distance(adjustedUV, adjustedCenter2);

                if (dist2 < holeRadius2)
                    return true;
            }

            return false;
        }
    }
}
