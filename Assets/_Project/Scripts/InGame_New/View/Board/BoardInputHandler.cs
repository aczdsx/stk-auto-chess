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
        [SerializeField] private LayerMask _boardLayerMask = ~0;

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
        private GameObject _ghostIndicator;
        private int _ghostCol = -1;
        private int _ghostRow = -1;

        // 타일 하이라이트 (TileEffectManager handle 기반)
        private int _placementHandle;
        private int _rangeHandle;
        private int _highlightCol = -1;
        private int _highlightRow = -1;

        private bool _isEnabled = true;

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
            UpdateGhostDrag(screenPos);
        }

        /// <summary>UI 드래그 중 매 프레임 호출. 고스트 위치 갱신.</summary>
        public void UpdateGhostDrag(Vector3 screenPos)
        {
            if (!_isGhostActive) return;

            var grid = ScreenToGrid(screenPos);
            if (grid.HasValue)
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

            var grid = ScreenToGrid(screenPos);
            if (grid.HasValue)
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

            // 타겟 셀 하이라이트
            var grid = ScreenToGrid(screenPos);
            if (grid.HasValue)
                ShowHighlight(grid.Value.col, grid.Value.row);
            else
                HideHighlight();
        }

        private void EndBoardDrag(Vector2 screenPos)
        {
            _isDraggingBoardUnit = false;
            HideHighlight();

            // UI 영역으로 드롭 → 회수
            if (_isPointInUI != null && _isPointInUI(screenPos))
            {
                var cmd = GameCommand.WithdrawUnit(0, _dragEntityId);
                _viewBridge.SendCommand(cmd);
            }
            else
            {
                // 보드 위 다른 셀로 이동
                var grid = ScreenToGrid(screenPos);
                if (grid.HasValue && (grid.Value.col != _dragStartCol || grid.Value.row != _dragStartRow))
                {
                    var cmd = GameCommand.MoveUnit(0, _dragEntityId, (byte)grid.Value.col, (byte)grid.Value.row);
                    _viewBridge.SendCommand(cmd);
                }
                else
                {
                    // 같은 위치 또는 보드 밖 → 원위치 복귀
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

            if (_camera == null) return false;

            var ray = _camera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, 100f, _boardLayerMask))
            {
                unitView = hit.collider.GetComponentInParent<UnitView>();
                if (unitView != null && !unitView.IsCombatUnit)
                {
                    entityId = unitView.EntityId;
                    return true;
                }
            }

            return false;
        }

        // ── 좌표 변환 ──

        /// <summary>스크린 좌표 → 보드 그리드 좌표</summary>
        public (int boardIndex, int col, int row)? ScreenToGrid(Vector2 screenPos)
        {
            var worldPos = ScreenToWorldOnGround(screenPos);
            if (!worldPos.HasValue) return null;

            if (BoardWorldHelper.WorldToBoard(worldPos.Value, out int board, out int col, out int row))
                return (board, col, row);

            return null;
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
