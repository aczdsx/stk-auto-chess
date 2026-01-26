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
        StateStartAsync().Forget();
    }

    private async UniTaskVoid StateStartAsync()
    {
        var _mvpCharacterData = SpecDataManager.Instance.GetCharacterData(InGameStatistics.Instance.GetMvpID());

        var resp = await NetManager.Instance.TrialDungeon.ClearAsync(InGameManager.Instance.BattleSessionId, true);
        InGameManager.Instance.EndInGame();

        var gdb = new GuideMissionDataBridge();
        // ! GUIDE_TODO
        // ! 505	25	CLEAR_BABEL	GUIDE_MISSION_NAME_505	바벨던전 1층 클리어 하기	30009	GUIDE_MISSION_DESC_505	10001	1	GOLD	210001	200											
        // ! 506	26	CLEAR_BABEL	GUIDE_MISSION_NAME_506	바벨던전 4층 클리어 하기	0	GUIDE_MISSION_DESC_506	10004	1	GOLD	210001	200											
        // ! 601	30	CLEAR_BABEL	GUIDE_MISSION_NAME_601	바벨던전 13층 클리어 하기	0	GUIDE_MISSION_DESC_601	10013	1	GOLD	210001	200											
        // ! CLEAR_BABEL
        if(GuideMissionConstants.바벨범위기준ID <= InGameManager.Instance.SpecDungeonTrial.dungeon_id)
        {
            if(GuideMissionTestUtility.CLEAR_BABEL_TOWER_GUIDE_ID.Contains(InGameManager.Instance.SpecDungeonTrial.dungeon_id)) 
                await gdb.AddActionAsync(GuideMissionType.CLEAR_BABEL, 1, InGameManager.Instance.SpecDungeonTrial.dungeon_id);
        }

        await NetManager.Instance.TrialDungeon.GetAsync(); // 데이터 갱신 필요해서 추가
        
        var param = new InGameDungeonTrialResultPopupParam(true, _mvpCharacterData, resp.Rewards);
        SceneUILayerManager.Instance.PushUILayerAsync<InGameDungeonTrialResultPopup>(param).Forget();
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
