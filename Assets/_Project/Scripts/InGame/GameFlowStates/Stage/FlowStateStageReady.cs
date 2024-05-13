using CookApps.BattleSystem;

public class FlowStateStageReady : StateBase
{
    //[TODO] 캐릭터 배치 관련 로직 추가
    public override void StateInit(object target)
    {
    }

    public override void StateStart()
    {
        // 캐릭터 배치
        // UI에서 Start 버튼을 눌렀을 때 Start로 이동
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
