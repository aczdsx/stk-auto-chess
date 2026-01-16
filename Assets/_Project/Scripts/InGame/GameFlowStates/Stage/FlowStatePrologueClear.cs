using System;
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
        var firstStage = SpecDataManager.Instance.StageInfo.All[0];
        var task = NetManager.Instance.Battle.StartAsync(firstStage.chapter_id, firstStage.stage_id, (int)InGameType.STAGE, Array.Empty<string>());
        await SceneTransition.FadeInAsync();
        var inGameParams = await task;
        SceneLoading.GoToNextSceneWithSpecialTrigger("InGame", "PrologueEnd", inGameParams);
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
