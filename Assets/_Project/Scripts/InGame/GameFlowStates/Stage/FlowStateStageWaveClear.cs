using CookApps.TeamBattle.BattleSystem;

public class FlowStateStageWaveClear : StateBase
{
    public override void StateInit(object target)
    {
    }

    public override void StateStart()
    {
        // Wave 클리어! UI 출력

        if (false) // 다음 Wave가 있다면 WaveStart로 이동
        {
            InGameMainFlowManager.Instance.AddNextState<FlowStateStageWaveStart>();
        }
        else // 없다면 StageClear로 이동
        {
            InGameMainFlowManager.Instance.AddNextState<FlowStateStageClear>();
        }
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
