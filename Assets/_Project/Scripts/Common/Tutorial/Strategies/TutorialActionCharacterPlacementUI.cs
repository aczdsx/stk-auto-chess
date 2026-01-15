using CookApps.BattleSystem;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// UI 캐릭터 배치 튜토리얼 액션.
    /// 하단 UI의 캐릭터를 드래그하여 타일에 배치하도록 유도합니다.
    /// 마스크 홀이 드래그를 따라다니며 배치 위치를 하이라이트합니다.
    ///
    /// tutorial_action_key: UI 타겟 이름 (TutorialTargetRegistry에 등록된 이름)
    ///
    /// 동작 흐름:
    /// 1. tutorial_action_key로 UI 타겟(배치 가능한 캐릭터 UI) 찾기
    /// 2. 마스크 홀이 해당 타겟을 따라다님
    /// 3. 드래그 시작 시 드래그 오브젝트로 마스크 홀 타겟 변경
    /// 4. 배치 완료 시 자동으로 다음 튜토리얼로 진행
    /// </summary>
    public class TutorialActionCharacterPlacementUI : ITutorialActionStrategy
    {
        // 구멍 셰이더 프로퍼티 ID
        private static readonly int HoleRadius = Shader.PropertyToID("_HoleRadius");
        private static readonly int HoleRadius2 = Shader.PropertyToID("_HoleRadius2");
        private static readonly int HoleCenter2 = Shader.PropertyToID("_HoleCenter2");

        // 드래그 시 하이라이트할 타일 ID
        private const int TARGET_TILE_ID = 7;

        // 두 번째 구멍 기본 반지름
        private const float SECOND_HOLE_RADIUS = 0.08f;

        /// <summary>
        /// 배치 완료 시 호출되는 콜백 (TutorialController에서 설정)
        /// </summary>
        public static System.Action OnPlacementCompleted;

        /// <summary>
        /// 현재 튜토리얼 진행 중인지 여부
        /// </summary>
        public static bool IsActive { get; private set; }

        /// <summary>
        /// 현재 컨텍스트 참조 (드래그 업데이트용)
        /// </summary>
        private static TutorialActionContext _currentContext;

        /// <summary>
        /// 초기 타겟 UI 오브젝트 (드래그 시작 전 마스크 홀 위치)
        /// </summary>
        private static GameObject _initialTargetUI;

        /// <summary>
        /// 첫 번째 구멍의 원래 반지름 (복원용)
        /// </summary>
        private static float _savedHoleRadius;

        public void OnShow(TutorialActionContext context)
        {
            // UI 타겟 찾기
            var targetKey = context.CurrentTutorial.tutorial_action_key;
            context.TargetUIObj = TutorialTargetRegistry.FindGameObject(targetKey);

            if (context.TargetUIObj == null)
            {
                Debug.LogWarning($"[TutorialActionCharacterPlacementUI] 타겟을 찾을 수 없음: {targetKey}");
                context.NextObj.SetActive(true);
                return;
            }

            // 초기 타겟 저장
            _initialTargetUI = context.TargetUIObj;

            // 원위치 정보 저장
            context.OriginalParent = context.TargetUIObj.transform.parent;
            context.OriginalSiblingIndex = context.TargetUIObj.transform.GetSiblingIndex();
            context.OriginalPosition = context.TargetUIObj.transform.localPosition;

            // 타겟을 최상위(딤드 위)로 이동하여 터치 가능하게 함
            context.TargetUIObj.transform.SetParent(context.TargetSpawnTransform, true);

            // 마스크 홀 타겟 설정 (초기에는 UI 타겟 위치)
            context.TargetUnmaskObj = context.TargetUIObj;

            // 화살표 설정 (타겟 위치에 표시)
            SetArrowPosition(context);

            // 상태 설정
            IsActive = true;
            _currentContext = context;
        }

        public void OnNext(TutorialActionContext context)
        {
            // 배치 튜토리얼에서는 "다음" 버튼을 표시하지 않음
            // 드래그 배치가 완료되면 자동으로 진행됨
            context.NextObj.SetActive(false);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            // 딤드 클릭으로 진행 불가 - 반드시 드래그 배치해야 함
            return false;
        }

        public void OnClear(TutorialActionContext context)
        {
            // 타겟 UI 원위치 복구
            if (context.OriginalParent != null && context.TargetUIObj != null)
            {
                context.TargetUIObj.transform.SetParent(context.OriginalParent);
                context.TargetUIObj.transform.SetSiblingIndex(context.OriginalSiblingIndex);
                context.TargetUIObj.transform.localPosition = context.OriginalPosition;
            }

            // 두 번째 구멍 숨김
            HideSecondHole();

            // 상태 초기화
            IsActive = false;
            _currentContext = null;
            _initialTargetUI = null;
            OnPlacementCompleted = null;

            // 화살표 비활성화
            context.ArrowRectTransform.gameObject.SetActive(false);

            // 컨텍스트 정리
            context.TargetUnmaskObj = null;
            context.TargetUIObj = null;
            context.OriginalParent = null;
        }

        /// <summary>
        /// 화살표 위치 설정
        /// </summary>
        private void SetArrowPosition(TutorialActionContext context)
        {
            if (context.TargetUIObj == null) return;

            var targetRect = context.TargetUIObj.GetComponent<RectTransform>();
            if (targetRect == null) return;

            context.ArrowRectTransform.gameObject.SetActive(true);
            Vector3 targetPosition = targetRect.localPosition;
            context.ArrowRectTransform.localPosition = new Vector3(
                targetPosition.x,
                targetPosition.y + context.CurrentTutorial.arrow_yPos,
                targetPosition.z);
        }

        #region External API

        /// <summary>
        /// 드래그 시작 시 호출 - 드래그 오브젝트로 마스크 홀 타겟 변경
        /// </summary>
        /// <param name="dragObject">드래그 중인 오브젝트</param>
        public static void OnDragStart(GameObject dragObject)
        {
            if (!IsActive || _currentContext == null) return;

            // 드래그 오브젝트를 마스크 홀 타겟으로 설정
            _currentContext.TargetUnmaskObj = dragObject;

            // 화살표 숨김 (드래그 중에는 불필요)
            _currentContext.ArrowRectTransform.gameObject.SetActive(false);

            // Tile5에 두 번째 구멍 표시
            ShowSecondHoleAtTile(TARGET_TILE_ID);
        }

        /// <summary>
        /// 마스크 타겟 업데이트 - 보드에 캐릭터가 생성된 후 해당 캐릭터로 마스크 타겟 변경
        /// </summary>
        /// <param name="targetObject">새로운 마스크 타겟 (보드 위의 캐릭터)</param>
        public static void UpdateMaskTarget(GameObject targetObject)
        {
            if (!IsActive || _currentContext == null) return;

            _currentContext.TargetUnmaskObj = targetObject;
        }

        /// <summary>
        /// 드래그 종료 시 호출 - 마스크 홀 타겟을 초기 위치로 복원 (배치 취소 시)
        /// </summary>
        public static void OnDragCancel()
        {
            if (!IsActive || _currentContext == null) return;

            // 초기 타겟으로 복원
            _currentContext.TargetUnmaskObj = _initialTargetUI;

            // 두 번째 구멍 숨김
            HideSecondHole();

            // 화살표 다시 표시
            if (_initialTargetUI != null)
            {
                var targetRect = _initialTargetUI.GetComponent<RectTransform>();
                if (targetRect != null)
                {
                    _currentContext.ArrowRectTransform.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// 배치 완료 시 외부에서 호출
        /// </summary>
        public static void NotifyPlacementCompleted()
        {
            if (IsActive)
            {
                OnPlacementCompleted?.Invoke();
            }
        }

        /// <summary>
        /// 현재 튜토리얼 활성 상태 확인
        /// </summary>
        public static bool IsCharacterPlacementUIActive()
        {
            return IsActive;
        }

        #endregion

        #region Second Hole Control

        /// <summary>
        /// 지정된 타일 위치에 두 번째 구멍 표시 (첫 번째 구멍은 숨김)
        /// </summary>
        private static void ShowSecondHoleAtTile(int tileId)
        {
            if (_currentContext?.MaskMaterial == null) return;

            var tile = InGameObjectManager.Instance.InGameGrid.GetTile(tileId);

            if (tile?.View == null) return;

            // 첫 번째 구멍 반지름 저장 후 숨김
            _savedHoleRadius = _currentContext.MaskMaterial.GetFloat(HoleRadius);
            _currentContext.MaskMaterial.SetFloat(HoleRadius, 0f);

            // 두 번째 구멍 표시
            Vector3 worldPos = tile.View.Position;
            Vector2 uvPos = CalculateWorldPositionUV(worldPos);

            _currentContext.MaskMaterial.SetVector(HoleCenter2, new Vector4(uvPos.x, uvPos.y, 0, 0));
            _currentContext.MaskMaterial.SetFloat(HoleRadius2, SECOND_HOLE_RADIUS);
        }

        /// <summary>
        /// 두 번째 구멍 숨김 및 첫 번째 구멍 복원
        /// </summary>
        private static void HideSecondHole()
        {
            if (_currentContext?.MaskMaterial == null) return;

            // 두 번째 구멍 숨김
            _currentContext.MaskMaterial.SetFloat(HoleRadius2, 0f);

            // 첫 번째 구멍 복원
            if (_savedHoleRadius > 0f)
            {
                _currentContext.MaskMaterial.SetFloat(HoleRadius, _savedHoleRadius);
                _savedHoleRadius = 0f;
            }
        }

        /// <summary>
        /// 3D 월드 좌표를 마스크 UV 좌표로 변환
        /// </summary>
        private static Vector2 CalculateWorldPositionUV(Vector3 worldPosition)
        {
            Camera cam = _currentContext?.MainCamera ?? Camera.main;
            if (cam == null) return Vector2.zero;

            Vector3 screenPosition = cam.WorldToScreenPoint(worldPosition);
            if (screenPosition.z < 0) return Vector2.zero;

            RectTransform canvasRect = _currentContext?.CanvasRectTransform;
            if (canvasRect == null) return Vector2.zero;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                null,
                out var localPoint);

            return new Vector2(
                (localPoint.x + (canvasRect.rect.width * 0.5f)) / canvasRect.rect.width,
                (localPoint.y + (canvasRect.rect.height * 0.5f)) / canvasRect.rect.height);
        }

        #endregion
    }
}
