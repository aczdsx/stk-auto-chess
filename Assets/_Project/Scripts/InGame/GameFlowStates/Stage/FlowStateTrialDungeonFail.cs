using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

public class FlowStateTrialDungeonFail : StateBase
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
        InGameManager.Instance.EndInGame();
        CharacterInfo mvpCharacterData = null;

        // 서버에 시련 던전 실패 결과 전송
        var resp = await NetManager.Instance.TrialDungeon.ClearAsync(InGameManager.Instance.BattleSessionId, false);

        // 서버 응답 후 결과 팝업 표시
        var param = new InGameDungeonTrialResultPopupParam(false, mvpCharacterData, resp.Rewards);
        SceneUILayerManager.Instance.PushUILayerAsync<InGameDungeonTrialResultPopup>(param).Forget();
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
