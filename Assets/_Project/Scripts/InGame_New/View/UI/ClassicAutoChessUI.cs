using CookApps.AutoBattler;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.Utility;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private Button _startBattleButton;

        private TableViewController<int, BenchUnitSlot> _benchController;
        private TableViewController<int, InGameSynergyUI> _synergyController;

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
        }

        private void OnStartBattleClicked()
        {
            var cmd = GameCommand.Ready(PlayerIndex);
            ViewBridge?.SendCommand(cmd);
        }

        protected override void OnCleanup()
        {
            _benchController?.Detach();
            _synergyController?.Detach();
            _startBattleButton?.onClick.RemoveListener(OnStartBattleClicked);
        }
    }
}
