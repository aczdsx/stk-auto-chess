using CookApps.BattleSystem;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// UI 캐릭터 배치 튜토리얼 액션.
    /// 하단 UI의 캐릭터를 드래그하여 타일에 배치하도록 유도합니다.
    /// 마스크 홀이 UI슬롯(A)→타겟타일(B) 왕복 애니메이션을 수행합니다.
    ///
    /// tutorial_action_key 형식: "SlotName->TileId" (예: "Slot_3401->7")
    ///   - SlotName: UI 타겟 이름 (TutorialTargetRegistry에 등록된 이름)
    ///   - TileId: 배치해야 할 타일 ID
    ///
    /// 동작 흐름:
    /// 1. tutorial_action_key 파싱하여 UI 타겟과 타일 ID 추출
    /// 2. 마스크 홀이 A(UI슬롯)→B(타겟타일) 왕복 애니메이션
    /// 3. 드래그 시작 시 애니메이션 중지, 초기 위치(A)로 복귀
    /// 4. 지정된 타일에 배치 완료 시 자동으로 다음 튜토리얼로 진행
    /// </summary>
    public class TutorialActionCharacterPlacementUI : ITutorialActionStrategy
    {

        /// <summary>
        /// 드래그 시 하이라이트할 타일 ID (tutorial_action_key에서 파싱)
        /// </summary>
        private static int _targetTileId;


        /// <summary>
        /// 현재 튜토리얼 진행 중인지 여부
        /// </summary>
        public static bool IsActive { get; private set; }


        /// <summary>
        /// 현재 컨텍스트 참조 (드래그 업데이트용)
        /// </summary>
        private static TutorialActionContext _currentContext;

        /// <summary>
        /// 초기 타겟 UI 오브젝트 (A 위치)
        /// </summary>
        private static GameObject _initialTargetUI;

        // A→B 반복 애니메이션
        private static float _animTime;
        private const float ANIM_DURATION = 1.2f; // A→B 편도 시간

        // 홀 크기 애니메이션
        private static float _holeRadiusAnimTime;
        private const float HOLE_GROW_DURATION = 0.5f;
        private static bool _holeGrown;

        // 위치 캐싱
        private static Vector3 _destTilePosition;
        private static Vector2 _sourceUV; // 초기 UI 위치 (UV 좌표로 캐싱)
        private static bool _positionsValid;

        // 레이아웃 대기 (다이얼로그 팝업 닫힘 후 UI 안정화 대기)
        private static float _layoutWaitTime;
        private const float LAYOUT_WAIT_DURATION = 0.1f; // 레이아웃 안정화 대기 시간
        private static bool _sourceUVCached;

        // 타겟 타일 뷰 캐싱 (Navigator 제어용)
        private static InGameTileView _targetTileView;

        // 딤 레이어 비활성화 (hole_radius >= 1일 때, _MaskAlpha 셰이더 프로퍼티 사용)
        private static bool _dimmedDisabled;
        private static float _originalMaskAlpha;

        public void OnShow(TutorialActionContext context)
        {
            _currentContext = context;
            _animTime = 0f;
            _holeRadiusAnimTime = 0f;
            _holeGrown = false;
            _positionsValid = false;
            _layoutWaitTime = 0f;
            _sourceUVCached = false;

            // 드래그 오브젝트 비활성화 (초기 위치 캐싱 후 활성화)
            if (context.DragObj != null)
            {
                context.DragObj.SetActive(false);
            }

            // tutorial_action_key 파싱 (형식: "Slot_3401->7")
            var actionKey = context.CurrentTutorial.tutorial_action_key;
            var (targetKey, tileId) = ParseActionKey(actionKey);
            _targetTileId = tileId;

            // UI 타겟 찾기
            context.TargetUIObj = TutorialTargetRegistry.FindGameObject(targetKey);

            if (context.TargetUIObj == null)
            {
                Debug.LogWarning($"[TutorialActionCharacterPlacementUI] 타겟을 찾을 수 없음: {targetKey}");
                context.NextObj.SetActive(true);
                return;
            }

            // 초기 타겟 저장
            _initialTargetUI = context.TargetUIObj;

            // 타겟 타일 위치 가져오기
            if (!GetTilePosition())
            {
                Debug.LogWarning($"[TutorialActionCharacterPlacementUI] 타일 위치를 찾을 수 없음: {_targetTileId}");
                context.NextObj.SetActive(true);
                return;
            }

            // UI 위치 캐싱은 UpdateHolePositions()에서 레이아웃 대기 후 수행
            // (다이얼로그 팝업 닫힘 직후 UI 레이아웃이 안정화될 때까지 대기)

            // TutorialController의 마스크 업데이트 건너뛰기 (직접 처리)
            context.SkipMaskUpdate = true;

            // hole_radius >= 1이면 Hole 및 딤 사용 안함
            bool useHole = context.CurrentTutorial.hole_radius < 1f;
            _dimmedDisabled = !useHole;

            if (useHole)
            {
                // 초기 홀 크기 0으로 설정 (애니메이션으로 커짐)
                context.MaskMaterial.SetFloat(TutorialShaderHelper.HoleRadius, 0f);

                // 초기 홀 위치를 타겟 타일로 설정 (이전 액션의 HoleCenter 잔류 방지)
                Vector2 destUV = TutorialCoordinateHelper.CalculateWorldPositionUV(
                    _destTilePosition, context.CanvasRectTransform);
                context.MaskMaterial.SetVector(TutorialShaderHelper.HoleCenter, new Vector4(destUV.x, destUV.y, 0, 0));
            }
            else
            {
                // Hole 비활성화
                context.MaskMaterial.SetFloat(TutorialShaderHelper.HoleRadius, 0f);

                // 딤 비활성화 (밝게) - _MaskAlpha = 0
                if (context.MaskMaterial != null)
                {
                    _originalMaskAlpha = context.MaskMaterial.GetFloat(TutorialShaderHelper.MaskAlpha);
                    context.MaskMaterial.SetFloat(TutorialShaderHelper.MaskAlpha, 0f);
                }
            }

            // 화살표 활성화 및 타일 위치로 이동
            context.ArrowRectTransform.gameObject.SetActive(true);
            float yOff = context.CurrentTutorial?.arrow_yPos ?? 0;
            TutorialCoordinateHelper.UpdateArrowPosition(
                context.ArrowRectTransform, context.CanvasRectTransform, _destTilePosition, yOff);

            // 딤드 이미지는 raycastTarget 유지 (ICanvasRaycastFilter가 구멍 영역만 통과시킴)
            // raycastTarget = false로 하면 모든 UI 터치가 통과되므로 제거

            // 타겟 UI는 구멍과 상관없이 항상 터치 허용
            if (context.TargetUIObj != null)
            {
                var targetRect = context.TargetUIObj.GetComponent<RectTransform>();
                TutorialMaskRaycastFilter.AddAllowedUITarget(targetRect);
            }

            // 3D 터치 허용 (캐릭터 드래그 가능하도록)
            TutorialTouchBlocker.Allow3DTouch = true;

            // 상태 설정
            IsActive = true;
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
            // 위치가 유효하지 않으면 딤드 클릭으로 진행 가능 (fallback)
            return !_positionsValid;
        }

        public void OnClear(TutorialActionContext context)
        {
            // 드래그 오브젝트 비활성화
            if (context.DragObj != null)
            {
                context.DragObj.SetActive(false);
            }

            // 딤 레이어 복구 (어둡게) - _MaskAlpha 원래값
            if (_dimmedDisabled && context.MaskMaterial != null)
            {
                context.MaskMaterial.SetFloat(TutorialShaderHelper.MaskAlpha, _originalMaskAlpha);
            }
            _dimmedDisabled = false;

            // 타겟 UI 허용 해제
            if (_initialTargetUI != null)
            {
                var targetRect = _initialTargetUI.GetComponent<RectTransform>();
                TutorialMaskRaycastFilter.RemoveAllowedUITarget(targetRect);
            }

            // 3D 터치 허용 해제
            TutorialTouchBlocker.Allow3DTouch = false;

            // 마스크 업데이트 복원
            context.SkipMaskUpdate = false;

            // 홀 숨기기
            if (context.MaskMaterial != null)
            {
                context.MaskMaterial.SetFloat(TutorialShaderHelper.HoleRadius, 0f);
            }

            // Commander SkillNavigateObj 비활성화
            if (_targetTileView != null)
            {
                _targetTileView.SetNavigateObj(false);
            }

            // 상태 초기화
            IsActive = false;
            _currentContext = null;
            _initialTargetUI = null;
            _targetTileId = 0;
            _targetTileView = null;
            _animTime = 0f;
            _holeRadiusAnimTime = 0f;
            _holeGrown = false;
            _positionsValid = false;
            _sourceUV = Vector2.zero;
            _layoutWaitTime = 0f;
            _sourceUVCached = false;
            _dimmedDisabled = false;

            // 화살표 비활성화
            context.ArrowRectTransform.gameObject.SetActive(false);

            // 컨텍스트 정리
            context.TargetUnmaskObj = null;
            context.TargetUIObj = null;
            context.OriginalParent = null;
        }

        /// <summary>
        /// tutorial_action_key 파싱 (형식: "Slot_3401->7")
        /// </summary>
        /// <param name="actionKey">액션 키 문자열</param>
        /// <returns>(타겟 이름, 타일 ID) 튜플</returns>
        private static (string targetKey, int tileId) ParseActionKey(string actionKey)
        {
            if (string.IsNullOrEmpty(actionKey))
            {
                Debug.LogWarning("[TutorialActionCharacterPlacementUI] actionKey가 비어있음");
                return (string.Empty, 0);
            }

            var parts = actionKey.Split(new[] { "->" }, System.StringSplitOptions.None);
            if (parts.Length != 2)
            {
                Debug.LogWarning($"[TutorialActionCharacterPlacementUI] actionKey 형식 오류: {actionKey} (예상 형식: Slot_3401->7)");
                return (actionKey, 0);
            }

            var targetKey = parts[0].Trim();
            if (!int.TryParse(parts[1].Trim(), out var tileId))
            {
                Debug.LogWarning($"[TutorialActionCharacterPlacementUI] 타일 ID 파싱 실패: {parts[1]}");
                tileId = 0;
            }

            return (targetKey, tileId);
        }

        /// <summary>
        /// 타겟 타일 위치 가져오기
        /// </summary>
        private static bool GetTilePosition()
        {
            var grid = InGameObjectManager.Instance?.InGameGrid;
            if (grid == null)
            {
                return false;
            }

            var tile = grid.GetTile(_targetTileId);
            if (tile?.View == null)
            {
                return false;
            }

            _targetTileView = tile.View;
            _destTilePosition = tile.View.Position;
            _positionsValid = true;

            // Commander SkillNavigateObj 활성화
            _targetTileView.SetNavigateObj(true);

            return true;
        }

        public void OnUpdate(TutorialActionContext context)
        {
            UpdateHolePositions();
        }

        /// <summary>
        /// 매 프레임 홀 위치 업데이트
        /// A(UI슬롯)→B(타겟타일) 왕복 애니메이션
        /// </summary>
        private static void UpdateHolePositions()
        {
            if (!IsActive || !_positionsValid || _currentContext == null)
            {
                return;
            }

            var canvasRect = _currentContext.CanvasRectTransform;

            // hole_radius >= 1이면 Hole 사용 안함
            bool useHole = _currentContext.CurrentTutorial.hole_radius < 1f;

            // 레이아웃 대기 (다이얼로그 팝업 닫힘 후 UI 안정화 대기)
            if (!_sourceUVCached)
            {
                _layoutWaitTime += Time.deltaTime;
                if (_layoutWaitTime < LAYOUT_WAIT_DURATION)
                {
                    return;
                }

                // 레이아웃 안정화 후 UI 위치 캐싱
                if (_initialTargetUI != null)
                {
                    var targetRect = _initialTargetUI.GetComponent<RectTransform>();
                    Camera uiCam = _currentContext.TutorialCanvas?.worldCamera;
                    _sourceUV = TutorialCoordinateHelper.CalculateUIPositionUV(targetRect, canvasRect, uiCam);
                    _sourceUVCached = true;

                    // 초기 위치 캐싱 완료 후 드래그 오브젝트 활성화
                    if (_currentContext?.DragObj != null)
                    {
                        _currentContext.DragObj.SetActive(true);
                        TutorialCoordinateHelper.UpdateDragObjPosition(_currentContext.DragObj, canvasRect, _sourceUV);
                    }
                }

                return;
            }

            // A(UI슬롯 - 캐싱됨)와 B(타겟타일)의 UV 좌표
            Vector2 aUV = _sourceUV;
            Vector2 bUV = TutorialCoordinateHelper.CalculateWorldPositionUV(_destTilePosition, canvasRect);

            // A→B 이동 후 A로 순간이동, 반복
            _animTime += Time.deltaTime;
            float t = Mathf.Repeat(_animTime / ANIM_DURATION, 1f);
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector2 currentUV = Vector2.Lerp(aUV, bUV, t);

            // Hole 사용 시에만 마스크 업데이트
            if (useHole)
            {
                TutorialCoordinateHelper.UpdateHoleRadius(
                    _currentContext.MaskMaterial,
                    _currentContext.CurrentTutorial.hole_radius,
                    ref _holeRadiusAnimTime, ref _holeGrown, HOLE_GROW_DURATION);

                float aspect = (float)Screen.width / Screen.height;
                _currentContext.MaskMaterial.SetFloat(TutorialShaderHelper.AspectRatio, aspect);
                _currentContext.MaskMaterial.SetVector(TutorialShaderHelper.HoleCenter, new Vector4(currentUV.x, currentUV.y, 0, 0));
            }

            // DragObj는 항상 홀 위치를 따라 이동
            TutorialCoordinateHelper.UpdateDragObjPosition(_currentContext.DragObj, canvasRect, currentUV);

            // 화살표 위치 업데이트 (타일 위치 추적)
            float yOffset = _currentContext.CurrentTutorial?.arrow_yPos ?? 0;
            TutorialCoordinateHelper.UpdateArrowPosition(
                _currentContext.ArrowRectTransform, canvasRect, _destTilePosition, yOffset);
        }

        #region External API

        /// <summary>
        /// 드래그 시작 시 호출 (하위 호환용)
        /// </summary>
        /// <param name="dragObject">드래그 중인 오브젝트</param>
        public static void OnDragStart(GameObject dragObject)
        {
            // A→B 왕복 애니메이션은 드래그 중에도 계속 진행
        }

        /// <summary>
        /// 마스크 타겟 업데이트 - 보드에 캐릭터가 생성된 후 해당 캐릭터로 마스크 타겟 변경
        /// </summary>
        /// <param name="targetObject">새로운 마스크 타겟 (보드 위의 캐릭터)</param>
        public static void UpdateMaskTarget(GameObject targetObject)
        {
            // 이제 사용하지 않음 - 드래그 중에는 초기 위치 고정
        }

        /// <summary>
        /// 드래그 종료 시 호출 (하위 호환용 - 배치 취소 시)
        /// </summary>
        public static void OnDragCancel()
        {
            // A→B 왕복 애니메이션은 계속 진행 중
        }

        /// <summary>
        /// 배치 완료 시 외부에서 호출
        /// </summary>
        public static void NotifyPlacementCompleted()
        {
            if (IsActive)
            {
                _currentContext?.OnCompleted?.Invoke();
            }
        }

        /// <summary>
        /// 현재 튜토리얼 활성 상태 확인
        /// </summary>
        public static bool IsCharacterPlacementUIActive()
        {
            return IsActive;
        }

        /// <summary>
        /// 지정된 타일에 배치 가능한지 확인
        /// </summary>
        /// <param name="tileId">배치하려는 타일 ID</param>
        /// <returns>_targetTileId와 일치하면 true</returns>
        public static bool CanPlaceOnTile(int tileId)
        {
            if (!IsActive) return true; // 튜토리얼 비활성 시 제한 없음
            return tileId == _targetTileId;
        }

        /// <summary>
        /// 타겟 타일 ID 반환
        /// </summary>
        public static int GetTargetTileId()
        {
            return _targetTileId;
        }

        #endregion
    }
}
