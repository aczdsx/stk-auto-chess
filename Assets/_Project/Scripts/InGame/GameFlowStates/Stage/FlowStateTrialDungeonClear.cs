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
        var _mvpCharacterData = SpecDataManager.Instance.GetCharacterData(InGameStatistics.Instance.GetMvpID());
        InGameManager.Instance.EndInGame();
        bool start2 = InGameMain.GetInGameMain().InGameTime >= 30;
        bool start3 = InGameObjectManager.Instance.IsCheckAllPlayerCharacterAlive();

        SceneUILayerManager.Instance.PushUILayerAsync<InGameDungeonResultPopup>((true, start2, start3, _mvpCharacterData));

        // 다이얼로그 체크
        DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.STAGE_CLEAR, InGameManager.Instance.SpecStage.stage_id.ToString());

        // 행동력 소모 처리
        UserDataManager.Instance.DecreaseItem(ItemType.AP, 0, InGameManager.Instance.SpecStage.need_ap, true, false);

        // 행동력 소모 이벤트 처리 (todo.. 추후 상황에 따라 DecreaseItem 함수 내부로 이동 가능)
        UserDataManager.Instance.SetUserEventActionCount(EventType.USE_AP, InGameManager.Instance.SpecStage.need_ap, true, true);
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
