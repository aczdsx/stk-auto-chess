using System;
using CookApps.TeamBattle.Utility;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 튜토리얼 액션 전략의 컨텍스트.
    /// 전략이 필요로 하는 UI 요소들에 대한 참조를 제공합니다.
    /// </summary>
    public class TutorialActionContext
    {
        public TutorialDialogue CurrentTutorial { get; set; }

        // UI 요소들
        public GameObject NextObj { get; set; }
        public RectTransform ArrowRectTransform { get; set; }
        public Transform TargetSpawnTransform { get; set; }
        public Image DimmedImage { get; set; }

        // 3D 타겟용 화살표 (월드 좌표 추적)
        public RectTransform WorldArrowRectTransform { get; set; }
        public Canvas TutorialCanvas { get; set; }
        public Camera MainCamera { get; set; }
        public RectTransform CanvasRectTransform { get; set; }

        // 마스크 머티리얼 (두 번째 구멍 제어용)
        public Material MaskMaterial { get; set; }

        // 타겟 관련 (전략에서 설정)
        public GameObject TargetUIObj { get; set; }
        public GameObject TargetUnmaskObj { get; set; }

        // 3D 월드 타겟 (CHARACTER_PLACEMENT에서 사용)
        public Vector3? WorldTargetPosition { get; set; }

        // 버튼 원위치 정보 (FORCED_TOUCH_UI에서 사용)
        public Transform OriginalParent { get; set; }
        public int OriginalSiblingIndex { get; set; }
        public Vector3 OriginalPosition { get; set; }

        // 드래그 오브젝트 (CHARACTER_PLACEMENT_UI에서 사용)
        public GameObject DragObj { get; set; }

        // 튜토리얼 토스트 UI (TOAST_MESSAGE에서 사용)
        public GameObject TutorialToastObj { get; set; }
        public TextMeshProUGUI TutorialToastText { get; set; }
        public Animator TutorialToastAnimator { get; set; }

        // 마스크 위치/크기 업데이트 건너뛰기 (SPAWN_ENEMY, TOAST_MESSAGE 등에서 사용)
        public bool SkipMaskUpdate { get; set; }

        // TargetSpawnObj의 SafeArea (FORCED_TOUCH_UI에서 Refresh용)
        public SafeArea TutorialSafeArea { get; set; }

        #region Full Screen Mask Helpers

        private static readonly int MaskAlpha = Shader.PropertyToID("_MaskAlpha");
        private const float MASK_ANIMATION_DURATION = 0.8f;

        private MotionHandle _maskHandle;

        /// <summary>
        /// 전체 화면이 보이도록 마스크 알파를 0으로 애니메이션
        /// </summary>
        public void SetFullScreenMask()
        {
            SkipMaskUpdate = true;

            if (MaskMaterial != null)
            {
                _maskHandle.TryCancel();

                float currentAlpha = MaskMaterial.GetFloat(MaskAlpha);
                _maskHandle = LMotion.Create(currentAlpha, 0f, MASK_ANIMATION_DURATION)
                    .WithEase(Ease.InOutSine)
                    .Bind(MaskMaterial, (x, mask) => mask.SetFloat(MaskAlpha, x));
            }
        }

        /// <summary>
        /// 마스크 알파를 1로 복원 (부드럽게)
        /// </summary>
        public void RestoreMask()
        {
            SkipMaskUpdate = false;

            if (MaskMaterial != null)
            {
                _maskHandle.TryCancel();

                float currentAlpha = MaskMaterial.GetFloat(MaskAlpha);
                _maskHandle = LMotion.Create(currentAlpha, 1f, MASK_ANIMATION_DURATION)
                    .WithEase(Ease.InOutSine)
                    .Bind(MaskMaterial, (x, mask) => mask.SetFloat(MaskAlpha, x));
            }
        }

        #endregion
    }

    /// <summary>
    /// 튜토리얼 액션 전략 인터페이스.
    /// 각 TutorialActionType별로 구현체를 만듭니다.
    /// </summary>
    public interface ITutorialActionStrategy
    {
        /// <summary>
        /// 튜토리얼이 표시될 때 호출 (ShowNextTutorial)
        /// </summary>
        void OnShow(TutorialActionContext context);

        /// <summary>
        /// 다음 단계로 진행할 준비가 됐을 때 호출 (OnNextObj - 애니메이션 완료 후)
        /// </summary>
        void OnNext(TutorialActionContext context);

        /// <summary>
        /// 딤드 배경 클릭 시 다음으로 진행 가능한지 여부
        /// </summary>
        bool CanProceedOnDimmedClick(TutorialActionContext context);

        /// <summary>
        /// 튜토리얼 정리 시 호출 (ClearTutorial)
        /// </summary>
        void OnClear(TutorialActionContext context);
    }
}
