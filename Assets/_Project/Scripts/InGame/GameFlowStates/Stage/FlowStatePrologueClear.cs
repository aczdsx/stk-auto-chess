using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

public class FlowStatePrologueClear : StateBase
{
    public override void StateInit(object target)
    {
    }

    public override void StateStart()
    {
        StateStartAsync().Forget();
    }

    public async UniTask StateStartAsync()
    {
        InGameManager.Instance.EndInGame();
        SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
        await SceneTransition.FadeInAsync();
        SceneLoading.GoToNextSceneWithSpecialTrigger("InGame", "PrologueEnd",
            (InGameType.STAGE, (IGameStateUICore)new InGameMainStateStage(), 10001));
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
