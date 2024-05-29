using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

public class FlowStateStageClear : StateBase
{
    public override void StateInit(object target)
    {
    }

    public override void StateStart()
    {
        InGameManager.Instance.EndInGame();
        SceneUILayerManager.Instance.PushUILayerAsync<InGameResultPopup>(true);
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
