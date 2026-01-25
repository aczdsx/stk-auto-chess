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

        await NetManager.Instance.TrialDungeon.GetAsync(); // 데이터 갱신 필요해서 추가
        
        var param = new InGameDungeonTrialResultPopupParam(true, _mvpCharacterData, resp.Rewards);
        SceneUILayerManager.Instance.PushUILayerAsync<InGameDungeonTrialResultPopup>(param).Forget();
        GuideMissionTestUtility.HandleClearStage(InGameManager.Instance.SpecDungeonTrial.dungeon_id, false).Forget();
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
