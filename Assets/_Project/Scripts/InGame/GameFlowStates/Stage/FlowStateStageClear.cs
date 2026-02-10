using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

public class FlowStateStageClear : StateBase
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
        bool star2 = InGameMain.GetInGameMain().InGameTime >= 30;
        bool star3 = InGameObjectManager.Instance.IsCheckAllPlayerCharacterAlive();

        // 서버에 스테이지 클리어 결과 전송
        uint stars = 1; // 승리 기본 1별
        if (star2) stars++;
        if (star3) stars++;
        ulong clearTimeMs = (ulong)((60 - InGameMain.GetInGameMain().InGameTime) * 1000);
        var resp = await SendEndAsync(stars, clearTimeMs);

        InGameManager.Instance.EndInGame();

        // 다이얼로그 체크
        DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.STAGE_CLEAR, InGameManager.Instance.SpecStage.stage_id.ToString());

        // await ServerDataManager.Instance.GuideMission.AddActionValueAsync(GuideMissionType.CLEAR_STAGE, InGameManager.Instance.SpecStage.stage_id, 1);

        // 서버 응답 후 결과 팝업 표시
        InGameResultPopupParam param = new InGameResultPopupParam(true, star2, star3, _mvpCharacterData, (IReadOnlyList<Reward>)resp.Rewards);
        SceneUILayerManager.Instance.PushUILayerAsync<InGameResultPopup>(param);

        // 다이얼로그 체크
        DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.STAGE_CLEAR, InGameManager.Instance.SpecStage.stage_id.ToString());
        

        // 상점 배너 팝업 체크
        ShopPurchaseManager.Instance.UpdateShopBannerConditionValue(ShopBannerConditionType.STAGE_CLEAR, InGameManager.Instance.SpecStage.stage_id, 1, false);
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }

    private async UniTask<BattleEndResponse> SendEndAsync(uint stars, ulong clearTimeMs)
    {
        var battleResult = new BattleResult
        {
            IsVictory = true,
            Stars = stars,
            ClearTime = clearTimeMs
        };
        return await NetManager.Instance.Battle.EndAsync(InGameManager.Instance.BattleSessionId, battleResult);
    }
}
