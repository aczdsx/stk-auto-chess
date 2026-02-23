using System.Collections.Generic;
using CookApps.TeamBattle.UI;
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
        [Header("Bench")]
        [SerializeField] protected TableView tableView;

        [Header("Info")]
        [SerializeField] protected TMP_Text unitCountText;

        [Header("HUD")]
        [SerializeField] protected TMP_Text phaseText;
        [SerializeField] protected TMP_Text timerText;
        [SerializeField] protected TMP_Text goldText;
        [SerializeField] protected TMP_Text levelText;
        [SerializeField] protected TMP_Text hpText;
        [SerializeField] protected TMP_Text stageText;

        protected AutoChessViewBridge ViewBridge { get; private set; }
        protected BoardInputHandler BoardInput { get; private set; }
        protected byte PlayerIndex { get; private set; }
        protected GameWorld CurrentWorld { get; private set; }

        protected readonly List<int> benchIds = new();

        // ── 초기화 ──

        public void Initialize(
            AutoChessViewBridge viewBridge,
            BoardInputHandler boardInput,
            byte playerIndex = 0)
        {
            ViewBridge = viewBridge;
            BoardInput = boardInput;
            PlayerIndex = playerIndex;

            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        // ── 틱 동기화 ──

        public void SyncState(GameWorld world)
        {
            if (world == null) return;
            CurrentWorld = world;

            SyncBenchSlots(world);
            UpdateUnitCountText(world);
            UpdateHUD(world);
            OnSyncState(world);
        }

        protected virtual void OnSyncState(GameWorld world) { }

        // ── 페이즈 전환 ──

        public void OnPhaseChanged(GamePhase newPhase)
        {
            if (phaseText != null)
            {
                phaseText.text = newPhase switch
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
            if (timerText != null)
            {
                float seconds = world.PhaseTimerFrames / (float)world.TickRate;
                timerText.text = Mathf.CeilToInt(seconds).ToString();
            }

            // 플레이어 정보
            if (PlayerIndex >= 0 && PlayerIndex < GameWorld.MaxPlayers)
            {
                var player = world.Players[PlayerIndex];
                var economy = world.Economies[PlayerIndex];

                if (goldText != null)
                    goldText.text = economy.Gold.ToString();

                if (levelText != null)
                    levelText.text = $"Lv.{economy.Level}";

                if (hpText != null)
                    hpText.text = $"{player.HP}/{player.MaxHP}";

                if (stageText != null)
                    stageText.text = $"{world.CurrentStage}-{world.CurrentRound}";
            }
        }

        // ── 벤치 슬롯 동기화 ──

        private void SyncBenchSlots(GameWorld world)
        {
            var benchSlots = world.BenchSlots[PlayerIndex];
            benchIds.Clear();

            for (int i = 0; i < PlayerBoard.BenchSize; i++)
            {
                int entityId = benchSlots[i];
                if (entityId == UnitData.InvalidId) continue;
                benchIds.Add(entityId);
            }

            tableView.RefreshAll();
        }

        // ── 배치 인원 표시 ──

        private void UpdateUnitCountText(GameWorld world)
        {
            if (unitCountText == null) return;

            int boardUnitCount = world.Boards[PlayerIndex].UnitCount;
            int maxUnits = world.Economies[PlayerIndex].Level;
            unitCountText.text = $"{boardUnitCount}/{maxUnits}";
        }

        // ── UI 영역 판별 ──

        public bool IsPointInScrollRect(Vector2 screenPos)
        {
            if (tableView == null) return false;
            var rectTransform = tableView.GetComponent<RectTransform>();
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPos, null, out var localPoint)
                && rectTransform.rect.Contains(localPoint);
        }

        public ScrollRect GetScrollRect() => tableView;

        // ── 정리 ──

        private void OnDestroy()
        {
            OnCleanup();
        }

        protected virtual void OnCleanup() { }
    }
}
