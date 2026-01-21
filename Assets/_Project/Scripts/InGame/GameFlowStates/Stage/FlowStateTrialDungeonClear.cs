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

        // 서버에 시련 던전 클리어 결과 전송
        var resp = await NetManager.Instance.TrialDungeon.ClearAsync(InGameManager.Instance.BattleSessionId, true);
        InGameManager.Instance.EndInGame();

        // 서버 응답 후 결과 팝업 표시
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
