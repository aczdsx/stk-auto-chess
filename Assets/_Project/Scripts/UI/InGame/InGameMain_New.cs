using CookApps.AutoChess;
using CookApps.AutoChess.View;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class InGameMain_New : UILayer
    {
        [Header("Auto Chess")]
        [SerializeField] private LocalSimulationRunner _runner;
        [SerializeField] private AutoChessViewBridge _viewBridge;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            StartAutoChess();
        }

        private void StartAutoChess()
        {
            if (_runner == null || _viewBridge == null)
            {
                Debug.LogError("[InGameMain_New] Runner 또는 ViewBridge가 할당되지 않았습니다.");
                return;
            }

            // 1. 시뮬레이션 시작 (SpecDataManager → ChampionPool/Synergy 주입 포함)
            _runner.StartSimulation();

            // 2. View 브릿지 초기화
            _viewBridge.Initialize(localPlayerIndex: 0);

            // 3. 테스트 유닛 생성 및 배치
            SpawnTestUnits();

            Debug.Log("[InGameMain_New] AutoChess started with test units.");
        }

        /// <summary>
        /// 테스트용 유닛 자동 생성.
        /// ChampionPool에서 사용 가능한 챔피언을 뽑아 각 플레이어 보드에 배치.
        /// </summary>
        private void SpawnTestUnits()
        {
            var world = _runner.GetWorld();
            if (world == null) return;

            int playerCount = world.Config.PlayerCount;
            int poolCount = world.Pool.SpecCount;

            if (poolCount == 0)
            {
                Debug.LogWarning("[InGameMain_New] ChampionPool이 비어있습니다. 하드코딩 ID로 생성합니다.");
                SpawnHardcodedUnits(world, playerCount);
                return;
            }

            // ChampionPool에서 순서대로 챔피언 ID를 가져와 배치
            // 플레이어당 3~4 유닛을 2열로 배치
            for (int p = 0; p < playerCount; p++)
            {
                int unitsToPlace = 3;
                int specOffset = p * unitsToPlace; // 각 플레이어가 다른 챔피언을 갖도록 오프셋

                for (int u = 0; u < unitsToPlace; u++)
                {
                    int specIdx = (specOffset + u) % poolCount;
                    int champId = world.Pool.Specs[specIdx].ChampionId;

                    int entityId = BoardSystem.CreateUnit(world, (byte)p, champId, 1);
                    if (entityId == UnitData.InvalidId) continue;

                    // 2열 배치: col = u, row = 0~1
                    byte col = (byte)(u % PlayerBoard.BoardWidth);
                    byte row = (byte)(u / PlayerBoard.BoardWidth);
                    BoardSystem.PlaceUnit(world, (byte)p, entityId, col, row);
                }

                Debug.Log($"[InGameMain_New] P{p}: {unitsToPlace} units placed from pool.");
            }
        }

        private void SpawnHardcodedUnits(GameWorld world, int playerCount)
        {
            // ChampionPool이 없을 때 fallback (챔피언 ID 1로 생성)
            for (int p = 0; p < playerCount; p++)
            {
                for (int u = 0; u < 3; u++)
                {
                    int entityId = BoardSystem.CreateUnit(world, (byte)p, 1, 1);
                    if (entityId == UnitData.InvalidId) continue;

                    byte col = (byte)u;
                    BoardSystem.PlaceUnit(world, (byte)p, entityId, col, 0);
                }
            }
        }
    }
}
