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
        private static readonly int HoleRadius = Shader.PropertyToID("_HoleRadius");
        private static readonly int HoleCenter = Shader.PropertyToID("_HoleCenter");
        private static readonly int AspectRatio = Shader.PropertyToID("_AspectRatio");

        /// <summary>
        /// 드래그 시 하이라이트할 타일 ID (tutorial_action_key에서 파싱)
        /// </summary>
        private static int _targetTileId;

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

        public void OnShow(TutorialActionContext context)
        {
            _currentContext = context;
            _animTime = 0f;
            _holeRadiusAnimTime = 0f;
            _holeGrown = false;
            _positionsValid = false;
            _layoutWaitTime = 0f;
            _sourceUVCached = false;

            // 드래그 오브젝트 활성화
            if (context.DragObj != null)
            {
                context.DragObj.SetActive(true);
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

            // 초기 홀 크기 0으로 설정 (애니메이션으로 커짐)
            context.MaskMaterial.SetFloat(HoleRadius, 0f);

            // 초기 홀 위치를 타겟 타일로 설정 (이전 액션의 HoleCenter 잔류 방지)
            Vector2 destUV = CalculateWorldPositionUV(_destTilePosition);
            context.MaskMaterial.SetVector(HoleCenter, new Vector4(destUV.x, destUV.y, 0, 0));

            // 화살표 활성화 및 타일 위치로 이동
            context.ArrowRectTransform.gameObject.SetActive(true);
            UpdateArrowPosition();

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

            // 딤드 이미지는 raycastTarget 유지 (ICanvasRaycastFilter로 제어)

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
                context.MaskMaterial.SetFloat(HoleRadius, 0f);
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
            OnPlacementCompleted = null;
            _animTime = 0f;
            _holeRadiusAnimTime = 0f;
            _holeGrown = false;
            _positionsValid = false;
            _sourceUV = Vector2.zero;
            _layoutWaitTime = 0f;
            _sourceUVCached = false;

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

        /// <summary>
        /// 매 프레임 홀 위치 업데이트 (TutorialController.Update에서 호출)
        /// A(UI슬롯)→B(타겟타일) 왕복 애니메이션
        /// </summary>
        public static void UpdateHolePositions()
        {
            if (!IsActive || !_positionsValid || _currentContext == null)
            {
                return;
            }

            // 레이아웃 대기 (다이얼로그 팝업 닫힘 후 UI 안정화 대기)
            if (!_sourceUVCached)
            {
                _layoutWaitTime += Time.deltaTime;
                if (_layoutWaitTime < LAYOUT_WAIT_DURATION)
                {
                    return; // 대기 중에는 애니메이션 건너뛰기
                }

                // 레이아웃 안정화 후 UI 위치 캐싱
                if (_initialTargetUI != null)
                {
                    _sourceUV = CalculateUIPositionUV(_initialTargetUI);
                    _sourceUVCached = true;
                }
            }

            // 홀 크기 애니메이션 (Growing)
            UpdateHoleRadius();

            // A(UI슬롯 - 캐싱됨)와 B(타겟타일)의 UV 좌표
            Vector2 aUV = _sourceUV;
            Vector2 bUV = CalculateWorldPositionUV(_destTilePosition);

            // A→B 이동 후 A로 순간이동, 반복
            _animTime += Time.deltaTime;
            float t = Mathf.Repeat(_animTime / ANIM_DURATION, 1f);
            // SmoothStep으로 자연스러운 이동
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector2 currentUV = Vector2.Lerp(aUV, bUV, t);

            float aspect = (float)Screen.width / Screen.height;
            _currentContext.MaskMaterial.SetFloat(AspectRatio, aspect);
            _currentContext.MaskMaterial.SetVector(HoleCenter, new Vector4(currentUV.x, currentUV.y, 0, 0));

            // DragObj도 홀 위치를 따라 이동
            UpdateDragObjPosition(currentUV);

            // 화살표 위치 업데이트 (타일 위치 추적)
            UpdateArrowPosition();
        }

        /// <summary>
        /// DragObj를 홀 위치에 맞춰 이동 및 회전
        /// </summary>
        private static void UpdateDragObjPosition(Vector2 uvPosition)
        {
            if (_currentContext?.DragObj == null) return;

            var dragRect = _currentContext.DragObj.GetComponent<RectTransform>();
            if (dragRect == null) return;

            var canvasRect = _currentContext.CanvasRectTransform;
            if (canvasRect == null) return;

            // UV 좌표 → 캔버스 로컬 좌표로 변환
            float localX = (uvPosition.x - 0.5f) * canvasRect.rect.width;
            float localY = (uvPosition.y - 0.5f) * canvasRect.rect.height;

            dragRect.localPosition = new Vector3(localX, localY, 0f);
        }

        /// <summary>
        /// 화살표를 타일 위치에 맞춰 이동
        /// </summary>
        private static void UpdateArrowPosition()
        {
            if (_currentContext?.ArrowRectTransform == null || !_positionsValid) return;

            var canvasRect = _currentContext.CanvasRectTransform;
            if (canvasRect == null) return;

            // 타일의 3D 월드 좌표 → 캔버스 로컬 좌표로 변환
            Camera cam = MainCameraHolder.MainCamera;
            if (cam == null) return;

            Vector3 screenPosition = cam.WorldToScreenPoint(_destTilePosition);
            if (screenPosition.z < 0) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                null,
                out var localPoint);

            // arrow_yPos 만큼 Y축 위로 오프셋
            float yOffset = _currentContext.CurrentTutorial?.arrow_yPos ?? 0;
            _currentContext.ArrowRectTransform.localPosition = new Vector3(localPoint.x, localPoint.y + yOffset, 0f);
        }

        /// <summary>
        /// 홀 크기 애니메이션
        /// </summary>
        private static void UpdateHoleRadius()
        {
            if (_holeGrown)
            {
                return;
            }

            _holeRadiusAnimTime += Time.deltaTime;
            float t = Mathf.Clamp01(_holeRadiusAnimTime / HOLE_GROW_DURATION);
            t = Mathf.SmoothStep(0f, 1f, t);

            float targetRadius = _currentContext.CurrentTutorial.hole_radius;
            float currentRadius = Mathf.Lerp(0f, targetRadius, t);

            _currentContext.MaskMaterial.SetFloat(HoleRadius, currentRadius);

            if (t >= 1f)
            {
                _holeGrown = true;
            }
        }

        /// <summary>
        /// UI 오브젝트의 UV 좌표 계산
        /// </summary>
        private static Vector2 CalculateUIPositionUV(GameObject uiObject)
        {
            if (_currentContext == null || uiObject == null) return new Vector2(0.5f, 0.5f);

            var rectTransform = uiObject.GetComponent<RectTransform>();
            if (rectTransform == null) return new Vector2(0.5f, 0.5f);

            // UI 요소의 월드 좌표에서 중심점 계산
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) / 2f;

            // 월드 좌표 → 스크린 좌표
            Camera cam = _currentContext.TutorialCanvas?.worldCamera;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);

            // 스크린 좌표 → 캔버스 로컬 좌표
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _currentContext.CanvasRectTransform,
                screenPoint,
                cam,
                out var localPoint);

            // 정규화
            return new Vector2(
                (localPoint.x + (_currentContext.CanvasRectTransform.rect.width * 0.5f)) / _currentContext.CanvasRectTransform.rect.width,
                (localPoint.y + (_currentContext.CanvasRectTransform.rect.height * 0.5f)) / _currentContext.CanvasRectTransform.rect.height);
        }

        /// <summary>
        /// 3D 월드 좌표를 마스크 UV 좌표로 변환
        /// </summary>
        private static Vector2 CalculateWorldPositionUV(Vector3 worldPosition)
        {
            // MainCameraHolder에서 직접 카메라 가져오기 (context.MainCamera가 null일 수 있음)
            Camera cam = MainCameraHolder.MainCamera;
            if (cam == null) return new Vector2(0.5f, 0.5f);

            Vector3 screenPosition = cam.WorldToScreenPoint(worldPosition);
            if (screenPosition.z < 0) return new Vector2(0.5f, 0.5f);

            RectTransform canvasRect = _currentContext?.CanvasRectTransform;
            if (canvasRect == null) return new Vector2(0.5f, 0.5f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                null,
                out var localPoint);

            return new Vector2(
                (localPoint.x + (canvasRect.rect.width * 0.5f)) / canvasRect.rect.width,
                (localPoint.y + (canvasRect.rect.height * 0.5f)) / canvasRect.rect.height);
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
