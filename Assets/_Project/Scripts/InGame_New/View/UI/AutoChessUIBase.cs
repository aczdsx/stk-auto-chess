using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using LitMotion;
using R3;
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

        [Header("Synergy")]
        [SerializeField] protected TableView synergyTableView;

        [Header("Info")]
        [SerializeField] protected TMP_Text unitCountText;

        [Header("Exit")]
        [SerializeField] protected CAButton exitButton;

        [Header("Speed")]
        [SerializeField] protected CAButton speedButton;
        [SerializeField] protected GameObject speedOnObj;
        [SerializeField] protected GameObject speedOffObj;

        [Header("Animator")]
        [SerializeField] protected Animator animator;

        [Header("HUD")]
        [SerializeField] protected TMP_Text phaseText;
        [SerializeField] protected TMP_Text timerText;
        [SerializeField] protected TMP_Text goldText;
        [SerializeField] protected TMP_Text levelText;
        [SerializeField] protected TMP_Text hpText;
        [SerializeField] protected TMP_Text stageText;

        [Header("Timer Warning")]
        [SerializeField] protected GameObject timerWarningObj;

        private MotionHandle _timerWarningHandle;
        private bool _isTimerWarningActive;

        protected AutoChessViewBridge ViewBridge { get; private set; }
        protected BoardInputHandler BoardInput { get; private set; }
        protected byte PlayerIndex { get; private set; }
        protected GameWorld CurrentWorld { get; private set; }

        protected readonly List<int> benchIds = new();
        private readonly List<int> _rawBenchIds = new();
        protected readonly List<int> synergyIds = new();
        private readonly List<byte> _synergyCounts = new();
        private readonly HashSet<int> _inBattleChampionIds = new();

        // ── HUD ReactiveProperty (값 변경 시에만 TMP_Text 갱신 → GC 방지) ──
        private readonly ReactiveProperty<int> _rpTimer = new(-1);
        private readonly ReactiveProperty<int> _rpGold = new(-1);
        private readonly ReactiveProperty<int> _rpLevel = new(-1);
        private readonly ReactiveProperty<(int hp, int maxHp)> _rpHp = new((-1, -1));
        private readonly ReactiveProperty<(int count, int max)> _rpUnitCount = new((-1, -1));

        // ── 초기화 ──

        protected InGameMainParams InGameParams { get; private set; }

        public void Initialize(
            AutoChessViewBridge viewBridge,
            BoardInputHandler boardInput,
            InGameMainParams inGameParams,
            byte playerIndex = 0)
        {
            ViewBridge = viewBridge;
            BoardInput = boardInput;
            InGameParams = inGameParams;
            PlayerIndex = playerIndex;

            exitButton?.onClick.AddListener(OnExitClicked);
            speedButton?.onClick.AddListener(OnSpeedClicked);
            if (timerWarningObj != null)
            {
                timerWarningObj.SetActive(false);
            }
            BindHUDReactiveProperties();
            InitSpeed();
            OnInitialize();
        }

        private void BindHUDReactiveProperties()
        {
            if (timerText != null)
                _rpTimer.SubscribeToText(timerText).AddTo(this);
            if (goldText != null)
                _rpGold.SubscribeToText(goldText).AddTo(this);
            if (levelText != null)
                _rpLevel.SubscribeToText(levelText, v => $"Lv.{v}").AddTo(this);
            if (hpText != null)
                _rpHp.SubscribeToText(hpText, v => $"{v.hp}/{v.maxHp}").AddTo(this);
            if (unitCountText != null)
                _rpUnitCount.SubscribeToText(unitCountText, v => $"{v.count}/{v.max}").AddTo(this);
        }

        protected virtual void OnInitialize() { }

        public void SetStageName(string name)
        {
            if (stageText != null)
                stageText.text = name;
        }

        // ── 애니메이션 ──

        public void PlayAnimation(string trigger)
        {
            if (animator != null)
                animator.SetTrigger(trigger);
        }

        // ── 나가기 ──

        private void OnExitClicked()
        {
            OnExitClickedAsync().Forget();
        }

        protected virtual async UniTaskVoid OnExitClickedAsync()
        {
            var popupData = new SystemConfirmPopupData(
                "UI_SYSTEM_ALERT", "MSG_SURRENDER_CONFIRM", "UI_CONFIRM_BTN", "UI_CANCEL_BTN");
            var popup = await SceneUILayerManager.Instance
                .PushUILayerAsync<SystemConfirmPopup>(popupData);
            var isConfirmed = await popup.WaitForExit();
            if (isConfirmed is not true) return;

            // TODO: 항복 처리
        }

        // ── 배속 ──

        private const float SpeedNormal = 1f;
        private const float SpeedFast = 1.3f;

        private void InitSpeed()
        {
            bool isSpeedUp = Preference.LoadPreference(Pref.IS_SPEED_UP, false);
            LocalSimulationRunner.SpeedMultiplier = isSpeedUp ? SpeedFast : SpeedNormal;
            UpdateSpeedUI(isSpeedUp);
        }

        private void OnSpeedClicked()
        {
            bool isSpeedUp = Preference.LoadPreference(Pref.IS_SPEED_UP, false);
            isSpeedUp = !isSpeedUp;
            Preference.SavePreference(Pref.IS_SPEED_UP, isSpeedUp);
            LocalSimulationRunner.SpeedMultiplier = isSpeedUp ? SpeedFast : SpeedNormal;
            UpdateSpeedUI(isSpeedUp);
        }

        private void UpdateSpeedUI(bool isSpeedUp)
        {
            if (speedOnObj != null) speedOnObj.SetActive(isSpeedUp);
            if (speedOffObj != null) speedOffObj.SetActive(!isSpeedUp);
        }

        // ── 틱 동기화 ──

        public void SyncState(GameWorld world)
        {
            if (world == null) return;
            CurrentWorld = world;

            SyncInBattleChampionIds(world);
            SyncBenchSlots(world);
            SyncSynergySlots(world);
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

            // 타이머 경고 정리
            if (newPhase != GamePhase.Combat)
            {
                StopTimerWarning();
            }

            OnPhaseChangedInternal(newPhase);
        }

        private void StopTimerWarning()
        {
            if (_isTimerWarningActive)
            {
                _isTimerWarningActive = false;
                if (_timerWarningHandle.IsActive())
                    _timerWarningHandle.Cancel();
                if (timerWarningObj != null)
                    timerWarningObj.SetActive(false);
            }
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

        public virtual void OnUnitDied(int victimEntityId, int killerEntityId, GameWorld world) { }

        public void OnSynergyUpdated(GameWorld world)
        {
            if (world == null) return;
            CurrentWorld = world;
            SyncSynergySlots(world);
        }

        // ── HUD 갱신 ──

        private void UpdateHUD(GameWorld world)
        {
            // 타이머 (Result 페이즈에서는 갱신 안 함 — 마지막 전투 타이머 유지)
            if (timerText != null && world.CurrentPhase != GamePhase.Result)
            {
                _rpTimer.Value = Mathf.CeilToInt(world.PhaseTimerFrames / (float)world.TickRate);

                // 10초 경고 인디케이터 (정수 프레임 비교로 부동소수점 오차 방지)
                int warningThresholdFrames = 10 * world.TickRate;
                if (world.CurrentPhase == GamePhase.Combat
                    && world.PhaseTimerFrames <= warningThresholdFrames
                    && world.PhaseTimerFrames > 0
                    && !_isTimerWarningActive)
                {
                    _isTimerWarningActive = true;
                    if (timerWarningObj != null)
                    {
                        timerWarningObj.SetActive(true);
                    }
                }
            }

            // 플레이어 정보
            if (PlayerIndex < world.MaxPlayers)
            {
                var player = world.Players[PlayerIndex];
                var economy = world.Economies[PlayerIndex];

                _rpGold.Value = economy.Gold;
                _rpLevel.Value = economy.Level;
                _rpHp.Value = (player.HP, player.MaxHP);

                // stageText는 SetStageName()으로 초기화 시 한 번만 설정
            }
        }

        // ── 벤치 슬롯 동기화 ──

        private void SyncBenchSlots(GameWorld world)
        {
            var benchSlots = world.BenchSlots[PlayerIndex];

            // 변경 감지: _rawBenchIds(필터 전 원본)과 비교
            bool changed = false;
            int newCount = 0;
            for (int i = 0; i < benchSlots.Length; i++)
            {
                if (benchSlots[i] != UnitData.InvalidId)
                    newCount++;
            }

            if (newCount != _rawBenchIds.Count)
            {
                changed = true;
            }
            else
            {
                int idx = 0;
                for (int i = 0; i < benchSlots.Length; i++)
                {
                    int entityId = benchSlots[i];
                    if (entityId == UnitData.InvalidId) continue;
                    if (idx >= _rawBenchIds.Count || _rawBenchIds[idx] != entityId)
                    {
                        changed = true;
                        break;
                    }
                    idx++;
                }
            }

            if (!changed) return;

            _rawBenchIds.Clear();
            for (int i = 0; i < benchSlots.Length; i++)
            {
                int entityId = benchSlots[i];
                if (entityId == UnitData.InvalidId) continue;
                _rawBenchIds.Add(entityId);
            }

            benchIds.Clear();
            benchIds.AddRange(_rawBenchIds);
            FilterBenchIds();

            tableView.RefreshAll();
        }

        /// <summary>
        /// benchIds에 필터를 적용하는 훅. 서브클래스에서 override.
        /// </summary>
        protected virtual void FilterBenchIds() { }

        /// <summary>
        /// 필터 변경 시 benchIds를 다시 계산하고 TableView 갱신.
        /// </summary>
        protected void RefreshBenchDisplay()
        {
            benchIds.Clear();
            benchIds.AddRange(_rawBenchIds);
            FilterBenchIds();
            tableView.RefreshAll();
        }

        // ── 보드 유닛 ChampionSpecId 수집 (시너지 팝업용) ──

        private void SyncInBattleChampionIds(GameWorld world)
        {
            _inBattleChampionIds.Clear();
            var boardSlots = world.BoardSlots[PlayerIndex];
            for (int i = 0; i < boardSlots.Length; i++)
            {
                int entityId = boardSlots[i];
                if (entityId == UnitData.InvalidId) continue;
                int unitIndex = world.FindUnitIndex(entityId);
                if (unitIndex >= 0)
                    _inBattleChampionIds.Add(world.Units[unitIndex].ChampionSpecId);
            }
        }

        // ── 시너지 슬롯 동기화 ──

        private void SyncSynergySlots(GameWorld world)
        {
            if (synergyTableView == null) return;
            if (world.Synergies == null || world.Synergies[PlayerIndex] == null) return;

            var synergy = world.Synergies[PlayerIndex];

            // count > 0 인 traitId 수집 (내림차순 정렬)
            _tempSynergyList.Clear();
            for (int i = 1; i < PlayerSynergy.MaxTraits; i++)
            {
                byte count = synergy.GetTraitCount(i);
                if (count > 0)
                    _tempSynergyList.Add((i, count));
            }
            _tempSynergyList.Sort(SynergyCountDescComparer);

            // 변경 감지 (traitId + count 모두 비교)
            bool changed = _tempSynergyList.Count != synergyIds.Count;
            if (!changed)
            {
                for (int i = 0; i < _tempSynergyList.Count; i++)
                {
                    if (synergyIds[i] != _tempSynergyList[i].traitId ||
                        _synergyCounts[i] != _tempSynergyList[i].count)
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed) return;

            synergyIds.Clear();
            _synergyCounts.Clear();
            for (int i = 0; i < _tempSynergyList.Count; i++)
            {
                synergyIds.Add(_tempSynergyList[i].traitId);
                _synergyCounts.Add(_tempSynergyList[i].count);
            }

            synergyTableView.RefreshAll();
        }

        private readonly List<(int traitId, byte count)> _tempSynergyList = new();
        private static readonly System.Comparison<(int traitId, byte count)> SynergyCountDescComparer
            = (a, b) => b.count.CompareTo(a.count);

        protected void BindSynergyCell(InGameSynergyUI cell, int synergyTypeId, int index)
        {
            if (CurrentWorld == null) return;

            cell.SetInBattleChampionIds(_inBattleChampionIds);

            var synergyType = (SynergyType)synergyTypeId;
            int count = CurrentWorld.Synergies[PlayerIndex].GetTraitCount(synergyTypeId);

            var specDataManager = SpecDataManager.Instance;
            specDataManager.TryGetSynergyDataByCount(
                synergyType, count,
                out var outSynergyData, out var outSynergyList);

            if (outSynergyList == null || outSynergyList.Count == 0) return;

            bool isActive = outSynergyData != null;
            if (isActive)
            {
                var nextData = outSynergyList.Find(l => l.grade == outSynergyData.grade + 1) ?? outSynergyData;
                cell.SetSynergy(synergyType, count, outSynergyData, nextData, isActive: true);
            }
            else if (count > 0)
            {
                var nextData = outSynergyList[0];
                cell.SetSynergy(synergyType, count, nextData, nextData, isActive: false);
            }

            // 활성화된 마지막 항목에만 split line 표시 (하단에 비활성 항목이 있을 때)
            bool showSplit = isActive && IsNextSynergyInactive(index + 1);
            cell.SetSplitLine(showSplit);
        }

        private bool IsNextSynergyInactive(int index)
        {
            if (CurrentWorld == null) return false;
            if (index < 0 || index >= synergyIds.Count) return false;

            int nextTraitId = synergyIds[index];
            int nextCount = CurrentWorld.Synergies[PlayerIndex].GetTraitCount(nextTraitId);

            SpecDataManager.Instance.TryGetSynergyDataByCount(
                (SynergyType)nextTraitId, nextCount,
                out var nextData, out _);

            return nextData == null;
        }

        // ── 배치 인원 표시 ──

        private void UpdateUnitCountText(GameWorld world)
        {
            if (unitCountText == null) return;

            int boardUnitCount = world.Boards[PlayerIndex].UnitCount;
            int maxUnits = world.Economies[PlayerIndex].Level;
            _rpUnitCount.Value = (boardUnitCount, maxUnits);
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
            StopTimerWarning();
            exitButton?.onClick.RemoveListener(OnExitClicked);
            speedButton?.onClick.RemoveListener(OnSpeedClicked);
            OnCleanup();
        }

        protected virtual void OnCleanup() { }
    }
}
