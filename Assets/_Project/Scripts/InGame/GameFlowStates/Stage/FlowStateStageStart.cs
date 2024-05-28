using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;

public class FlowStateStageStart : StateBase
{
    public override void StateInit(object target)
    {
    }

    public override async void StateStart()
    {
        // 스테이지 시작! UI 출력
        // var uiLayer = await SceneUILayerManager.Instance.PushUILayerAsync<StageStartOverlay>();
        // await uiLayer.WaitForExit();

        // 캐릭터 선택 UI 없애기
        // InGameMain.GetInGameMain().HideCharacterSelectUI();

        // 닫히면 내 캐릭터 등장

        // 다 등장하면 WaveStart로 이동
        InGameMainFlowManager.Instance.AddNextState<FlowStateStageWaveStart>();
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
