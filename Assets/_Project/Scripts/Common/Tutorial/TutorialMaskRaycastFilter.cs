using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 튜토리얼 마스크 구멍 영역만 레이캐스트를 통과시키는 필터.
    /// MaskHole 셰이더와 동일한 원 계산 로직을 사용합니다.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class TutorialMaskRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        private static readonly int HoleCenter = Shader.PropertyToID("_HoleCenter");
        private static readonly int HoleRadius = Shader.PropertyToID("_HoleRadius");
        private static readonly int HoleCenter2 = Shader.PropertyToID("_HoleCenter2");
        private static readonly int HoleRadius2 = Shader.PropertyToID("_HoleRadius2");
        private static readonly int AspectRatio = Shader.PropertyToID("_AspectRatio");

        [SerializeField] private Material _maskMaterial;
        [SerializeField] private RectTransform _canvasRectTransform;

        /// <summary>
        /// true면 구멍 외 영역 레이캐스트 차단, false면 차단 안 함
        /// </summary>
        private bool _blockOutsideHole = false;

        /// <summary>
        /// 구멍과 상관없이 항상 터치 허용할 UI 목록
        /// </summary>
        private static readonly List<RectTransform> _allowedUITargets = new();

        public bool BlockOutsideHole
        {
            get => _blockOutsideHole;
            set => _blockOutsideHole = value;
        }

        #region Allowed UI Targets

        /// <summary>
        /// 항상 터치 허용할 UI 추가
        /// </summary>
        public static void AddAllowedUITarget(RectTransform target)
        {
            if (target != null && !_allowedUITargets.Contains(target))
                _allowedUITargets.Add(target);
        }

        /// <summary>
        /// 허용 UI 제거
        /// </summary>
        public static void RemoveAllowedUITarget(RectTransform target)
        {
            _allowedUITargets.Remove(target);
        }

        /// <summary>
        /// 허용 UI 목록 초기화
        /// </summary>
        public static void ClearAllowedUITargets()
        {
            _allowedUITargets.Clear();
        }

        #endregion

        public void Initialize(Material maskMaterial, RectTransform canvasRectTransform)
        {
            _maskMaterial = maskMaterial;
            _canvasRectTransform = canvasRectTransform;
        }

        /// <summary>
        /// ICanvasRaycastFilter 구현.
        /// 구멍 영역 내부면 false(딤드 통과 → 아래 UI 터치 가능),
        /// 외부면 true(딤드가 터치 받음 → 아래 UI 차단).
        /// 허용된 UI 영역은 구멍과 상관없이 항상 통과.
        /// </summary>
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            // 차단 모드가 아니면 항상 통과 (딤드가 터치 안 받음)
            if (!_blockOutsideHole)
                return false;

            if (_maskMaterial == null || _canvasRectTransform == null)
                return false;

            // 허용된 UI 영역 체크 - 해당 영역이면 딤드 통과
            foreach (var target in _allowedUITargets)
            {
                if (target != null && RectTransformUtility.RectangleContainsScreenPoint(target, screenPoint, eventCamera))
                {
                    return false; // 딤드 통과 → 해당 UI 터치 가능
                }
            }

            // 스크린 좌표를 UV 좌표(0~1)로 변환
            Vector2 uv = ScreenPointToUV(screenPoint, eventCamera);

            // 구멍 안이면 false (딤드 통과), 밖이면 true (딤드가 차단)
            bool isInHole = IsPointInHole(uv);
            return !isInHole;
        }

        /// <summary>
        /// 스크린 좌표가 구멍 영역 안에 있는지 확인 (외부에서 호출용)
        /// </summary>
        public bool IsScreenPointInHole(Vector2 screenPoint, Camera eventCamera = null)
        {
            if (_maskMaterial == null || _canvasRectTransform == null)
                return true;

            Vector2 uv = ScreenPointToUV(screenPoint, eventCamera);
            return IsPointInHole(uv);
        }

        /// <summary>
        /// 스크린 좌표를 캔버스 UV 좌표(0~1)로 변환
        /// </summary>
        private Vector2 ScreenPointToUV(Vector2 screenPoint, Camera eventCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform,
                screenPoint,
                eventCamera,
                out Vector2 localPoint);

            // 로컬 좌표를 0~1 UV로 변환
            float u = (localPoint.x + (_canvasRectTransform.rect.width * 0.5f)) / _canvasRectTransform.rect.width;
            float v = (localPoint.y + (_canvasRectTransform.rect.height * 0.5f)) / _canvasRectTransform.rect.height;

            return new Vector2(u, v);
        }

        /// <summary>
        /// UV 좌표가 구멍 영역 안에 있는지 확인 (MaskHole 셰이더와 동일한 로직)
        /// </summary>
        private bool IsPointInHole(Vector2 uv)
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

            // 둘 다 구멍 안에 없음
            return false;
        }
    }
}
