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

        // 패배 카운트 증가 및 다이얼로그 이벤트
        if (InGameManager.Instance.AppEventReason != "exit")
        {
            var stats = ClientStatisticsData.Get();
            stats.IncrementUserStageLoseCount();

            DialogueManager.Instance.UpdateDialogueEvent(
                DialogueEventType.FAIL,
                stats.UserStageLoseCount.ToString(),
                () =>
                {
                    if (stats.UserStageLoseCount == 1)
                        HandleFirstStageLoseAsync().Forget();
                });
        }

        // 상점 배너 팝업 체크
        ShopPurchaseManager.Instance.UpdateShopBannerConditionValue(ShopBannerConditionType.FIRST_STAGE_LOSE, 0, 1, false);
    }

    private static async UniTask HandleFirstStageLoseAsync()
    {
        var lastPlayStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
        var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);
        SceneTransition.Create<SceneTransition_FadeInOut>();
        await SceneTransition.FadeInAsync();

        SceneLoading.GoToNextScene("Lobby", specLastStageData.chapter_id);

        SceneUILayerManager.OnSceneLoadedEvent += OnSceneLoadedOpenCharacterCollection;
    }

    private static void OnSceneLoadedOpenCharacterCollection(string scenename)
    {
        if (scenename == "Lobby")
        {
            SceneUILayerManager.Instance.PushUILayerAsync<CharacterCollectionPopup>().Forget();
            ToastManager.Instance.ShowToastByTokenKey("MSG_GROWTH_CHARACTER");
            SceneUILayerManager.OnSceneLoadedEvent -= OnSceneLoadedOpenCharacterCollection;
        }
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
