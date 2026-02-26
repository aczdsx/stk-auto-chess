using CookApps.TeamBattle.UI;
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
        [SerializeField] private BenchUnitSlot slotPrefab;

        [Header("Classic Mode")]
        [SerializeField] private Button startBattleButton;

        private TableViewController<int, BenchUnitSlot> tableViewController;

        protected override void OnInitialize()
        {
            tableViewController = tableView.CreateController<int, BenchUnitSlot>()
                .WithData(benchIds)
                .WithCellPrefab(slotPrefab.gameObject)
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

            startBattleButton?.onClick.AddListener(OnStartBattleClicked);
        }

        private void OnStartBattleClicked()
        {
            var cmd = GameCommand.Ready(PlayerIndex);
            ViewBridge?.SendCommand(cmd);
        }

        protected override void OnCleanup()
        {
            startBattleButton?.onClick.RemoveListener(OnStartBattleClicked);
        }
    }
}
