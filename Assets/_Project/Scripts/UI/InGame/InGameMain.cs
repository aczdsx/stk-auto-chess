using System.Linq;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace CookApps.AutoBattler
{
    class InGameData
    {
        private int stageId;
        // char///
    }
    
    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/InGame/InGameMain.prefab")]
    public class InGameMain : UILayer
    {
        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            LoadResources().Forget();
        }

        private async UniTask  LoadResources()
        {
            InGameHpBarViewPool.Instance.InitializePool();
            
            GameObject stageObj = Instantiate(InGameResourceHolder.StagePrefab);
            if (!stageObj.TryGetComponent(out InGameStage stage))
            {
                Debug.LogError("InGameStage is not found");
                return;
            }
            var tileViews = stage.TileViews.Select(x => x as IInGameTileView).ToArray();
            InGameGrid grid = new InGameGrid(stage.GridSize, tileViews);
            InGameObjectManager.Instance.Initialize(grid);
            
            CharacterStatData statData1 = new CharacterStatData(30001, 10);
            CharacterStatData statData2 = new CharacterStatData(30002, 10);
            await InGameObjectManager.Instance.AddCharacterToField(statData2, new int2(3, 3), AllianceType.Enemy, typeof(CharacterStateIdle));
            await InGameObjectManager.Instance.AddCharacterToField(statData1, new int2(1, 1), AllianceType.Player, typeof(CharacterStateIdle));
            
            
            InGameManager.Instance.StartInGame<FlowStateStageStart>();
        }
    }
}
