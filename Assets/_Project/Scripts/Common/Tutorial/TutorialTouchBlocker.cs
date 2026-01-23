using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 튜토리얼 터치 차단 유틸리티.
    /// 구멍 영역 외 터치를 차단하는 정적 API를 제공합니다.
    /// UI(ICanvasRaycastFilter)와 3D(Physics.Raycast) 모두에서 사용 가능합니다.
    /// </summary>
    public static class TutorialTouchBlocker
    {
        private static readonly int HoleCenter = Shader.PropertyToID("_HoleCenter");
        private static readonly int HoleRadius = Shader.PropertyToID("_HoleRadius");
        private static readonly int HoleCenter2 = Shader.PropertyToID("_HoleCenter2");
        private static readonly int HoleRadius2 = Shader.PropertyToID("_HoleRadius2");
        private static readonly int AspectRatio = Shader.PropertyToID("_AspectRatio");

        private static Material _maskMaterial;
        private static RectTransform _canvasRectTransform;
        private static Camera _uiCamera;

        /// <summary>
        /// 터치 차단 활성화 여부
        /// </summary>
        public static bool IsBlocking { get; set; } = false;

        /// <summary>
        /// 3D 터치 허용 여부 (CHARACTER_PLACEMENT_UI, MOVE_OBJECT 등 드래그 전략에서 사용)
        /// true면 3D 터치는 차단하지 않음 (UI만 차단)
        /// </summary>
        public static bool Allow3DTouch { get; set; } = false;

        /// <summary>
        /// 초기화 - TutorialController에서 호출
        /// </summary>
        public static void Initialize(Material maskMaterial, RectTransform canvasRectTransform, Camera uiCamera = null)
        {
            _maskMaterial = maskMaterial;
            _canvasRectTransform = canvasRectTransform;
            _uiCamera = uiCamera;
        }

        /// <summary>
        /// 정리
        /// </summary>
        public static void Clear()
        {
            _maskMaterial = null;
            _canvasRectTransform = null;
            _uiCamera = null;
            IsBlocking = false;
            Allow3DTouch = false;
        }

        /// <summary>
        /// 스크린 좌표가 차단되어야 하는지 확인.
        /// true면 차단(터치 무시), false면 통과(터치 허용).
        /// </summary>
        public static bool ShouldBlockTouch(Vector2 screenPoint)
        {
            if (!IsBlocking)
                return false;

            if (_maskMaterial == null || _canvasRectTransform == null)
                return false;

            // 구멍 안에 있으면 통과(차단 안 함), 밖이면 차단
            return !IsPointInHole(screenPoint);
        }

        /// <summary>
        /// 스크린 좌표가 구멍 영역 안에 있는지 확인.
        /// </summary>
        public static bool IsPointInHole(Vector2 screenPoint)
        {
            if (_maskMaterial == null || _canvasRectTransform == null)
                return true;

            Vector2 uv = ScreenPointToUV(screenPoint);
            return IsUVInHole(uv);
        }

        /// <summary>
        /// 스크린 좌표를 캔버스 UV 좌표(0~1)로 변환
        /// </summary>
        private static Vector2 ScreenPointToUV(Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform,
                screenPoint,
                _uiCamera,
                out Vector2 localPoint);

            float u = (localPoint.x + (_canvasRectTransform.rect.width * 0.5f)) / _canvasRectTransform.rect.width;
            float v = (localPoint.y + (_canvasRectTransform.rect.height * 0.5f)) / _canvasRectTransform.rect.height;

            return new Vector2(u, v);
        }

        /// <summary>
        /// UV 좌표가 구멍 영역 안에 있는지 확인 (MaskHole 셰이더와 동일한 로직)
        /// </summary>
        private static bool IsUVInHole(Vector2 uv)
        {
            float aspectRatio = _maskMaterial.GetFloat(AspectRatio);
            if (aspectRatio <= 0) aspectRatio = (float)Screen.width / Screen.height;

            // 첫 번째 구멍 체크
            Vector4 holeCenter1 = _maskMaterial.GetVector(HoleCenter);
            float holeRadius1 = _maskMaterial.GetFloat(HoleRadius);

            if (holeRadius1 > 0)
            {
                Vector2 adjustedUV = new Vector2(uv.x, uv.y / aspectRatio);
                Vector2 adjustedCenter1 = new Vector2(holeCenter1.x, holeCenter1.y / aspectRatio);
                float dist1 = Vector2.Distance(adjustedUV, adjustedCenter1);

                if (dist1 < holeRadius1)
                    return true;
            }

            // 두 번째 구멍 체크
            Vector4 holeCenter2 = _maskMaterial.GetVector(HoleCenter2);
            float holeRadius2 = _maskMaterial.GetFloat(HoleRadius2);

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
