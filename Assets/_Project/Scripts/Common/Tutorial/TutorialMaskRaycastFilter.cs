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
            Vector2 uv = TutorialShaderHelper.ScreenPointToUV(_canvasRectTransform, screenPoint, eventCamera);

            // 구멍 안이면 false (딤드 통과), 밖이면 true (딤드가 차단)
            bool isInHole = TutorialShaderHelper.IsPointInHole(_maskMaterial, uv);
            return !isInHole;
        }

        /// <summary>
        /// 스크린 좌표가 구멍 영역 안에 있는지 확인 (외부에서 호출용)
        /// </summary>
        public bool IsScreenPointInHole(Vector2 screenPoint, Camera eventCamera = null)
        {
            if (_maskMaterial == null || _canvasRectTransform == null)
                return true;

            Vector2 uv = TutorialShaderHelper.ScreenPointToUV(_canvasRectTransform, screenPoint, eventCamera);
            return TutorialShaderHelper.IsPointInHole(_maskMaterial, uv);
        }
    }
}
