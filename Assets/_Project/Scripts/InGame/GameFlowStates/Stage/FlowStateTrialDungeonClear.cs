using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

public class FlowStateTrialDungeonClear : StateBase
{
    public override void StateInit(object target)
    {
    }

    public override void StateStart()
    {
        StateStartAsync().Forget();
    }

    private async UniTaskVoid StateStartAsync()
    {
        var _mvpCharacterData = SpecDataManager.Instance.GetCharacterData(InGameStatistics.Instance.GetMvpID());

        var resp = await NetManager.Instance.TrialDungeon.ClearAsync(InGameManager.Instance.BattleSessionId, true);
        InGameManager.Instance.EndInGame();

        var gdb = new GuideMissionDataBridge();
        await gdb.AddActionAsync(GuideMissionType.CLEAR_BABEL, 1, InGameManager.Instance.SpecDungeonTrial.dungeon_id);
        
        var param = new InGameDungeonTrialResultPopupParam(true, _mvpCharacterData, resp.Rewards);
        SceneUILayerManager.Instance.PushUILayerAsync<InGameDungeonTrialResultPopup>(param).Forget();
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
