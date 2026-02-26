using UnityEngine;
using UnityEngine.EventSystems;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 보드 터치/드래그 입력 처리.
    /// - UI에서 보드로 드래그 시 고스트 프리뷰 관리
    /// - 보드 위 유닛 드래그 (재배치/회수)
    /// - 스크린 좌표 → 그리드 좌표 변환
    /// </summary>
    public class BoardInputHandler : MonoBehaviour
    {
        private Camera _camera;
        private AutoChessViewBridge _viewBridge;
        private UnitViewManager _unitViewManager;
        private TileEffectManager _tileEffectManager;

        // UI 영역 판별용 콜백
        private System.Func<Vector2, bool> _isPointInUI;

        // 보드 유닛 드래그 상태
        private bool _isDraggingBoardUnit;
        private int _dragEntityId = UnitData.InvalidId;
        private Vector3 _dragOriginalPos;
        private UnitView _dragUnitView;
        private int _dragStartCol;
        private int _dragStartRow;

        // 고스트 드래그 (UI → 보드)
        private bool _isGhostActive;
        private int _ghostEntityId = UnitData.InvalidId;
        private UnitView _ghostView;
        private int _ghostCol = -1;
        private int _ghostRow = -1;

        // 타일 하이라이트 (TileEffectManager handle 기반)
        private int _placementHandle;
        private int _rangeHandle;
        private int _highlightCol = -1;
        private int _highlightRow = -1;

        private bool _isEnabled = true;

        // 타일 스냅 거리 제한
        private float _maxSnapDistSq;

        // ── 초기화 ──

        public void Initialize(
            Camera camera,
            AutoChessViewBridge viewBridge,
            UnitViewManager unitViewManager,
            TileEffectManager tileEffectManager,
            System.Func<Vector2, bool> isPointInUI)
        {
            _camera = camera;
            _viewBridge = viewBridge;
            _unitViewManager = unitViewManager;
            _tileEffectManager = tileEffectManager;
            _isPointInUI = isPointInUI;

            ComputeSnapThreshold();
        }

        private void ComputeSnapThreshold()
        {
            var p00 = BoardWorldHelper.BoardGridToWorld(0, 0, 0);
            var p10 = BoardWorldHelper.BoardGridToWorld(0, 1, 0);
            var p01 = BoardWorldHelper.BoardGridToWorld(0, 0, 1);

            float colDist = Vector3.Distance(p00, p10);
            float rowDist = Vector3.Distance(p00, p01);
            float halfDiag = Mathf.Sqrt(colDist * colDist + rowDist * rowDist) * 0.5f;
            _maxSnapDistSq = halfDiag * halfDiag;
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (!enabled)
            {
                CancelDrag();
                HideHighlight();
            }
        }

        // ── UI → 보드 고스트 드래그 API ──

        /// <summary>UI에서 보드 영역 진입 시 호출. 고스트 프리뷰 시작.</summary>
        public void StartGhostDrag(int entityId, Vector3 screenPos)
        {
            _isGhostActive = true;
            _ghostEntityId = entityId;

            // 홀로그램 고스트 생성
            var world = _viewBridge.GetWorld();
            if (world != null)
            {
                _ghostView = _unitViewManager.CreateGhostView(entityId, world);
                _ghostView.SetHologram(true);
            }

            UpdateGhostDrag(screenPos);
        }

        /// <summary>UI 드래그 중 매 프레임 호출. 고스트 위치 갱신.</summary>
        public void UpdateGhostDrag(Vector3 screenPos)
        {
            if (!_isGhostActive) return;

            // 고스트 뷰는 항상 손가락 위치를 따라다님
            if (_ghostView != null)
            {
                var worldPos = ScreenToWorldOnGround(screenPos);
                if (worldPos.HasValue)
                    _ghostView.SetPositionImmediate(worldPos.Value);
            }

            // 타일 하이라이트는 그리드 스냅 (빈 타일만)
            var grid = ScreenToGrid(screenPos);
            if (grid.HasValue && !IsTileOccupied(grid.Value.col, grid.Value.row))
            {
                _ghostCol = grid.Value.col;
                _ghostRow = grid.Value.row;
                ShowHighlight(_ghostCol, _ghostRow);
            }
            else
            {
                _ghostCol = -1;
                _ghostRow = -1;
                HideHighlight();
            }
        }

        /// <summary>UI 드래그 종료. 유효 셀이면 (col, row) 반환.</summary>
        public (int col, int row)? EndGhostDrag(Vector3 screenPos)
        {
            if (!_isGhostActive)
                return null;

            _isGhostActive = false;
            _ghostEntityId = UnitData.InvalidId;
            HideHighlight();
            ReleaseGhostView();

            var grid = ScreenToGrid(screenPos);
            if (grid.HasValue && !IsTileOccupied(grid.Value.col, grid.Value.row))
                return (grid.Value.col, grid.Value.row);

            return null;
        }

        /// <summary>UI 드래그 취소.</summary>
        public void CancelGhostDrag()
        {
            _isGhostActive = false;
            _ghostEntityId = UnitData.InvalidId;
            _ghostCol = -1;
            _ghostRow = -1;
            HideHighlight();
            ReleaseGhostView();
        }

        private void ReleaseGhostView()
        {
            _ghostView = null;
            _unitViewManager.ReleaseGhostView();
        }

        // ── 보드 유닛 터치/드래그 (Update에서 처리) ──

        private void Update()
        {
            if (!_isEnabled) return;
            if (_isGhostActive) return; // UI 드래그 중이면 보드 입력 무시

            HandleBoardTouch();
        }

        private void HandleBoardTouch()
        {
            // 터치 시작
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 screenPos = Input.mousePosition;

                // UI 위의 터치는 무시
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                if (TryPickUnit(screenPos, out int entityId, out var unitView))
                {
                    StartBoardDrag(entityId, unitView);
                }
            }
            // 드래그 중
            else if (Input.GetMouseButton(0) && _isDraggingBoardUnit)
            {
                UpdateBoardDrag(Input.mousePosition);
            }
            // 드래그 종료
            else if (Input.GetMouseButtonUp(0) && _isDraggingBoardUnit)
            {
                EndBoardDrag(Input.mousePosition);
            }
        }

        private void StartBoardDrag(int entityId, UnitView unitView)
        {
            _isDraggingBoardUnit = true;
            _dragEntityId = entityId;
            _dragUnitView = unitView;
            _dragOriginalPos = unitView.transform.position;

            // 홀로그램 적용
            _dragUnitView.SetHologram(true);

            // 시작 그리드 좌표 기록
            if (BoardWorldHelper.WorldToBoard(_dragOriginalPos, out _, out int col, out int row))
            {
                _dragStartCol = col;
                _dragStartRow = row;
            }
        }

        private void UpdateBoardDrag(Vector2 screenPos)
        {
            // 유닛을 손가락 위치로 이동
            var worldPos = ScreenToWorldOnGround(screenPos);
            if (worldPos.HasValue && _dragUnitView != null)
            {
                _dragUnitView.SetPositionImmediate(worldPos.Value);
            }

            // 타겟 셀 하이라이트 (자기 자신은 제외하고 점유 검사)
            var grid = ScreenToGrid(screenPos);
            if (grid.HasValue && !IsTileOccupied(grid.Value.col, grid.Value.row, _dragEntityId))
                ShowHighlight(grid.Value.col, grid.Value.row);
            else
                HideHighlight();
        }

        private void EndBoardDrag(Vector2 screenPos)
        {
            _isDraggingBoardUnit = false;
            HideHighlight();
            _dragUnitView?.SetHologram(false);

            // UI 영역으로 드롭 → 회수
            if (_isPointInUI != null && _isPointInUI(screenPos))
            {
                var cmd = GameCommand.WithdrawUnit(0, _dragEntityId);
                _viewBridge.SendCommand(cmd);
            }
            else
            {
                // 보드 위 다른 셀로 이동 (빈 타일만, 자기 위치 제외)
                var grid = ScreenToGrid(screenPos);
                if (grid.HasValue
                    && !IsTileOccupied(grid.Value.col, grid.Value.row, _dragEntityId)
                    && (grid.Value.col != _dragStartCol || grid.Value.row != _dragStartRow))
                {
                    var cmd = GameCommand.MoveUnit(0, _dragEntityId, (byte)grid.Value.col, (byte)grid.Value.row);
                    _viewBridge.SendCommand(cmd);
                }
                else
                {
                    // 같은 위치, 점유된 타일, 또는 보드 밖 → 원위치 복귀
                    _dragUnitView?.SetPositionImmediate(_dragOriginalPos);
                }
            }

            _dragEntityId = UnitData.InvalidId;
            _dragUnitView = null;
        }

        private void CancelDrag()
        {
            if (_isDraggingBoardUnit && _dragUnitView != null)
            {
                _dragUnitView.SetHologram(false);
                _dragUnitView.SetPositionImmediate(_dragOriginalPos);
            }
            _isDraggingBoardUnit = false;
            _dragEntityId = UnitData.InvalidId;
            _dragUnitView = null;

            CancelGhostDrag();
        }

        // ── 유닛 선택 ──

        private bool TryPickUnit(Vector2 screenPos, out int entityId, out UnitView unitView)
        {
            entityId = UnitData.InvalidId;
            unitView = null;

            // 타일 그리드 좌표로 변환 → BoardSlots에서 유닛 조회
            var grid = ScreenToGrid(screenPos);
            if (!grid.HasValue) return false;

            var world = _viewBridge.GetWorld();
            if (world == null) return false;

            int index = BoardHelper.ToIndex(grid.Value.col, grid.Value.row);
            int occupant = world.BoardSlots[0][index];
            if (occupant == UnitData.InvalidId) return false;

            unitView = _unitViewManager.FindBoardView(occupant);
            if (unitView == null) return false;

            entityId = occupant;
            return true;
        }

        // ── 좌표 변환 ──

        /// <summary>스크린 좌표 → 보드 그리드 좌표 (타일 거리 검증 포함)</summary>
        public (int boardIndex, int col, int row)? ScreenToGrid(Vector2 screenPos)
        {
            var worldPos = ScreenToWorldOnGround(screenPos);
            if (!worldPos.HasValue) return null;

            if (!BoardWorldHelper.WorldToBoard(worldPos.Value, out int board, out int col, out int row))
                return null;

            // 타일과의 거리 검증 (적 영역 등 먼 타일 스냅 방지)
            var tilePos = BoardWorldHelper.BoardGridToWorld(board, col, row);
            float dx = worldPos.Value.x - tilePos.x;
            float dz = worldPos.Value.z - tilePos.z;
            if (dx * dx + dz * dz > _maxSnapDistSq)
                return null;

            return (board, col, row);
        }

        /// <summary>타일에 다른 유닛이 배치되어 있는지 검사</summary>
        private bool IsTileOccupied(int col, int row, int ignoreEntityId = UnitData.InvalidId)
        {
            var world = _viewBridge.GetWorld();
            if (world == null) return false;

            int index = BoardHelper.ToIndex(col, row);
            if (index < 0 || index >= world.BoardSize) return false;

            int occupant = world.BoardSlots[0][index];
            return occupant != UnitData.InvalidId && occupant != ignoreEntityId;
        }

        private Vector3? ScreenToWorldOnGround(Vector2 screenPos)
        {
            if (_camera == null) return null;

            var ray = _camera.ScreenPointToRay(screenPos);

            // Ground plane (y=0) intersection
            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
                return ray.GetPoint(distance);

            return null;
        }

        // ── 타일 하이라이트 ──

        private void ShowHighlight(int col, int row)
        {
            if (_highlightCol == col && _highlightRow == row && _placementHandle != 0)
                return;

            HideHighlight();

            _highlightCol = col;
            _highlightRow = row;

            _placementHandle = _tileEffectManager.Show(TileEffectType.Placement, col, row);
            // TODO: 드래그 유닛의 AttackRange를 가져와서 공격범위 표시
            // _rangeHandle = _tileEffectManager.ShowRange(TileEffectType.AttackRange, col, row, attackRange);
        }

        private void HideHighlight()
        {
            _highlightCol = -1;
            _highlightRow = -1;

            if (_placementHandle != 0)
            {
                _tileEffectManager.Hide(_placementHandle);
                _placementHandle = 0;
            }
            if (_rangeHandle != 0)
            {
                _tileEffectManager.Hide(_rangeHandle);
                _rangeHandle = 0;
            }
        }
    }
}
