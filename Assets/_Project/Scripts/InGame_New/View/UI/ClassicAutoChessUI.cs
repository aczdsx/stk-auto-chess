using System.Collections.Generic;
using System.Threading;
using CookApps.AutoBattler;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using CookApps.BattleSystem;
using TMPro;
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

        [Header("Kill Log")]
        [SerializeField] private Transform _killLogRoot;
        [SerializeField] private InGameKillLogItem_New _killLogItemPrefab;

        [Header("Classic Mode")]
        [SerializeField] private CAButton _startBattleButton;
        [SerializeField] private CAButton _filterButton;
        [SerializeField] private CAButton _recommendButton;
        [SerializeField] private GameObject _recommendObjOn;
        [SerializeField] private GameObject _recommendObjOff;
        [SerializeField] private CAButton _presetButton;
        [SerializeField] private TMP_Text _myTeamCpText;
        [SerializeField] private TMP_Text _enemyTeamCpText;
        private bool _presetLoading;

        private TableViewController<int, BenchUnitSlot> _benchController;
        private TableViewController<int, InGameSynergyUI> _synergyController;
        private bool _isStarting;

        private readonly List<InGameKillLogItem_New> _killLogItems = new();
        private const float KillLogGapY = 40f;

        private HashSet<SynergyType> _selectedElementFilters = new();
        private HashSet<SynergyType> _selectedStellaFilters = new();
        private readonly HashSet<int> _calcBoardCpVisited = new();
        private int _lastMyCp = -1;
        private int _lastEnemyCp = -1;

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
            _presetButton?.onClick.AddListener(OnPresetClicked);

            // 스테이지 이름 설정
            var stageInfo = SpecDataManager.Instance.GetStageData(InGameParams.StageId);
            if (stageInfo != null)
            {
                string stageString = LanguageManager.Instance.GetDefaultText("UI_STAGE");
                SetStageName($"{stageString} {stageInfo.chapter_id}-{stageInfo.stage_number}");
            }
        }

        protected override void OnSyncState(GameWorld world)
        {
            UpdateRecommendState();
            UpdateTeamCp();
        }

        // ── 전투 시작 ──

        private void OnStartBattleClicked()
            => OnStartBattleClickedAsync().Forget();

        private async UniTaskVoid OnStartBattleClickedAsync()
        {
            if (_isStarting) return;
            _isStarting = true;

            try
            {
                var ct = this.GetCancellationTokenOnDestroy();
                if (!await IsCheckStartBattle().AttachExternalCancellation(ct))
                {
                    _isStarting = false;
                    return;
                }

                var cmd = GameCommand.Ready(PlayerIndex);
                ViewBridge?.SendCommand(cmd);
            }
            catch (System.OperationCanceledException)
            {
                // 오브젝트 파괴로 인한 취소 — 무시
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
            int cp = CombatPowerCalculator.Calculate(ref unit);
            list.Add((entityId, cp));
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

        private void UpdateTeamCp()
        {
            if (CurrentWorld == null) return;

            if (_myTeamCpText != null)
            {
                int myCp = CalcBoardCp(PlayerIndex);
                if (_lastMyCp != myCp)
                {
                    _lastMyCp = myCp;
                    _myTeamCpText.text = myCp.ToString("n0");
                }
            }

            if (_enemyTeamCpText != null)
            {
                int enemyCp = CalcEnemyCp();
                if (_lastEnemyCp != enemyCp)
                {
                    _lastEnemyCp = enemyCp;
                    _enemyTeamCpText.text = enemyCp.ToString("n0");
                }
            }
        }

        private int CalcBoardCp(byte playerIndex)
        {
            if (CurrentWorld.BoardSlots == null
                || playerIndex >= CurrentWorld.BoardSlots.Length
                || CurrentWorld.BoardSlots[playerIndex] == null)
                return 0;

            int totalCp = 0;
            var boardSlots = CurrentWorld.BoardSlots[playerIndex];
            _calcBoardCpVisited.Clear();

            for (int i = 0; i < boardSlots.Length; i++)
            {
                int entityId = boardSlots[i];
                if (entityId == UnitData.InvalidId || !_calcBoardCpVisited.Add(entityId)) continue;

                ref var unit = ref CurrentWorld.GetUnit(entityId);
                totalCp += CombatPowerCalculator.Calculate(ref unit);
            }

            return totalCp;
        }

        private int CalcEnemyCp()
        {
            // PvE 적: BoardSlots가 아닌 PvEEnemies에서 계산
            if (CurrentWorld.PvEEnemies != null && CurrentWorld.PvEEnemyCount > 0)
            {
                int totalCp = 0;
                for (int i = 0; i < CurrentWorld.PvEEnemyCount; i++)
                {
                    ref var enemy = ref CurrentWorld.PvEEnemies[i];
                    totalCp += CombatPowerCalculator.Calculate(ref enemy);
                }
                return totalCp;
            }

            // PvP 적: 상대 보드에서 계산
            byte enemyIndex = (byte)(PlayerIndex == 0 ? 1 : 0);
            return CalcBoardCp(enemyIndex);
        }

        // ── 프리셋 ──

        private void OnPresetClicked()
        {
            var existing = SceneUILayerManager.Instance.GetUILayer<PresetInGamePopup>();
            if (existing != null)
            {
                SceneUILayerManager.Instance.PopUILayer(existing);
                return;
            }

            if (_presetLoading) return;
            OpenPresetAsync().Forget();
        }

        private async UniTaskVoid OpenPresetAsync()
        {
            _presetLoading = true;
            var param = new PresetInGamePopup.PresetPopupParam
            {
                ViewBridge = ViewBridge,
                GetWorld = () => CurrentWorld,
                PlayerIndex = PlayerIndex,
            };
            await SceneUILayerManager.Instance.PushUILayerAsync<PresetInGamePopup>(param);
            _presetLoading = false;
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
            if (_selectedElementFilters.Count > 0 || _selectedStellaFilters.Count > 0)
            {
                for (int i = benchIds.Count - 1; i >= 0; i--)
                {
                    if (!PassFilter(benchIds[i]))
                        benchIds.RemoveAt(i);
                }
            }

            // CP 내림차순 정렬
            if (CurrentWorld != null && benchIds.Count > 1)
            {
                benchIds.Sort((a, b) =>
                {
                    ref var unitA = ref CurrentWorld.GetUnit(a);
                    ref var unitB = ref CurrentWorld.GetUnit(b);
                    int cpA = CombatPowerCalculator.Calculate(ref unitA);
                    int cpB = CombatPowerCalculator.Calculate(ref unitB);
                    return cpB.CompareTo(cpA);
                });
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

        // ── 킬로그 ──

        public override void OnUnitDied(int victimEntityId, int killerEntityId, GameWorld world)
        {
            if (_killLogItemPrefab == null || _killLogRoot == null)
            {
                Debug.Log($"[KillLog] SKIP: prefab={_killLogItemPrefab != null}, root={_killLogRoot != null}");
                return;
            }
            if (killerEntityId == CombatUnit.InvalidId)
            {
                Debug.Log($"[KillLog] SKIP: killerEntityId=InvalidId, victim={victimEntityId}");
                return;
            }

            var (killerChampSpecId, killerIsPlayerSide) = FindCombatUnitInfo(world, killerEntityId);
            var (victimChampSpecId, _) = FindCombatUnitInfo(world, victimEntityId);
            if (killerChampSpecId == 0 || victimChampSpecId == 0)
            {
                Debug.Log($"[KillLog] SKIP: killerSpec={killerChampSpecId}, victimSpec={victimChampSpecId}, killerId={killerEntityId}, victimId={victimEntityId}");
                return;
            }

            Debug.Log($"[KillLog] ADD: killer={killerChampSpecId}(isPlayer={killerIsPlayerSide}), victim={victimChampSpecId}");
            AddKillLog(killerChampSpecId, victimChampSpecId, killerIsPlayerSide);
        }

        private (int champSpecId, bool isPlayerSide) FindCombatUnitInfo(GameWorld world, int entityId)
        {
            for (int m = 0; m < GameWorld.MaxCombatMatches; m++)
            {
                var matchState = world.CombatMatchStates[m];
                if (matchState == null) continue;
                for (int u = 0; u < matchState.UnitCount; u++)
                {
                    ref var unit = ref matchState.Units[u];
                    // CombatId를 우선 체크 (UnitDied 이벤트는 CombatId로 전달)
                    // SourceEntityId와 CombatId가 충돌하면 잘못된 유닛 반환 방지
                    if (unit.CombatId == entityId || unit.SourceEntityId == entityId)
                        return (unit.ChampionSpecId, unit.TeamIndex == 0);
                }
            }
            return (0, false);
        }

        private void AddKillLog(int killerChampSpecId, int victimChampSpecId, bool isPlayerKill)
        {
            var item = Instantiate(_killLogItemPrefab, _killLogRoot);
            item.transform.position = _killLogRoot.position;
            item.SetData(killerChampSpecId, victimChampSpecId, isPlayerKill);
            item.transform.SetAsFirstSibling();
            item.OnDespawn = HandleKillLogDespawn;
            _killLogItems.Insert(0, item);
            RelayoutKillLogs(animated: true);
        }

        private void HandleKillLogDespawn(InGameKillLogItem_New item)
        {
            item.OnDespawn = null;
            _killLogItems.Remove(item);
            RelayoutKillLogs(animated: true);
        }

        private CancellationTokenSource _killLogLayoutCts;

        private void RelayoutKillLogs(bool animated = false)
            => RelayoutKillLogsAsync(animated).Forget();

        private async UniTaskVoid RelayoutKillLogsAsync(bool animated = false)
        {
            if (_killLogRoot == null) return;

            _killLogLayoutCts?.Cancel();
            _killLogLayoutCts = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy());
            var token = _killLogLayoutCts.Token;

            var items = _killLogItems;
            float currentY = 0f;

            // 즉시 배치
            if (!animated)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var rt = items[i].RectTransform;
                    if (rt == null) continue;
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -currentY);
                    currentY += items[i].Height + KillLogGapY;
                }
                return;
            }

            // 슬라이드 애니메이션
            var startPositions = new Vector2[items.Count];
            var targetPositions = new Vector2[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                var rt = items[i].RectTransform;
                if (rt == null) continue;
                startPositions[i] = rt.anchoredPosition;
                targetPositions[i] = new Vector2(rt.anchoredPosition.x, -currentY);
                currentY += items[i].Height + KillLogGapY;
            }

            const float slideDuration = 0.2f;
            float elapsed = 0f;

            while (elapsed < slideDuration)
            {
                if (token.IsCancellationRequested) return;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                t = 1f - Mathf.Pow(1f - t, 3f); // easeOutCubic

                for (int i = 0; i < items.Count; i++)
                {
                    var rt = items[i].RectTransform;
                    if (rt == null) continue;
                    rt.anchoredPosition = Vector2.LerpUnclamped(startPositions[i], targetPositions[i], t);
                }

                await UniTask.Yield(token).SuppressCancellationThrow();
                if (token.IsCancellationRequested) return;
            }

            // 최종 위치 보정
            for (int i = 0; i < items.Count; i++)
            {
                var rt = items[i].RectTransform;
                if (rt == null) continue;
                rt.anchoredPosition = targetPositions[i];
            }
        }

        // ── 정리 ──

        protected override void OnCleanup()
        {
            _benchController?.Detach();
            _synergyController?.Detach();
            _startBattleButton?.onClick.RemoveListener(OnStartBattleClicked);
            _filterButton?.onClick.RemoveListener(OnFilterClicked);
            _recommendButton?.onClick.RemoveListener(OnRecommendClicked);
            _presetButton?.onClick.RemoveListener(OnPresetClicked);
        }
    }
}
