using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

public class FlowStatePvpClear : StateBase
{
    public override void StateInit(object target)
    {
    }

    public override async void StateStart()
    {
        var detailDeckData = InGameManager.Instance.UserPvpBattleDeckList;
        var simpleDeckData = PVPManager.Instance.ChangeDetailDataToSimpleData(detailDeckData);

        var resultSimpleData = BMUtil.ConvertToJsonSerialize(simpleDeckData);
        //string gzipSimpleData = BMUtil.CompressStringToGzip(resultSimpleData);

        var isRevenge = string.IsNullOrEmpty(detailDeckData.MatchId) == false;

        // 전투 종료 API
        PvpMatchResponse matchResultData = null;
        if (isRevenge)
            matchResultData =
                await PVPManager.Instance.SendMatchPVPRevengeResult(PvpMatchResult.RevengeWin, detailDeckData.PlayerId, resultSimpleData,
                    detailDeckData.MatchId);
        else
            matchResultData = await PVPManager.Instance.SendMatchPVPBattleResult(PvpMatchResult.Win, detailDeckData.PlayerId, resultSimpleData);

        InGameManager.Instance.EndInGame();

        SceneUILayerManager.Instance.PushUILayerAsync<ArenaPVPEndPopup>((true, detailDeckData, matchResultData));

        // 가이드 미션 체크
        GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.PLAY_PVP, 0, 1);

        // 퀘스트 데이터 갱신
        UserDataManager.Instance.SetUserQuestActionCount(QuestType.BATTLE_PVP, 1, true, true);
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}