using CookApps.TeamBattle.BattleSystem;

public class FlowStateStageWaveFail : StateBase
{
    public override void StateInit(object target)
    {
    }

    public override void StateStart()
    {
        // Wave 실패! UI 출력

        // Wave 실패 UI 닫히면 StageFailState로 이동
        InGameMainFlowManager.Instance.AddNextState<FlowStateStageFail>();
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
