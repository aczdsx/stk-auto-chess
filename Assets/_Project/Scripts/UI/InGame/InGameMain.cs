using CookApps.TeamBattle.BattleSystem;
using CookApps.TeamBattle.UIManagements;

namespace CookApps.SampleTeamBattle
{
    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/InGame/InGameMain.prefab")]
    public class InGameMain : UILayer
    {
        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            Instantiate(InGameResourceHolder.StagePrefab);
            InGameManager.Instance.StartInGame<FlowStateStageStart>();
        }
    }
}
