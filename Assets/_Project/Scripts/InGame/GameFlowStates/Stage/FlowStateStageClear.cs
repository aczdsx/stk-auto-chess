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

        var gdb = new GuideMissionDataBridge();

        // ! GUIDE_TODO
        // ! 301	4	CLEAR_STAGE	GUIDE_MISSION_NAME_301	스테이지 1_1 클리어	20004	GUIDE_MISSION_DESC_301	20001	1	GOLD	210001	200											
        // ! 302	5	CLEAR_STAGE	GUIDE_MISSION_NAME_302	스테이지 1_2 클리어	20005	GUIDE_MISSION_DESC_302	20002	1	GOLD	210001	200											
        // ! 303	6	CLEAR_STAGE	GUIDE_MISSION_NAME_303	스테이지 1_3 클리어	0	GUIDE_MISSION_DESC_303	20003	1	GOLD	210001	200											
        // ! 304	7	CLEAR_STAGE	GUIDE_MISSION_NAME_304	스테이지 1_4 클리어	20005	GUIDE_MISSION_DESC_304	20004	1	GOLD	210001	200											
        // ! 306	9	CLEAR_STAGE	GUIDE_MISSION_NAME_306	스테이지 1_5 클리어	0	GUIDE_MISSION_DESC_306	20005	1	GOLD	210001	200											
        // ! 307	10	CLEAR_STAGE	GUIDE_MISSION_NAME_307	스테이지 1_6 클리어	0	GUIDE_MISSION_DESC_307	20006	1	GOLD	210001	200											
        // ! 308	11	CLEAR_STAGE	GUIDE_MISSION_NAME_308	스테이지 1_7 클리어	0	GUIDE_MISSION_DESC_308	20007	1	GOLD	210001	200											
        // ! 309	12	CLEAR_STAGE	GUIDE_MISSION_NAME_309	스테이지 1_8 클리어	0	GUIDE_MISSION_DESC_309	20008	1	GOLD	210001	200											
        // ! 310	13	CLEAR_STAGE	GUIDE_MISSION_NAME_310	스테이지 1_9 클리어	20007	GUIDE_MISSION_DESC_310	20009	1	GOLD	210001	200											
        // ! 502	22	CLEAR_STAGE	GUIDE_MISSION_NAME_502	스테이지 2-1 클리어 가이드 미션	0	GUIDE_MISSION_DESC_502	30001	1	GOLD	210001	200											
        // ! 503	23	CLEAR_STAGE	GUIDE_MISSION_NAME_503	스테이지 2-2 클리어 가이드 미션	30008	GUIDE_MISSION_DESC_503	30002	1	GOLD	210001	200											
        // ! 504	24	CLEAR_STAGE	GUIDE_MISSION_NAME_504	스테이지 2-5 클리어 가이드 미션	0	GUIDE_MISSION_DESC_504	30005	1	GOLD	210001	200											
        // ! 507	27	CLEAR_STAGE	GUIDE_MISSION_NAME_507	스테이지 2-6 클리어 가이드 미션 제공	30010	GUIDE_MISSION_DESC_507	30006	1	GOLD	210001	200											
        // ! 508	28	CLEAR_STAGE	GUIDE_MISSION_NAME_508	스테이지 2-9 클리어 가이드 미션 제공	0	GUIDE_MISSION_DESC_508	30009	1	GOLD	210001	200											
        // ! 509	29	CLEAR_STAGE	GUIDE_MISSION_NAME_509	스테이지 2-12 클리어 가이드 미션 제공	30011	GUIDE_MISSION_DESC_509	30012	1	GOLD	210001	200											
        // ! CLEAR_STAGE


        await gdb.AddActionAsync(GuideMissionType.CLEAR_STAGE, 1, InGameManager.Instance.SpecStage.stage_id);

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
