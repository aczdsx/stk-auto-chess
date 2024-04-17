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

        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            Instantiate(InGameResourceHolder.StagePrefab);
            InGameManager.Instance.StartInGame<FlowStateStageStart>(gridSize);
        }
    }
}
