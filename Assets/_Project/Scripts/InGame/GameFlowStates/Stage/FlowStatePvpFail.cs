using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

public class FlowStatePvpFail : StateBase
{
    public override void StateInit(object target)
    {
    }

    public override async void StateStart()
    {
        if (InGameObjectManager.Instance.StartingPlayerCharacters.Count > 0)
        {
            var detailDeckData = InGameManager.Instance.UserPvpBattleDeckList;
            var simpleDeckData = PVPManager.Instance.ChangeDetailDataToSimpleData(detailDeckData);

            var resultSimpleData = BMUtil.ConvertToJsonSerialize(simpleDeckData);
            //string gzipSimpleData = BMUtil.CompressStringToGzip(resultSimpleData);

            var isRevenge = string.IsNullOrEmpty(detailDeckData.MatchId) == false;
            // 전투 종료 API
            PvpMatchResponse matchResultData = null;
            if (isRevenge)
                matchResultData = await PVPManager.Instance.SendMatchPVPRevengeResult(PvpMatchResult.RevengeLose, detailDeckData.PlayerId, resultSimpleData,
                    detailDeckData.MatchId);
            else
                matchResultData = await PVPManager.Instance.SendMatchPVPBattleResult(PvpMatchResult.Lose, detailDeckData.PlayerId, resultSimpleData);
            InGameManager.Instance.EndInGame();

            SceneUILayerManager.Instance.PushUILayerAsync<ArenaPVPEndPopup>((false, detailDeckData, matchResultData));

            // 가이드 미션 체크
            GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.PLAY_PVP, 0, 1);

            // 퀘스트 데이터 갱신
            UserDataManager.Instance.SetUserQuestActionCount(QuestType.BATTLE_PVP, 1, true, true);
        }
        else
        {
            InGameManager.Instance.EndInGame();
            var lastPlayStageID = UserDataManager.Instance.GetLastPlayStageID();
            var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);
            var transition = SceneTransition_FadeInOut.Create();
            await SceneLoading.GoToNextScene("Lobby", (int)specLastStageData.chapter_id, transition);
        }
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}