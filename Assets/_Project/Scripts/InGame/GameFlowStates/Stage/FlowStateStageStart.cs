using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;

public class FlowStateStageStart : StateBase
{
    public override void StateInit(object target)
    {
    }

    public override async void StateStart()
    {
        InGameMain.GetInGameMain().PlaySceneAnimation("SetBattleEntry");
        InGameObjectManager.Instance.ClearSynergyFx();
        InGameMainFlowManager.Instance.AddNextState<FlowStateStageCombat>();
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
