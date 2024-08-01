using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

public class FlowStatePvpClear : StateBase
{
    public override void StateInit(object target)
    {
    }

    public override void StateStart()
    {
        //[TODO] pvp result pop 작업 필요
        InGameManager.Instance.EndInGame();
        
        SceneUILayerManager.Instance.PushUILayerAsync<ArenaPVPEndPopup>((true, InGameManager.Instance.UserPvpBattleDeckList));
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
