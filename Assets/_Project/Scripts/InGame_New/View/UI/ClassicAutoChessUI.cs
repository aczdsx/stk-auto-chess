using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using CookApps.BattleSystem;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// ClassicBattle 모드용 UI.
    /// 수동 전투 시작 버튼만 추가. 상점/경제/시너지 없음.
    /// </summary>
    public class ClassicAutoChessUI : AutoChessUIBase
    {
        [Header("Bench Units")]
        [SerializeField] private BenchUnitSlot _slotPrefab;

        [Header("Synergy Unit")]
        [SerializeField] private InGameSynergyUI _synergySlotPrefab;

        [Header("Tab")]
        [SerializeField] private SimpleTabSwapper _characterBattleItemTabSwapper;

        [Header("Classic Mode")]
        [SerializeField] private CAButton _startBattleButton;
        [SerializeField] private CAButton _filterButton;
        [SerializeField] private CAButton _recommendButton;
        [SerializeField] private GameObject _recommendObjOn;
        [SerializeField] private GameObject _recommendObjOff;

        private TableViewController<int, BenchUnitSlot> _benchController;
        private TableViewController<int, InGameSynergyUI> _synergyController;
        private bool _isStarting;

        private HashSet<SynergyType> _selectedElementFilters = new();
        private HashSet<SynergyType> _selectedStellaFilters = new();

        protected override void OnInitialize()
        {
            _benchController = tableView.CreateController<int, BenchUnitSlot>()
                .WithData(benchIds)
                .WithCellPrefab(_slotPrefab.gameObject)
                .WithCellSize(new Vector2(120, 172))
                .OnCellCreated(cell =>
                {
                    cell.Init(this, ViewBridge, BoardInput);
                })
                .OnBind((cell, entityId, index) =>
                {
                    if (CurrentWorld == null) return;
                    ref var unit = ref CurrentWorld.GetUnit(entityId);
                    cell.Bind(entityId, unit.ChampionSpecId, unit.StarLevel);
                })
                .Build();

            _synergyController = synergyTableView.CreateController<int, InGameSynergyUI>()
                .WithData(synergyIds)
                .WithCellPrefab(_synergySlotPrefab.gameObject)
                .WithCellSize(new Vector2(400, 70))
                .OnBind((cell, synergyTypeId, index) => BindSynergyCell(cell, synergyTypeId, index))
                .Build();

            _startBattleButton?.onClick.AddListener(OnStartBattleClicked);
            _filterButton?.onClick.AddListener(OnFilterClicked);
            _recommendButton?.onClick.AddListener(OnRecommendClicked);
        }

        protected override void OnSyncState(GameWorld world)
        {
            UpdateRecommendState();
        }

        // ── 전투 시작 ──

        private async void OnStartBattleClicked()
        {
            if (_isStarting) return;
            _isStarting = true;

            try
            {
                if (!await IsCheckStartBattle())
                {
                    _isStarting = false;
                    return;
                }

                var cmd = GameCommand.Ready(PlayerIndex);
                ViewBridge?.SendCommand(cmd);
            }
            catch
            {
                _isStarting = false;
            }
        }

        private async UniTask<bool> IsCheckStartBattle()
        {
            if (CurrentWorld == null) return false;

            int boardUnitCount = CurrentWorld.Boards[PlayerIndex].UnitCount;

            // 전투 인원 0명 검사
            if (boardUnitCount == 0)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_INGAME_CHAR_NOT_SET");
                return false;
            }

            // 전투 인원 최대 인원 미배치 검사
            int maxUnits = CurrentWorld.Economies[PlayerIndex].Level;
            if (boardUnitCount < maxUnits && benchIds.Count > 0)
            {
                var popupData = new SystemConfirmPopupData(
                    "UI_SYSTEM_ALERT", "SYSTEM_MSG_MAX_CHARACTER_ALERT", "UI_CONFIRM_BTN", "UI_CANCEL_BTN");
                var popup = await SceneUILayerManager.Instance
                    .PushUILayerAsync<SystemConfirmPopup>(popupData);
                var isConfirmed = await popup.WaitForExit();
                return isConfirmed is true;
            }

            return true;
        }

        // ── 추천 배치 ──

        private void OnRecommendClicked()
        {
            if (CurrentWorld == null) return;

            int maxUnits = CurrentWorld.Economies[PlayerIndex].Level;
            int boardCount = CurrentWorld.Boards[PlayerIndex].UnitCount;
            if (boardCount >= maxUnits) return;

            // 1. 보드 유닛 전부 회수 (멀티타일 중복 방지)
            var withdrawIds = new HashSet<int>();
            for (int i = 0; i < CurrentWorld.BoardSize; i++)
            {
                int entityId = CurrentWorld.BoardSlots[PlayerIndex][i];
                if (entityId != UnitData.InvalidId)
                    withdrawIds.Add(entityId);
            }
            foreach (var id in withdrawIds)
                ViewBridge?.SendCommand(GameCommand.WithdrawUnit(PlayerIndex, id));

            // 2. 전체 유닛 수집 (보드 + 벤치) → 필터 적용 → CP 내림차순 정렬
            var candidates = new List<(int entityId, float cp)>();

            foreach (var id in withdrawIds)
                TryAddCandidate(candidates, id);

            var benchSlots = CurrentWorld.BenchSlots[PlayerIndex];
            for (int i = 0; i < benchSlots.Length; i++)
            {
                int entityId = benchSlots[i];
                if (entityId != UnitData.InvalidId)
                    TryAddCandidate(candidates, entityId);
            }

            candidates.Sort((a, b) => b.cp.CompareTo(a.cp));

            // 3. 상위 N명 순차 배치
            int placed = 0;
            int boardWidth = CurrentWorld.BoardWidth;
            for (int i = 0; i < candidates.Count && placed < maxUnits; i++)
            {
                byte col = (byte)(placed % boardWidth);
                byte row = (byte)(placed / boardWidth);
                ViewBridge?.SendCommand(
                    GameCommand.PlaceUnit(PlayerIndex, candidates[i].entityId, col, row));
                placed++;
            }
        }

        private void TryAddCandidate(List<(int entityId, float cp)> list, int entityId)
        {
            if (!PassFilter(entityId)) return;
            ref var unit = ref CurrentWorld.GetUnit(entityId);
            var spec = SpecDataManager.Instance.GetSpecCharacter(unit.ChampionSpecId);
            list.Add((entityId, spec?.stat_atk ?? 0));
        }

        private void UpdateRecommendState()
        {
            if (_recommendObjOn == null || _recommendObjOff == null) return;
            if (CurrentWorld == null) return;

            int boardCount = CurrentWorld.Boards[PlayerIndex].UnitCount;
            int maxUnits = CurrentWorld.Economies[PlayerIndex].Level;
            bool canRecommend = boardCount != maxUnits;

            _recommendObjOn.SetActive(canRecommend);
            _recommendObjOff.SetActive(!canRecommend);
        }

        // ── 필터 ──

        private void OnFilterClicked()
        {
            var param = new FilterTooltipInIngamePopup.FilterParam(
                _selectedElementFilters,
                _selectedStellaFilters,
                ApplyFilter);
            SceneUILayerManager.Instance.PushUILayerAsync<FilterTooltipInIngamePopup>(param).Forget();
        }

        private void ApplyFilter(HashSet<SynergyType> elements, HashSet<SynergyType> stellas)
        {
            _selectedElementFilters = new HashSet<SynergyType>(elements);
            _selectedStellaFilters = new HashSet<SynergyType>(stellas);
            RefreshBenchDisplay();
        }

        protected override void FilterBenchIds()
        {
            if (_selectedElementFilters.Count == 0 && _selectedStellaFilters.Count == 0) return;

            for (int i = benchIds.Count - 1; i >= 0; i--)
            {
                if (!PassFilter(benchIds[i]))
                    benchIds.RemoveAt(i);
            }
        }

        private bool PassFilter(int entityId)
        {
            if (CurrentWorld == null) return true;

            ref var unit = ref CurrentWorld.GetUnit(entityId);
            var spec = SpecDataManager.Instance.GetSpecCharacter(unit.ChampionSpecId);
            if (spec == null) return true;

            bool passElement = _selectedElementFilters.Count == 0
                || _selectedElementFilters.Contains(spec.character_element_type);
            bool passStella = _selectedStellaFilters.Count == 0
                || _selectedStellaFilters.Contains(spec.character_stella_type);

            return passElement && passStella;
        }

        // ── 나가기 (전장 이탈 → BattleReady) ──

        protected override async UniTaskVoid OnExitClickedAsync()
        {
            var popupData = new SystemConfirmPopupData(
                "UI_SYSTEM_ALERT", "MSG_BATTLE_EXIT", "UI_CONFIRM_BTN", "UI_CANCEL_BTN");
            var popup = await SceneUILayerManager.Instance
                .PushUILayerAsync<SystemConfirmPopup>(popupData);
            var isConfirmed = await popup.WaitForExit();
            if (isConfirmed is not true) return;

            ViewBridge.ExitGame();

            int lastPlayStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
            var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);

            SceneTransition.Create<SceneTransition_FadeInOut>();
            await SceneTransition.FadeInAsync();
            SceneLoading.GoToNextScene("BattleReady", specLastStageData.chapter_id);
        }

        // ── 정리 ──

        protected override void OnCleanup()
        {
            _benchController?.Detach();
            _synergyController?.Detach();
            _startBattleButton?.onClick.RemoveListener(OnStartBattleClicked);
            _filterButton?.onClick.RemoveListener(OnFilterClicked);
            _recommendButton?.onClick.RemoveListener(OnRecommendClicked);
        }
    }
}
