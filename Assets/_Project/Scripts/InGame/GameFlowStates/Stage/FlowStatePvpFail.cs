using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

public class FlowStatePvpFail : StateBase
{

    public override void StateInit(object target)
    {
    }

    public async override void StateStart()
    {
        var detailDeckData = InGameManager.Instance.UserPvpBattleDeckList;
        var simpleDeckData = PVPManager.Instance.ChangeDetailDataToSimpleData(detailDeckData);
        
        string resultSimpleData = BMUtil.ConvertToJsonSerialize(simpleDeckData);
        //string gzipSimpleData = BMUtil.CompressStringToGzip(resultSimpleData);

        bool isRevenge = string.IsNullOrEmpty(detailDeckData.MatchId) == false;
        
        // 전투 종료 API
        MatchPvpResponse matchResultData = null;
        if (isRevenge)
        {
            matchResultData = await PVPManager.Instance.SendMatchPVPRevengeResult(PvpMatchResult.RevengeLose, detailDeckData.PlayerId, resultSimpleData, detailDeckData.MatchId);
        }
        else
        {
            matchResultData = await PVPManager.Instance.SendMatchPVPBattleResult(PvpMatchResult.Lose, detailDeckData.PlayerId, resultSimpleData);
        }
        
        // PVP 패배 팝업 노출
        InGameManager.Instance.EndInGame();
        
        SceneUILayerManager.Instance.PushUILayerAsync<ArenaPVPEndPopup>((false, detailDeckData, matchResultData));
        
        GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.PLAY_PVP, 0, 1);
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
