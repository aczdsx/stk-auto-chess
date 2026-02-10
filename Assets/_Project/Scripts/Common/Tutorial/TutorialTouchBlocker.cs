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

            Vector2 uv = TutorialShaderHelper.ScreenPointToUV(_canvasRectTransform, screenPoint, _uiCamera);
            return TutorialShaderHelper.IsPointInHole(_maskMaterial, uv);
        }
    }
}
