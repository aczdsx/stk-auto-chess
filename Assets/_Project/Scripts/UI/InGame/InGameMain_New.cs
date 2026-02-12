using CookApps.AutoChess;
using CookApps.AutoChess.View;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class InGameMain_New : UILayer
    {
        private AutoChessViewRoot _viewRoot;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            StartAutoChess().Forget();
        }

        private async UniTask StartAutoChess()
        {
            int stageId = 1; // TODO: param에서 추출

            // ViewRoot 생성 → 리소스 로드 → 초기화
            var rootObj = new GameObject("AutoChessRoot");
            _viewRoot = rootObj.AddComponent<AutoChessViewRoot>();
            await _viewRoot.LoadResources(stageId);
            _viewRoot.Initialize();

            // 시뮬레이션 시작
            _viewRoot.Runner.StartSimulation();

            // View 브릿지 초기화
            _viewRoot.ViewBridge.Initialize(localPlayerIndex: 0);

            // 테스트 유닛
            SpawnTestUnits(_viewRoot.Runner.GetWorld());

            Debug.Log("[InGameMain_New] AutoChess started with test units.");
        }

        protected override void OnPostExit()
        {
            _viewRoot?.Cleanup();
            base.OnPostExit();
        }

        /// <summary>
        /// 테스트용 유닛 자동 생성.
        /// ChampionPool에서 사용 가능한 챔피언을 뽑아 각 플레이어 보드에 배치.
        /// </summary>
        private void SpawnTestUnits(GameWorld world)
        {
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
