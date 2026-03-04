using CookApps.AutoBattler;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
        [Header("Character Icon")]
        [SerializeField] private Transform _characterIconRoot;

        [SerializeField] private CAButton _characterButton;

        [Header("Display")]
        // 머지 머지 모드에서 성을 표현
        [SerializeField] private Image _starIcon;
        [SerializeField] private SpriteLoader _synergyElementLoader;
        [SerializeField] private SpriteLoader _synergyClassLoader;

        public int EntityId { get; private set; } = UnitData.InvalidId;

        private AutoChessUIBase _parentUI;
        private AutoChessViewBridge _viewBridge;
        private BoardInputHandler _boardInput;
        private GameObject _loadedCharacterIcon;

        // 현재 표시 중인 데이터 (중복 로드 방지)
        protected int _currentChampSpecId;
        protected byte _currentStarLevel;

        // 드래그 상태
        private bool _isDraggingToBoard;
        private bool _scrollDragStarted;
        private bool _wasDragging;
        private ScrollRect _cachedScrollRect;

        // ── 초기화 ──

        protected virtual void Awake()
        {
            if (_characterButton != null)
            {
                _characterButton.OnClickAsObservable()
                    .Subscribe(this, (_, self) => self.OnClickSlot())
                    .AddTo(this);
            }
        }

        // ── 데이터 설정 ──

        public void Init(AutoChessUIBase parentUI, AutoChessViewBridge viewBridge, BoardInputHandler boardInput)
        {
            _parentUI = parentUI;
            _viewBridge = viewBridge;
            _boardInput = boardInput;
        }

        public void Bind(int entityId, int champSpecId, byte starLevel)
        {
            EntityId = entityId;
            if (_currentChampSpecId == champSpecId && _currentStarLevel == starLevel)
                return;

            _currentChampSpecId = champSpecId;
            _currentStarLevel = starLevel;
            UpdateVisual();
        }

        protected virtual void UpdateVisual()
        {
            if (_currentChampSpecId <= 0) return;

            var spec = SpecDataManager.Instance.GetSpecCharacter(_currentChampSpecId);
            if (spec == null) return;

            // 캐릭터 아이콘 로드 (Addressables 프리팹 인스턴스)
            LoadCharacterIcon(spec.prefab_id).Forget();

            // 별 레벨 표시
            if (_starIcon != null)
                _starIcon.gameObject.SetActive(_currentStarLevel > 0);

            // 시너지 아이콘
            _synergyElementLoader?.SetSprite(
                SpriteNameParser.GetSpriteName(spec.character_element_type)).Forget();
            _synergyClassLoader?.SetSprite(
                SpriteNameParser.GetSpriteName(spec.character_stella_type)).Forget();
        }

        // ── 캐릭터 아이콘 ──

        private async UniTaskVoid LoadCharacterIcon(int prefabId)
        {
            ReleaseCharacterIcon();
            ClearCharacterIconRoot();

            string address = $"SD/{prefabId}/UI_{prefabId}.prefab";
            _loadedCharacterIcon = await Addressables.InstantiateAsync(address, _characterIconRoot);
        }

        private void ClearCharacterIconRoot()
        {
            if (_characterIconRoot == null) return;
            for (int i = _characterIconRoot.childCount - 1; i >= 0; i--)
                Destroy(_characterIconRoot.GetChild(i).gameObject);
        }

        private void ReleaseCharacterIcon()
        {
            if (_loadedCharacterIcon != null)
            {
                Addressables.ReleaseInstance(_loadedCharacterIcon);
                _loadedCharacterIcon = null;
            }
        }

        private void OnDisable()
        {
            ReleaseCharacterIcon();
            _currentChampSpecId = 0;
            _currentStarLevel = 0;
        }

        // ── 드래그 핸들러 ──

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDraggingToBoard = false;
            _scrollDragStarted = false;
            _wasDragging = true;

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

        // ── 클릭 & 선택 ──

        private static BenchUnitSlot _currentSelected;
        private static CharacterInfoInGamePopup _openPopup;

        protected virtual void OnClickSlot()
        {
            if (_wasDragging)
            {
                _wasDragging = false;
                return;
            }

            if (_currentChampSpecId <= 0) return;

            // 같은 슬롯 재클릭 → 닫기
            if (_currentSelected == this)
            {
                CloseCurrentPopup();
                return;
            }

            // 팝업이 이미 열려있으면 → 슬롯만 교체하고 데이터 갱신
            if (_openPopup != null)
            {
                if (_currentSelected != null)
                    _currentSelected.OnDeselected();

                _currentSelected = this;
                OnSelected();

                var popupParam = new CharacterInfoInGamePopup.PopupParam(_currentChampSpecId, _currentStarLevel);
                _openPopup.Refresh(popupParam);
                return;
            }

            // 이전 슬롯 정리
            if (_currentSelected != null)
            {
                _currentSelected.OnDeselected();
                _currentSelected = null;
            }

            _currentSelected = this;
            OnSelected();
            OpenPopupAsync().Forget();
        }

        private static void CloseCurrentPopup()
        {
            if (_openPopup != null)
            {
                SceneUILayerManager.Instance.PopUILayer(_openPopup);
                _openPopup = null;
            }
            if (_currentSelected != null)
            {
                _currentSelected.OnDeselected();
                _currentSelected = null;
            }
        }

        private async UniTaskVoid OpenPopupAsync()
        {
            var self = this;
            var popupParam = new CharacterInfoInGamePopup.PopupParam(_currentChampSpecId, _currentStarLevel);
            var popup = await SceneUILayerManager.Instance.PushUILayerAsync<CharacterInfoInGamePopup>(popupParam, _ =>
            {
                // 다른 슬롯이 이미 선택되었으면 무시
                if (_currentSelected != self) return;
                _openPopup = null;
                self.OnDeselected();
                _currentSelected = null;
            });

            if (_currentSelected == self)
            {
                _openPopup = popup;
            }
            else
            {
                // 로딩 중 다른 슬롯이 선택됨 → 이 팝업 즉시 닫기
                SceneUILayerManager.Instance.PopUILayer(popup);
            }
        }

        protected virtual void OnSelected() { }
        protected virtual void OnDeselected() { }

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
