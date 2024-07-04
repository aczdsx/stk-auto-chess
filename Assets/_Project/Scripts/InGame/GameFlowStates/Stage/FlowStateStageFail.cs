using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

public class FlowStateStageFail : StateBase
{

    public override void StateInit(object target)
    {
    }

    public override void StateStart()
    {
        InGameManager.Instance.EndInGame();
        SpecCharacter mvpCharacterData = null;
        SceneUILayerManager.Instance.PushUILayerAsync<InGameResultPopup>((false, 0, mvpCharacterData));

        // 행동력 소모 처리
        UserDataManager.Instance.DecreaseItem(ItemType.AP, 0, InGameManager.Instance.SpecStage.need_ap, true, false);
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
