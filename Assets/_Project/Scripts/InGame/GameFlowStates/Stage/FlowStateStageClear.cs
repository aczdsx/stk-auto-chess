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
        int star = 1;
        if (InGameMain.GetInGameMain().InGameTime >= 30)
            star++;
        if (InGameObjectManager.Instance.IsCheckAllPlayerCharacterAlive())
            star++;

        SceneUILayerManager.Instance.PushUILayerAsync<InGameResultPopup>((true, star));

        // 행동력 소모 처리
        UserDataManager.Instance.DecreaseItem(ItemType.AP, 0, InGameManager.Instance.SpecStage.need_ap, true);

        // 다이얼로그 체크
        DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.STAGE_CLEAR, InGameManager.Instance.SpecStage.stage_id.ToString());
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
