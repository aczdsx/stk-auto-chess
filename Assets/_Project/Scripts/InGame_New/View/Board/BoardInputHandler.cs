using System.Collections.Generic;
using CookApps.AutoBattler;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 보드 터치/드래그 입력 처리.
    /// - UI에서 보드로 드래그 시 고스트 프리뷰 관리
    /// - 보드 위 유닛 드래그 (재배치/회수)
    /// - 스크린 좌표 → 그리드 좌표 변환
    /// - IBoardInputBlocker를 통한 세밀한 입력 차단 (튜토리얼 등)
    /// </summary>
    public class BoardInputHandler : MonoBehaviour
    {
        private Camera _camera;
        private AutoChessViewBridge _viewBridge;
        private UnitViewManager _unitViewManager;
        private TileEffectManager _tileEffectManager;
        private TargetLineManager _targetLineManager;

        // UI 영역 판별용 콜백
        private System.Func<Vector2, bool> _isPointInUI;

        // 입력 차단 (SelectableBlockerManager 패턴)
        private readonly List<IBoardInputBlocker> _inputBlockers = new();

        // 보드 유닛 드래그 상태
        private bool _isDraggingBoardUnit;
        private int _dragEntityId = UnitData.InvalidId;
        private Vector3 _dragOriginalPos;
        private UnitView _dragUnitView;
        private int _dragStartCol;
        private int _dragStartRow;

        // 보드 오브젝트 드래그 상태
        private bool _isDraggingObject;
        private IBoardDraggableObject _dragObject;
        private Vector3 _dragObjectOriginalPos;

        // 보드 오브젝트 조회 콜백
        private System.Func<int, int, IBoardDraggableObject> _findBoardObject; // (col, row) → object

        // 드래그 threshold (탭과 드래그 구분)
        private bool _isPendingDrag;
        private bool _isPendingObject; // true면 오브젝트 드래그 대기
        private int _pendingEntityId = UnitData.InvalidId;
        private UnitView _pendingUnitView;
        private Vector2 _pendingScreenPos;

        // 고스트 드래그 (UI → 보드)
        private bool _isGhostActive;
        private int _ghostEntityId = UnitData.InvalidId;
        private UnitView _ghostView;
        private int _ghostCol = -1;
        private int _ghostRow = -1;

        // 타일 하이라이트 (TileEffectManager handle 기반)
        private int _placementHandle;
        private int _rangeHandle;
        private int _canPlacementHandle;
        private List<(int col, int row)> _canPlacementTiles;
        private int _highlightCol = -1;
        private int _highlightRow = -1;

        private bool _isEnabled = true;

        // 유닛 선택(탭) 상태 — 타일 이펙트만 로컬 관리, 팝업은 InGameCharacterPopupHelper
        private int _selectEffectHandle;

        // 타일 스냅 거리 제한
        private float _maxSnapDistSq;

        // ── 초기화 ──

        public void Initialize(
            Camera camera,
            AutoChessViewBridge viewBridge,
            UnitViewManager unitViewManager,
            TileEffectManager tileEffectManager,
            TargetLineManager targetLineManager,
            System.Func<Vector2, bool> isPointInUI)
        {
            _camera = camera;
            _viewBridge = viewBridge;
            _unitViewManager = unitViewManager;
            _tileEffectManager = tileEffectManager;
            _targetLineManager = targetLineManager;
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

        /// <summary>보드 오브젝트 조회 콜백 설정</summary>
        public void SetBoardObjectFinder(System.Func<int, int, IBoardDraggableObject> finder)
        {
            _findBoardObject = finder;
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (!enabled)
            {
                CancelDrag();
                HideHighlight();
                ApplyUnitTapSelection(UnitData.InvalidId);
            }
        }

        // ── 입력 차단 (IBoardInputBlocker) ──

        public void AddInputBlocker(IBoardInputBlocker blocker)
        {
            if (_inputBlockers.Contains(blocker)) return;
            _inputBlockers.Add(blocker);
            _inputBlockers.Sort((x, y) => x.GetPriority() - y.GetPriority());
        }

        public void RemoveInputBlocker(IBoardInputBlocker blocker)
        {
            _inputBlockers.Remove(blocker);
        }

        /// <summary>
        /// 특정 액션+타일의 입력이 허용되는지 확인.
        /// 블로커가 하나라도 false를 반환하면 차단.
        /// BenchUnitSlot 등 외부에서도 호출 가능.
        /// </summary>
        public bool IsInputAllowed(BoardInputAction action, int col = -1, int row = -1)
        {
            for (int i = 0; i < _inputBlockers.Count; i++)
            {
                if (!_inputBlockers[i].IsAllowInput(action, col, row))
                    return false;
            }
            return true;
        }

        // ── UI → 보드 고스트 드래그 API ──

        /// <summary>UI에서 보드 영역 진입 시 호출. 고스트 프리뷰 시작. 차단 시 false 반환.</summary>
        public bool StartGhostDrag(int entityId, Vector3 screenPos)
        {
            if (!IsInputAllowed(BoardInputAction.Place))
                return false;

            _isGhostActive = true;
            _ghostEntityId = entityId;

            // 홀로그램 고스트 생성
            var world = _viewBridge.GetWorld();
            if (world != null)
            {
                _ghostView = _unitViewManager.CreateGhostView(entityId, world);
                _ghostView.SetHologram(true);
            }

            ShowCanPlacement(entityId);
            UpdateGhostDrag(screenPos);
            return true;
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

            // 타일 하이라이트는 그리드 스냅 (점유 타일도 허용 — 스왑 대상)
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
            HideCanPlacement();
            ReleaseGhostView();

            var grid = ScreenToGrid(screenPos);
            if (grid.HasValue
                && IsInputAllowed(BoardInputAction.Place, grid.Value.col, grid.Value.row))
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
            HideCanPlacement();
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
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                if (TryPickUnit(screenPos, out int entityId, out var unitView))
                {
                    var pickGrid = ScreenToGrid(screenPos);
                    if (pickGrid.HasValue
                        && !IsInputAllowed(BoardInputAction.Select, pickGrid.Value.col, pickGrid.Value.row))
                        return;

                    // threshold 대기 (즉시 드래그 시작하지 않음)
                    _isPendingDrag = true;
                    _isPendingObject = false;
                    _pendingEntityId = entityId;
                    _pendingUnitView = unitView;
                    _pendingScreenPos = screenPos;
                }
                else if (TryPickBoardObject(screenPos, out var boardObj))
                {
                    // 보드 오브젝트 드래그 대기
                    _isPendingDrag = true;
                    _isPendingObject = true;
                    _dragObject = boardObj;
                    _pendingScreenPos = screenPos;
                }
                else
                {
                    // 빈 곳 터치 → 팝업 닫기
                    ApplyUnitTapSelection(UnitData.InvalidId);
                }
            }
            // 드래그 threshold 체크
            else if (Input.GetMouseButton(0) && _isPendingDrag)
            {
                Vector2 screenPos = Input.mousePosition;
                float threshold = EventSystem.current.pixelDragThreshold;

                if ((screenPos - _pendingScreenPos).sqrMagnitude > threshold * threshold)
                {
                    _isPendingDrag = false;
                    if (_isPendingObject)
                    {
                        StartObjectDrag(_dragObject);
                        UpdateObjectDrag(screenPos);
                    }
                    else
                    {
                        StartBoardDrag(_pendingEntityId, _pendingUnitView);
                        UpdateBoardDrag(screenPos);
                    }
                }
            }
            // 드래그 중
            else if (Input.GetMouseButton(0) && (_isDraggingBoardUnit || _isDraggingObject))
            {
                if (_isDraggingObject)
                    UpdateObjectDrag(Input.mousePosition);
                else
                    UpdateBoardDrag(Input.mousePosition);
            }
            // 터치 종료
            else if (Input.GetMouseButtonUp(0))
            {
                if (_isPendingDrag)
                {
                    // threshold 미달 → 탭으로 처리
                    int tappedId = _pendingEntityId;
                    var tapGrid = ScreenToGrid(_pendingScreenPos);

                    _isPendingDrag = false;
                    _pendingEntityId = UnitData.InvalidId;
                    _pendingUnitView = null;

                    if (tapGrid.HasValue)
                    {
                        if (tappedId == InGameCharacterPopupHelper.SelectedEntityId)
                            ApplyUnitTapSelection(UnitData.InvalidId);
                        else
                            ApplyUnitTapSelection(tappedId, tapGrid.Value.col, tapGrid.Value.row);
                    }
                }
                else if (_isDraggingObject)
                {
                    EndObjectDrag(Input.mousePosition);
                }
                else if (_isDraggingBoardUnit)
                {
                    EndBoardDrag(Input.mousePosition);
                }
            }
        }

        private void StartBoardDrag(int entityId, UnitView unitView)
        {
            InGameCharacterPopupHelper.Close();
            _isDraggingBoardUnit = true;
            _dragEntityId = entityId;
            _dragUnitView = unitView;
            _dragOriginalPos = unitView.transform.position;

            // 드래그 중 해당 캐릭터 관련 라인만 표시
            _targetLineManager?.SetFocusedUnit(entityId);

            // 홀로그램 적용
            _dragUnitView.SetHologram(true);

            // 시작 그리드 좌표 기록
            if (BoardWorldHelper.WorldToBoard(_dragOriginalPos, out _, out int col, out int row))
            {
                _dragStartCol = col;
                _dragStartRow = row;
            }

            ShowCanPlacement(entityId);
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
            int prevCol = _highlightCol;
            int prevRow = _highlightRow;

            if (grid.HasValue)
                ShowHighlight(grid.Value.col, grid.Value.row);
            else
                HideHighlight();

            // 그리드 셀 변경 시 타겟 라인 즉시 재계산
            if (_highlightCol != prevCol || _highlightRow != prevRow)
                _targetLineManager?.RefreshFocusedLines();
        }

        private void EndBoardDrag(Vector2 screenPos)
        {
            _isDraggingBoardUnit = false;
            HideHighlight();
            HideCanPlacement();
            _dragUnitView?.SetHologram(false);

            // UI 영역으로 드롭 → 회수
            if (_isPointInUI != null && _isPointInUI(screenPos))
            {
                if (IsInputAllowed(BoardInputAction.Withdraw))
                {
                    var cmd = GameCommand.WithdrawUnit(0, _dragEntityId);
                    _viewBridge.SendCommand(cmd);
                }
                else
                {
                    _dragUnitView?.SetPositionImmediate(_dragOriginalPos);
                }
            }
            else
            {
                // 보드 위 다른 셀로 이동 (점유 타일은 시뮬레이션이 스왑 처리)
                var grid = ScreenToGrid(screenPos);
                if (grid.HasValue
                    && (grid.Value.col != _dragStartCol || grid.Value.row != _dragStartRow)
                    && IsInputAllowed(BoardInputAction.Move, grid.Value.col, grid.Value.row))
                {
                    var cmd = GameCommand.MoveUnit(0, _dragEntityId, (byte)grid.Value.col, (byte)grid.Value.row);
                    _viewBridge.SendCommand(cmd);
                }
                else
                {
                    // 같은 위치, 차단, 또는 보드 밖 → 원위치 복귀
                    _dragUnitView?.SetPositionImmediate(_dragOriginalPos);
                }
            }

            _dragEntityId = UnitData.InvalidId;
            _dragUnitView = null;

            // 드래그 종료 시 포커스 해제
            _targetLineManager?.ClearFocusedUnit();
        }

        // ── 보드 오브젝트 드래그 ──

        private bool TryPickBoardObject(Vector2 screenPos, out IBoardDraggableObject boardObj)
        {
            boardObj = null;
            if (_findBoardObject == null) return false;

            var grid = ScreenToGrid(screenPos);
            if (!grid.HasValue) return false;

            boardObj = _findBoardObject(grid.Value.col, grid.Value.row);
            return boardObj != null;
        }

        private void StartObjectDrag(IBoardDraggableObject boardObj)
        {
            InGameCharacterPopupHelper.Close();
            _isDraggingObject = true;
            _dragObject = boardObj;
            _dragObjectOriginalPos = boardObj.WorldPosition;
        }

        private void UpdateObjectDrag(Vector2 screenPos)
        {
            var worldPos = ScreenToWorldOnGround(screenPos);
            if (worldPos.HasValue && _dragObject != null)
                _dragObject.SetWorldPosition(worldPos.Value);

            var grid = ScreenToGrid(screenPos);
            if (grid.HasValue)
                ShowHighlight(grid.Value.col, grid.Value.row);
            else
                HideHighlight();
        }

        private void EndObjectDrag(Vector2 screenPos)
        {
            _isDraggingObject = false;
            HideHighlight();

            bool handled = false;
            var grid = ScreenToGrid(screenPos);
            if (grid.HasValue && _dragObject != null)
                handled = _dragObject.TryHandleDrop(grid.Value.col, grid.Value.row);

            if (!handled && _dragObject != null)
                _dragObject.SetWorldPosition(_dragObjectOriginalPos);

            _dragObject = null;
        }

        private void CancelDrag()
        {
            // pending 상태 정리
            _isPendingDrag = false;
            _isPendingObject = false;
            _pendingEntityId = UnitData.InvalidId;
            _pendingUnitView = null;

            if (_isDraggingBoardUnit && _dragUnitView != null)
            {
                _dragUnitView.SetHologram(false);
                _dragUnitView.SetPositionImmediate(_dragOriginalPos);
            }
            _isDraggingBoardUnit = false;
            _dragEntityId = UnitData.InvalidId;
            _dragUnitView = null;
            HideCanPlacement();

            // 오브젝트 드래그 취소
            if (_isDraggingObject && _dragObject != null)
                _dragObject.SetWorldPosition(_dragObjectOriginalPos);
            _isDraggingObject = false;
            _dragObject = null;

            // 드래그 취소 시 포커스 해제
            _targetLineManager?.ClearFocusedUnit();

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

            // 드래그/고스트 유닛의 공격 범위 표시
            int curAttackRange = 0;
            int entityId = _isDraggingBoardUnit ? _dragEntityId : _isGhostActive ? _ghostEntityId : UnitData.InvalidId;
            if (entityId != UnitData.InvalidId)
            {
                var world = _viewBridge.GetWorld();
                if (world != null)
                {
                    curAttackRange = world.GetUnit(entityId).AttackRange;
                    if (curAttackRange > 0)
                        _rangeHandle = _tileEffectManager.ShowRange(TileEffectType.AttackRange, col, row, curAttackRange);
                }
            }

            // CanPlacement를 AttackRange와 겹치지 않도록 갱신
            RefreshCanPlacement(col, row, curAttackRange);
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

            // AttackRange 해제됐으므로 CanPlacement 전체 복원
            RefreshCanPlacement(-1, -1, 0);
        }

        // ── 배치 가능 타일 표시 ──

        private void ShowCanPlacement(int entityId)
        {
            var world = _viewBridge.GetWorld();
            if (world == null) return;

            ref var unit = ref world.GetUnit(entityId);
            byte sizeW = unit.SizeW > 0 ? unit.SizeW : (byte)1;
            byte sizeH = unit.SizeH > 0 ? unit.SizeH : (byte)1;

            _canPlacementTiles = new List<(int col, int row)>();

            for (int r = 0; r < BoardHelper.Height; r++)
            {
                for (int c = 0; c < BoardHelper.Width; c++)
                {
                    if (!BoardHelper.IsValidBoardFootprint(c, r, sizeW, sizeH))
                        continue;

                    if (sizeW == 1 && sizeH == 1)
                    {
                        int index = BoardHelper.ToIndex(c, r);
                        int occupant = world.BoardSlots[0][index];

                        if (occupant == UnitData.InvalidId || occupant != entityId)
                            _canPlacementTiles.Add((c, r));
                    }
                    else
                    {
                        bool clear = true;
                        for (int dr = 0; dr < sizeH && clear; dr++)
                        {
                            for (int dc = 0; dc < sizeW && clear; dc++)
                            {
                                int idx = BoardHelper.ToIndex(c + dc, r + dr);
                                int occupant = world.BoardSlots[0][idx];
                                if (occupant != UnitData.InvalidId && occupant != entityId)
                                    clear = false;
                            }
                        }
                        if (clear)
                            _canPlacementTiles.Add((c, r));
                    }
                }
            }

            RefreshCanPlacement(-1, -1, 0);
        }

        private void RefreshCanPlacement(int highlightCol, int highlightRow, int attackRange)
        {
            if (_canPlacementHandle != 0)
            {
                _tileEffectManager.Hide(_canPlacementHandle);
                _canPlacementHandle = 0;
            }

            if (_canPlacementTiles == null || _canPlacementTiles.Count == 0)
                return;

            var filtered = new List<(int col, int row)>(_canPlacementTiles.Count);
            for (int i = 0; i < _canPlacementTiles.Count; i++)
            {
                var tile = _canPlacementTiles[i];

                // Placement 커서 위치 제외
                if (tile.col == highlightCol && tile.row == highlightRow)
                    continue;

                // AttackRange 범위 내 제외
                if (attackRange > 0 && BoardHelper.ManhattanDistance(highlightCol, highlightRow, tile.col, tile.row) <= attackRange)
                    continue;

                filtered.Add(tile);
            }

            if (filtered.Count > 0)
                _canPlacementHandle = _tileEffectManager.ShowTiles(TileEffectType.CanPlacement, filtered);
        }

        private void HideCanPlacement()
        {
            if (_canPlacementHandle != 0)
            {
                _tileEffectManager.Hide(_canPlacementHandle);
                _canPlacementHandle = 0;
            }
            _canPlacementTiles = null;
        }

        // ── 유닛 선택 (팝업은 InGameCharacterPopupHelper, 이펙트만 로컬) ──

        private void ApplyUnitTapSelection(int entityId, int col = -1, int row = -1)
        {
            if (entityId == UnitData.InvalidId)
            {
                InGameCharacterPopupHelper.Close();
                _targetLineManager?.ClearFocusedUnit();
                return;
            }

            var world = _viewBridge.GetWorld();
            if (world == null) return;

            ref var unit = ref world.GetUnit(entityId);
            var param = new CharacterInfoInGamePopup.PopupParam(unit.ChampionSpecId, unit.StarLevel);

            // Select 내부에서 이전 onDeselected(HideSelectionTileEffect)가 호출되므로, 이펙트는 그 뒤에 표시
            InGameCharacterPopupHelper.Select(entityId, param, HideSelectionTileEffect);
            _selectEffectHandle = _tileEffectManager.Show(TileEffectType.Placement, col, row);

            _targetLineManager?.SetFocusedUnit(entityId);
        }

        private void HideSelectionTileEffect()
        {
            if (_selectEffectHandle != 0)
            {
                _tileEffectManager.Hide(_selectEffectHandle);
                _selectEffectHandle = 0;
            }
        }
    }
}
