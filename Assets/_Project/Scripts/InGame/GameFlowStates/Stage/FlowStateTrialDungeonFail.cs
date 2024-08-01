using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

public class FlowStateTrialDungeonFail : StateBase
{

    public override void StateInit(object target)
    {
    }

    public override void StateStart()
    {
        InGameManager.Instance.EndInGame();
        SpecCharacter mvpCharacterData = null;
        
        SceneUILayerManager.Instance.PushUILayerAsync<InGameDungeonTrialResultPopup>((false, mvpCharacterData));
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
