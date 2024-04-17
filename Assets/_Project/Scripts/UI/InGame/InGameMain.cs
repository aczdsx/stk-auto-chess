using System.Linq;
using CookApps.TeamBattle.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Unity.Mathematics;
using UnityEngine;

namespace CookApps.SampleTeamBattle
{
    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/InGame/InGameMain.prefab")]
    public class InGameMain : UILayer
    {
        [SerializeField] private int2 gridSize; // 임시. Spec에서 받아오거나 다른 방법으로 변경 필요
        [SerializeField] private InGameTileView[] tileViews;

        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            Instantiate(InGameResourceHolder.StagePrefab);
            var tileViews = this.tileViews.Select(x => x as IInGameTileView).ToArray();
            InGameManager.Instance.StartInGame<FlowStateStageStart>(gridSize, tileViews);
        }
    }
}
