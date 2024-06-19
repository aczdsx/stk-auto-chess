using CookApps.BattleSystem;

// 현재는 사용 x -> 웨이브 개념 들어갈 때 사용 예정.
public class FlowStateStageWaveStart : StateBase
{
    public override void StateInit(object target)
    {
    }

    public override void StateStart()
    {
        // 웨이브 n 시작! UI 출력

        // 적들 등장

        // 다 등장하면 CombatState으로 전환
        // InGameMainFlowManager.Instance.AddNextState<FlowStateStageCombat>();
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
