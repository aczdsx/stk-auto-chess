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
        var _mvpCharacterData = SpecDataManager.Instance.GetCharacterData(InGameStatistics.Instance.GetMvpID());
        InGameManager.Instance.EndInGame();

        SceneUILayerManager.Instance.PushUILayerAsync<InGameDungeonResultPopup>((true, _mvpCharacterData));
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
