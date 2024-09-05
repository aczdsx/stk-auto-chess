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
        SceneUILayerManager.Instance.PushUILayerAsync<InGameResultPopup>((false, false, false, mvpCharacterData));

        // 행동력 소모 처리
        //UserDataManager.Instance.DecreaseItem(ItemType.AP, 0, InGameManager.Instance.SpecStage.need_ap, true, false);

        // 패배 카운트 증가
        if (InGameManager.Instance.AppEventReason != "exit")
            UserDataManager.Instance.AddUserStageLoseCount(true);
        
        // 상점 배너 팝업 체크
        ShopPurchaseManager.Instance.UpdateShopBannerConditionValue(ShopBannerConditionType.FIRST_STAGE_LOSE, 0, 1, false);
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
