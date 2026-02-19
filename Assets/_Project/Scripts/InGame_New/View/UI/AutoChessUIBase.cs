using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// AutoChess 모드 UI 베이스 클래스.
    /// 벤치 슬롯, 배치 카운트, HUD(타이머/골드/레벨/HP/스테이지) 등 공통 로직.
    /// 모드별 서브클래스(ClassicAutoChessUI, CampaignAutoChessUI 등)에서 상속.
    /// </summary>
    public abstract class AutoChessUIBase : MonoBehaviour
    {
        [Header("Bench List")]
        [SerializeField] private BenchUnitSlot _slotPrefab;
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Info")]
        [SerializeField] private TMP_Text _unitCountText;

        [Header("HUD")]
        [SerializeField] private TMP_Text _phaseText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private TMP_Text _stageText;

        protected AutoChessViewBridge ViewBridge { get; private set; }
        protected BoardInputHandler BoardInput { get; private set; }
        protected byte PlayerIndex { get; private set; }

        private Dictionary<int, CharacterDisplayInfo> _entityDisplayMap;
        private readonly Dictionary<int, BenchUnitSlot> _slots = new();
        private readonly List<int> _toRemove = new();

        // ── 초기화 ──

        public void Initialize(
            AutoChessViewBridge viewBridge,
            BoardInputHandler boardInput,
            Dictionary<int, CharacterDisplayInfo> entityDisplayMap,
            byte playerIndex = 0)
        {
            ViewBridge = viewBridge;
            BoardInput = boardInput;
            _entityDisplayMap = entityDisplayMap;
            PlayerIndex = playerIndex;

            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        // ── 틱 동기화 ──

        public void SyncState(GameWorld world)
        {
            if (world == null) return;

            SyncBenchSlots(world);
            UpdateUnitCountText(world);
            UpdateHUD(world);
            OnSyncState(world);
        }

        protected virtual void OnSyncState(GameWorld world) { }

        // ── 페이즈 전환 ──

        public void OnPhaseChanged(GamePhase newPhase)
        {
            if (_phaseText != null)
            {
                _phaseText.text = newPhase switch
                {
                    GamePhase.Preparation => "Preparation",
                    GamePhase.Combat => "Combat",
                    GamePhase.Result => "Result",
                    GamePhase.SharedDraft => "Shared Draft",
                    _ => newPhase.ToString(),
                };
            }

            OnPhaseChangedInternal(newPhase);
        }

        protected virtual void OnPhaseChangedInternal(GamePhase newPhase) { }

        // ── 게임 이벤트 수신 (ViewBridge에서 호출) ──

        public virtual void OnGoldChanged(int playerIndex, int totalGold, int delta)
        {
            // TODO: 골드 변경 애니메이션 (+/- 플로팅 텍스트)
        }

        public virtual void OnLevelUp(int playerIndex, int newLevel)
        {
            // TODO: 레벨업 연출 (이펙트, 사운드)
        }

        public virtual void OnPlayerEliminated(int playerIndex, int rank)
        {
            // TODO: 탈락 알림 UI
        }

        public virtual void OnCombatResult(int matchIndex, int winner)
        {
            // TODO: 전투 결과 표시 (승리/패배 배너)
        }

        // ── HUD 갱신 ──

        private void UpdateHUD(GameWorld world)
        {
            // 타이머
            if (_timerText != null)
            {
                float seconds = world.PhaseTimerFrames / (float)world.TickRate;
                _timerText.text = Mathf.CeilToInt(seconds).ToString();
            }

            // 플레이어 정보
            if (PlayerIndex >= 0 && PlayerIndex < GameWorld.MaxPlayers)
            {
                var player = world.Players[PlayerIndex];
                var economy = world.Economies[PlayerIndex];

                if (_goldText != null)
                    _goldText.text = economy.Gold.ToString();

                if (_levelText != null)
                    _levelText.text = $"Lv.{economy.Level}";

                if (_hpText != null)
                    _hpText.text = $"{player.HP}/{player.MaxHP}";

                if (_stageText != null)
                    _stageText.text = $"{world.CurrentStage}-{world.CurrentRound}";
            }
        }

        // ── 벤치 슬롯 동기화 ──

        private void SyncBenchSlots(GameWorld world)
        {
            var benchSlots = world.BenchSlots[PlayerIndex];
            var activeBenchIds = new HashSet<int>();

            for (int i = 0; i < PlayerBoard.BenchSize; i++)
            {
                int entityId = benchSlots[i];
                if (entityId == UnitData.InvalidId) continue;

                activeBenchIds.Add(entityId);

                if (!_slots.ContainsKey(entityId))
                {
                    CreateSlot(entityId);
                }
            }

            _toRemove.Clear();
            foreach (var kvp in _slots)
            {
                if (!activeBenchIds.Contains(kvp.Key))
                    _toRemove.Add(kvp.Key);
            }
            foreach (int id in _toRemove)
            {
                DestroySlot(id);
            }
        }

        private void CreateSlot(int entityId)
        {
            if (_slotPrefab == null || _slotContainer == null) return;

            var slot = Instantiate(_slotPrefab, _slotContainer);

            CharacterDisplayInfo displayInfo = default;
            _entityDisplayMap?.TryGetValue(entityId, out displayInfo);

            slot.SetData(entityId, displayInfo, this, ViewBridge, BoardInput);
            slot.gameObject.SetActive(true);
            _slots[entityId] = slot;
        }

        private void DestroySlot(int entityId)
        {
            if (_slots.TryGetValue(entityId, out var slot))
            {
                Destroy(slot.gameObject);
                _slots.Remove(entityId);
            }
        }

        // ── 배치 인원 표시 ──

        private void UpdateUnitCountText(GameWorld world)
        {
            if (_unitCountText == null) return;

            int boardUnitCount = world.Boards[PlayerIndex].UnitCount;
            int maxUnits = world.Economies[PlayerIndex].Level;
            _unitCountText.text = $"{boardUnitCount}/{maxUnits}";
        }

        // ── UI 영역 판별 ──

        public bool IsPointInScrollRect(Vector2 screenPos)
        {
            if (_scrollRect == null) return false;
            var rectTransform = _scrollRect.GetComponent<RectTransform>();
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPos, null, out var localPoint)
                && rectTransform.rect.Contains(localPoint);
        }

        public ScrollRect GetScrollRect() => _scrollRect;

        // ── 정리 ──

        private void OnDestroy()
        {
            OnCleanup();
        }

        protected virtual void OnCleanup() { }
    }
}
