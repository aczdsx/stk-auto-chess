using CookApps.AutoBattler;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 벤치 유닛 UI 슬롯. ScrollRect 내에서 가로 스크롤과 보드 드래그를 동시 지원.
    /// 보드 영역 진입 시 BoardInputHandler의 고스트 드래그로 전환.
    /// </summary>
    public class BenchUnitSlot : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Display")]
        [SerializeField] private Image _icon;
        [SerializeField] private Image _starIcon;

        public int EntityId { get; private set; } = UnitData.InvalidId;

        private AutoChessUIBase _parentUI;
        private AutoChessViewBridge _viewBridge;
        private BoardInputHandler _boardInput;
        private CharacterDisplayInfo _displayInfo;

        // 드래그 상태
        private bool _isDraggingToBoard;
        private bool _scrollDragStarted;
        private ScrollRect _cachedScrollRect;

        // ── 데이터 설정 ──

        public void SetData(
            int entityId,
            CharacterDisplayInfo info,
            AutoChessUIBase parentUI,
            AutoChessViewBridge viewBridge,
            BoardInputHandler boardInput)
        {
            EntityId = entityId;
            _displayInfo = info;
            _parentUI = parentUI;
            _viewBridge = viewBridge;
            _boardInput = boardInput;

            UpdateVisual();
        }

        private void UpdateVisual()
        {
            // TODO: 아이콘, 별 등 표시 정보 갱신
            // 스펙에서 아이콘 경로를 조회하여 Addressables 로드
            if (_displayInfo.ChampionSpecId > 0)
            {
                var spec = SpecDataManager.Instance.GetSpecCharacter(_displayInfo.ChampionSpecId);
                // _icon.sprite = ...
            }
        }

        // ── 드래그 핸들러 ──

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDraggingToBoard = false;
            _scrollDragStarted = false;

            // ScrollRect 캐싱
            if (_cachedScrollRect == null)
                _cachedScrollRect = _parentUI?.GetScrollRect();

            // 기본은 ScrollRect에 위임
            if (_cachedScrollRect != null)
            {
                _scrollDragStarted = true;
                _cachedScrollRect.OnBeginDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 screenPos = eventData.position;

            if (!_isDraggingToBoard)
            {
                // 아직 스크롤 모드 → 보드 영역 진입 체크
                bool isOverBoard = IsOverBoardArea(screenPos);

                if (isOverBoard)
                {
                    // 스크롤 → 보드 드래그 전환
                    _isDraggingToBoard = true;

                    if (_scrollDragStarted && _cachedScrollRect != null)
                    {
                        _cachedScrollRect.OnEndDrag(eventData);
                        _scrollDragStarted = false;
                    }

                    _boardInput?.StartGhostDrag(EntityId, screenPos);
                }
                else
                {
                    // 스크롤 계속
                    _cachedScrollRect?.OnDrag(eventData);
                }
            }
            else
            {
                // 보드 드래그 모드
                _boardInput?.UpdateGhostDrag(screenPos);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Vector2 screenPos = eventData.position;

            if (_isDraggingToBoard)
            {
                // 보드 드래그 종료 → 배치 시도
                var result = _boardInput?.EndGhostDrag(screenPos);
                if (result.HasValue)
                {
                    // 유효한 셀에 드롭 → PlaceUnit 커맨드
                    var cmd = GameCommand.PlaceUnit(0, EntityId, (byte)result.Value.col, (byte)result.Value.row);
                    _viewBridge?.SendCommand(cmd);
                }
                // else: 무효 → 자동 취소 (EndGhostDrag에서 처리)
            }
            else
            {
                // 스크롤 드래그 종료
                if (_scrollDragStarted && _cachedScrollRect != null)
                {
                    _cachedScrollRect.OnEndDrag(eventData);
                    _scrollDragStarted = false;
                }
            }

            _isDraggingToBoard = false;
        }

        // ── 보드 영역 판별 ──

        private bool IsOverBoardArea(Vector2 screenPos)
        {
            // ScrollRect 영역 밖이면 보드 영역으로 간주
            if (_parentUI != null && !_parentUI.IsPointInScrollRect(screenPos))
                return true;

            return false;
        }
    }
}
