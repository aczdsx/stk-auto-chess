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

        SceneUILayerManager.Instance.PushUILayerAsync<InGameDungeonTrialResultPopup>((true, _mvpCharacterData));
        
        // 가이드 미션 체크
        GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.CLEAR_TRIAL, InGameManager.Instance.SpecDungeonTrial.dungeon_id, 1);
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
