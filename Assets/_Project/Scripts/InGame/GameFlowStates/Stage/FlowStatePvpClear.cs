using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

public class FlowStatePvpClear : StateBase
{
    public override void StateInit(object target)
    {
    }

    public async override void StateStart()
    {
        var detailDeckData = InGameManager.Instance.UserPvpBattleDeckList;
        var simpleDeckData = PVPManager.Instance.ChangeDetailDataToSimpleData(detailDeckData);
        
        string resultSimpleData = BMUtil.ConvertToJsonSerialize(simpleDeckData);
        string gzipSimpleData = BMUtil.CompressStringToGzip(resultSimpleData);
        
        // 전투 종료 API
        var matchResultData = await PVPManager.Instance.SendMatchPVPBattleResult(PvpMatchResult.Win, detailDeckData.PlayerId, gzipSimpleData);
        
        InGameManager.Instance.EndInGame();
        
        SceneUILayerManager.Instance.PushUILayerAsync<ArenaPVPEndPopup>((true, detailDeckData, matchResultData));
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
