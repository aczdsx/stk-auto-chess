using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

public class FlowStateStageFail : StateBase
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
        // 서버에 스테이지 패배 결과 전송
        await SendEndAsync();
        InGameManager.Instance.EndInGame();

        // 결과 팝업 표시
        var mvpCharacterData = SpecDataManager.Instance.GetCharacterData(InGameStatistics.Instance.GetMvpID());
        InGameResultPopupParam param = new InGameResultPopupParam(false, false, false, mvpCharacterData, null);
        SceneUILayerManager.Instance.PushUILayerAsync<InGameResultPopup>(param);

        // 행동력 소모 처리
        //UserDataManager.Instance.DecreaseItem(ItemType.AP, 0, InGameManager.Instance.SpecStage.need_ap, true, false);

        // 패배 카운트 증가
        if (InGameManager.Instance.AppEventReason != "exit")
            UserDataManager.Instance.AddUserStageLoseCount(true);

        // 상점 배너 팝업 체크
        ShopPurchaseManager.Instance.UpdateShopBannerConditionValue(ShopBannerConditionType.FIRST_STAGE_LOSE, 0, 1, false);
    }

    private async UniTask SendEndAsync()
    {
        var battleResult = new BattleResult
        {
            IsVictory = false,
            Stars = 0,
            ClearTime = 0
        };
        await NetManager.Instance.Battle.EndAsync(InGameManager.Instance.BattleSessionId, battleResult);
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
