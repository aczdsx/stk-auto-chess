using CookApps.BattleSystem;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 오브젝트 이동 튜토리얼 액션.
    /// 타일 A에서 타일 B로 오브젝트를 드래그하여 이동하도록 유도합니다.
    /// 첫 번째 마스크 홀이 A→B 이동 후 A로 순간이동하며 반복합니다.
    ///
    /// tutorial_action_key 형식: "SourceTileId->DestTileId"
    /// 예: "11->7" (타일 ID)
    ///
    /// 이동이 완료되면 자동으로 다음 튜토리얼로 진행됩니다.
    /// </summary>
    public class TutorialActionMoveObject : ITutorialActionStrategy
    {
        /// <summary>
        /// 현재 튜토리얼 컨텍스트 참조 (static - NotifyMoveCompleted에서 사용)
        /// </summary>
        private static TutorialActionContext _staticContext;

        /// <summary>
        /// 현재 튜토리얼에서 이동해야 할 Source 타일 ID
        /// </summary>
        public static int SourceTileId { get; private set; }

        /// <summary>
        /// 현재 튜토리얼에서 이동 목적지 타일 ID
        /// </summary>
        public static int DestTileId { get; private set; }

        /// <summary>
        /// 현재 오브젝트 이동 튜토리얼 진행 중인지 여부
        /// </summary>
        public static bool IsActive { get; private set; }

        // 타일 위치 캐싱
        private Vector3 _sourcePosition;
        private Vector3 _destPosition;
        private bool _positionsValid;
        private InGameTileView _destTileView;

        // A→B 반복 애니메이션
        private float _animTime;
        private const float ANIM_DURATION = 1.2f; // A→B 편도 시간

        // 홀 크기 애니메이션
        private float _holeRadiusAnimTime;
        private const float HOLE_GROW_DURATION = 0.5f;
        private bool _holeGrown;

        public void OnShow(TutorialActionContext context)
        {
            _staticContext = context;
            _animTime = 0f;
            _holeRadiusAnimTime = 0f;
            _holeGrown = false;
            _positionsValid = false;

            // 화살표 활성화 (도착지 타일 위치에 표시)
            context.ArrowRectTransform.gameObject.SetActive(true);

            // 드래그 오브젝트 활성화
            if (context.DragObj != null)
            {
                context.DragObj.SetActive(true);
            }

            // TutorialController의 마스크 업데이트 건너뛰기 (직접 처리)
            context.SkipMaskUpdate = true;

            // tutorial_action_key 파싱 (형식: "11->7")
            if (!ParseActionKey(context.CurrentTutorial.tutorial_action_key))
            {
                Debug.LogWarning($"[TutorialActionMoveObject] action_key 파싱 실패: {context.CurrentTutorial.tutorial_action_key}");
                context.SkipMaskUpdate = false;
                context.NextObj.SetActive(true);
                return;
            }

            // 타일 위치 가져오기
            if (!GetTilePositions())
            {
                Debug.LogWarning($"[TutorialActionMoveObject] 타일 위치를 찾을 수 없음. Source: {SourceTileId}, Dest: {DestTileId}");
                context.SkipMaskUpdate = false;
                context.NextObj.SetActive(true);
                return;
            }

            // 초기 홀 크기 0으로 설정 (애니메이션으로 커짐)
            context.MaskMaterial.SetFloat(TutorialShaderHelper.HoleRadius, 0f);

            // 딤드 이미지는 raycastTarget 유지 (ICanvasRaycastFilter가 구멍 영역만 통과시킴)
            // raycastTarget = false로 하면 모든 UI 터치가 통과되므로 제거

            // 3D 터치 허용 (오브젝트 드래그 가능하도록)
            TutorialTouchBlocker.Allow3DTouch = true;

            IsActive = true;
        }

        public void OnNext(TutorialActionContext context)
        {
            // 이동 튜토리얼에서는 "다음" 버튼을 표시하지 않음
            context.NextObj.SetActive(false);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            // 딤드 클릭으로 진행 불가 - 반드시 오브젝트를 이동해야 함
            // 위치가 유효하지 않으면 딤드 클릭으로 진행 가능 (fallback)
            return !_positionsValid;
        }

        public void OnClear(TutorialActionContext context)
        {
            // 홀 숨기기
            if (context.MaskMaterial != null)
            {
                context.MaskMaterial.SetFloat(TutorialShaderHelper.HoleRadius, 0f);
            }

            // 드래그 오브젝트 비활성화
            if (context.DragObj != null)
            {
                context.DragObj.SetActive(false);
            }

            // 딤드 이미지는 raycastTarget 유지 (ICanvasRaycastFilter로 제어)

            // 3D 터치 허용 해제
            TutorialTouchBlocker.Allow3DTouch = false;

            // 마스크 업데이트 복원
            context.SkipMaskUpdate = false;

            // 화살표 비활성화
            context.ArrowRectTransform.gameObject.SetActive(false);

            // 도착지 타일 NavigateObj 비활성화
            if (_destTileView != null)
            {
                _destTileView.SetNavigateObj(false);
            }

            // 상태 초기화
            IsActive = false;
            SourceTileId = 0;
            DestTileId = 0;
            _staticContext = null;
            _animTime = 0f;
            _holeRadiusAnimTime = 0f;
            _holeGrown = false;
            _positionsValid = false;
            _destTileView = null;
            context.TargetUnmaskObj = null;
        }

        private const string SEPARATOR = "->";

        /// <summary>
        /// tutorial_action_key 파싱 (형식: "11->7")
        /// </summary>
        /// <returns>파싱 성공 여부</returns>
        private bool ParseActionKey(string actionKey)
        {
            SourceTileId = 0;
            DestTileId = 0;

            if (string.IsNullOrEmpty(actionKey))
            {
                Debug.LogWarning("[TutorialActionMoveObject] action_key가 비어있습니다.");
                return false;
            }

            // '->'로 분리
            int separatorIndex = actionKey.IndexOf(SEPARATOR);
            if (separatorIndex <= 0 || separatorIndex >= actionKey.Length - SEPARATOR.Length)
            {
                Debug.LogWarning($"[TutorialActionMoveObject] action_key 형식 오류: {actionKey}. 'SourceTileId->DestTileId' 형식이어야 합니다.");
                return false;
            }

            string sourceStr = actionKey.Substring(0, separatorIndex).Trim();
            string destStr = actionKey.Substring(separatorIndex + SEPARATOR.Length).Trim();

            if (!int.TryParse(sourceStr, out int sourceTileId))
            {
                Debug.LogWarning($"[TutorialActionMoveObject] Source 타일 ID 파싱 실패: {sourceStr}");
                return false;
            }

            if (!int.TryParse(destStr, out int destTileId))
            {
                Debug.LogWarning($"[TutorialActionMoveObject] Dest 타일 ID 파싱 실패: {destStr}");
                return false;
            }

            SourceTileId = sourceTileId;
            DestTileId = destTileId;

            Debug.LogColor($"[TutorialActionMoveObject] Source Tile: {SourceTileId}, Dest Tile: {DestTileId}", "green");
            return true;
        }

        /// <summary>
        /// InGameGrid에서 타일 위치 가져오기
        /// </summary>
        /// <returns>위치 획득 성공 여부</returns>
        private bool GetTilePositions()
        {
            var grid = InGameObjectManager.Instance?.InGameGrid;
            if (grid == null)
            {
                Debug.LogWarning("[TutorialActionMoveObject] InGameGrid를 찾을 수 없음");
                return false;
            }

            var sourceTile = grid.GetTile(SourceTileId);
            var destTile = grid.GetTile(DestTileId);

            if (sourceTile?.View == null)
            {
                Debug.LogWarning($"[TutorialActionMoveObject] Source 타일을 찾을 수 없음: {SourceTileId}");
                return false;
            }

            if (destTile?.View == null)
            {
                Debug.LogWarning($"[TutorialActionMoveObject] Dest 타일을 찾을 수 없음: {DestTileId}");
                return false;
            }

            _sourcePosition = sourceTile.View.Position;
            _destPosition = destTile.View.Position;
            _destTileView = destTile.View;
            _positionsValid = true;

            // 도착지 타일 NavigateObj 활성화
            _destTileView.SetNavigateObj(true);

            return true;
        }

        public void OnUpdate(TutorialActionContext context)
        {
            UpdateHolePositions();
        }

        /// <summary>
        /// 매 프레임 홀 위치 업데이트
        /// 첫 번째 홀이 A→B 이동 후 A로 순간이동, 반복
        /// </summary>
        private void UpdateHolePositions()
        {
            if (!IsActive || !_positionsValid || _staticContext == null)
            {
                return;
            }

            // 홀 크기 애니메이션 (Growing)
            TutorialCoordinateHelper.UpdateHoleRadius(
                _staticContext.MaskMaterial,
                _staticContext.CurrentTutorial.hole_radius,
                ref _holeRadiusAnimTime, ref _holeGrown, HOLE_GROW_DURATION);

            // A(Source)와 B(Dest)의 UV 좌표
            var canvasRect = _staticContext.CanvasRectTransform;
            Vector2 sourceUV = TutorialCoordinateHelper.CalculateWorldPositionUV(_sourcePosition, canvasRect);
            Vector2 destUV = TutorialCoordinateHelper.CalculateWorldPositionUV(_destPosition, canvasRect);

            // 첫 번째 홀: A→B 이동 후 A로 순간이동, 반복
            _animTime += Time.deltaTime;
            float t = Mathf.Repeat(_animTime / ANIM_DURATION, 1f);
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector2 currentUV = Vector2.Lerp(sourceUV, destUV, t);

            float aspectRatio = (float)Screen.width / Screen.height;
            _staticContext.MaskMaterial.SetFloat(TutorialShaderHelper.AspectRatio, aspectRatio);
            _staticContext.MaskMaterial.SetVector(TutorialShaderHelper.HoleCenter, new Vector4(currentUV.x, currentUV.y, 0, 0));

            // DragObj도 홀 위치를 따라 이동
            TutorialCoordinateHelper.UpdateDragObjPosition(_staticContext.DragObj, canvasRect, currentUV);

            // 화살표 위치 업데이트 (도착지 타일 위치 추적)
            float yOffset = _staticContext.CurrentTutorial?.arrow_yPos ?? 0;
            TutorialCoordinateHelper.UpdateArrowPosition(
                _staticContext.ArrowRectTransform, canvasRect, _destPosition, yOffset);
        }

        /// <summary>
        /// 오브젝트 이동 완료 시 외부에서 호출
        /// </summary>
        public static void NotifyMoveCompleted()
        {
            if (IsActive)
            {
                _staticContext?.OnCompleted?.Invoke();
            }
        }

        /// <summary>
        /// 지정된 Source 타일에서만 선택 가능한지 확인
        /// </summary>
        /// <param name="tileId">선택하려는 타일 ID</param>
        /// <returns>SourceTileId와 일치하면 true</returns>
        public static bool CanSelectFromTile(int tileId)
        {
            if (!IsActive || SourceTileId == 0)
            {
                return true;
            }

            return tileId == SourceTileId;
        }

        /// <summary>
        /// 지정된 Dest 타일로만 이동 가능한지 확인
        /// </summary>
        /// <param name="tileId">이동하려는 타일 ID</param>
        /// <returns>DestTileId와 일치하면 true</returns>
        public static bool CanMoveToTile(int tileId)
        {
            if (!IsActive || DestTileId == 0)
            {
                return true;
            }

            return tileId == DestTileId;
        }
    }
}
