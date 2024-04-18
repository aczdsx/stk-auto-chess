using CookApps.TeamBattle.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

public class FlowStateStageClear : StateBase
{
    public override void StateInit(object target)
    {
    }

    public override void StateStart()
    {
        // Stage 클리어! UI 출력
        // 보상 출력

        // 로비로 이동
        InGameManager.Instance.EndInGame();
        SceneLoading.GoToNextScene("Lobby").Forget();
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
