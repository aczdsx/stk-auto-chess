using CookApps.TeamBattle.BattleSystem;
using CookApps.TeamBattle.UIManagements;

namespace CookApps.SampleTeamBattle
{
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
