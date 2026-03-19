#if UNITY_EDITOR || (!RELEASE && ENABLE_CHEAT)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public partial class SROptions
{
    #region 시스템 관련

    [Category("시스템 관련")]
    public void 게임언어변경()
    {
        LanguageManager.Instance.SetLanguageAsync(원하는언어);

        ToastManager.Instance.ShowToast("TEST - 치트 적용");

        InGameManager.Instance.EndInGame();
        SceneTransition.Create<SceneTransition_FadeInOut>();
        SceneTransition.FadeInAsync().Forget();
        SceneLoading.GoToNextScene("Title");
    }

    private static readonly SystemLanguage[] 지원하는언어들 = { SystemLanguage.Korean, SystemLanguage.English };
    private SystemLanguage _원하는언어 = SystemLanguage.Korean;

    [Category("시스템 관련")]
    public SystemLanguage 원하는언어
    {
        get => _원하는언어;
        set => _원하는언어 = Array.IndexOf(지원하는언어들, value) >= 0 ? value : _원하는언어;
    }

    #endregion

    #region 계정 관련

    [Category("계정 관련")]
    public void 계정로그아웃()
    {
        LoginManager.Instance.LogOut();
    }

    #endregion

    #region 영지 테스트

    [Category("영지 테스트")]
    public void 영지_확장_테스트()
    {
        var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
        if (lobbyMain == null)
            return;

        var mainBlock = lobbyMain.MainBlock;
        mainBlock.AttachSubBlock(영지_확장_인덱스, true).Forget();
    }

    [Category("영지 테스트")]
    public int 영지_확장_인덱스 { get; set; } = 0;

    [Category("팝업 테스트")]
    public void 팝업_테스트()
    {
        SceneUILayerManager.Instance.PushUILayerAsync<CharacterCollectionPopup>(null).Forget();
    }


    [Category("팝업 테스트")]
    public void 리워드_팝업_테스트()
    {
        var rewardItemList = new List<RewardItem>
        {
            new (리워드_팝업_테스트_리워드ID, 1)
        };
        SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList)).Forget();
    }

    public int 리워드_팝업_테스트_리워드ID { get; set; } = 1001;

    #endregion

    #region UI 테스트

    [Category("UI 테스트")]
    public void SafeArea테스트()
    {
        var safeAreas = Object.FindObjectsByType<SafeArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < safeAreas.Length; i++)
        {
            safeAreas[i].Refresh();
        }

        var safeAreaMargins = Object.FindObjectsByType<SafeAreaMarginBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < safeAreaMargins.Length; i++)
        {
            safeAreaMargins[i].Refresh();
        }
    }

    [Category("UI 테스트")]
    public uint 가이드퀘스트_테스트ID { get; set; } = 1001;
    [Category("UI 테스트")]
    public void 가이드퀘스트_UI_테스트()
    {
        var guideMission = ServerDataManager.Instance.GuideMission;
        guideMission.SetGuideMission(new GuideMissionData()
        {
            GuideMissionId = 가이드퀘스트_테스트ID
        });
    }

    #endregion

    #region 치트

    [Category("치트")]
    public void 모든캐릭터획득()
    {
        EarnAllCharactersAsync().Forget();
    }

    private async UniTaskVoid EarnAllCharactersAsync()
    {
        var resp = await NetManager.Instance.Cheat.EarnAllCharactersAsync();
        if (resp != null && resp.IsSuccess)
        {
            ToastManager.Instance.ShowToast("모든 캐릭터 획득 완료");
        }
    }

    [Category("치트")]
    public CheatCurrencyType 재화변경_아이템 { get; set; } = CheatCurrencyType.Gold;

    [Category("치트")]
    public int 재화변경_수량 { get; set; } = 10000;

    [Category("치트")]
    public void 재화변경()
    {
        ChangeCurrencyAsync().Forget();
    }

    private async UniTaskVoid ChangeCurrencyAsync()
    {
        var delta = new Tech.Hive.V1.CurrencyDelta
        {
            ItemId = (uint)재화변경_아이템,
            Delta = 재화변경_수량
        };
        var resp = await NetManager.Instance.Cheat.ChangeCurrencyAsync(new[] { delta });
        if (resp != null && resp.IsSuccess)
        {
            ToastManager.Instance.ShowToast($"재화 변경 완료: {재화변경_아이템} += {재화변경_수량}");
        }
    }


    [Category("가이드 미션 stage")]
    public void 가이드미션()
    {
        ServerDataManager.Instance.GuideMission.AddActionValueAsync(GuideMissionType.CLEAR_STAGE, 스테이지아이디, 1).Forget();
    }
    [Category("가이드 미션 stage")]
    public int 스테이지아이디 { get; set; } = 20003;


    [Category("스테이지 자동 클리어")]
    public int 스테이지일괄클리어_목표ID { get; set; } = 10010;

    [Category("스테이지 자동 클리어")]
    public void 스테이지일괄클리어()
    {
        BatchClearStagesAsync().Forget();
    }

    private async UniTaskVoid BatchClearStagesAsync()
    {
        int targetStageId = 스테이지일괄클리어_목표ID;

        var allStages = SpecDataManager.Instance.StageInfo.All;
        var targetStages = new List<StageInfo>();

        for (int i = 0; i < allStages.Count; i++)
        {
            var stage = allStages[i];
            if (stage.stage_id <= targetStageId
                && !ServerDataManager.Instance.Battle.IsStageCleared((uint)stage.stage_id))
            {
                targetStages.Add(stage);
            }
        }

        targetStages.Sort((a, b) => a.stage_id.CompareTo(b.stage_id));

        if (targetStages.Count == 0)
        {
            ToastManager.Instance.ShowToast("클리어할 스테이지가 없습니다");
            return;
        }

        ToastManager.Instance.ShowToast($"스테이지 일괄 클리어 시작: {targetStages.Count}개");

        int clearedCount = 0;
        int failedCount = 0;

        for (int i = 0; i < targetStages.Count; i++)
        {
            var stage = targetStages[i];
            try
            {
                var inGameParams = await NetManager.Instance.Battle.StartAsync(
                    stage.chapter_id, stage.stage_id, 0, Array.Empty<string>());

                if (inGameParams == null)
                {
                    failedCount++;
                    Debug.LogError($"[치트] 스테이지 {stage.stage_id} 시작 실패");
                    continue;
                }

                var battleResult = new Tech.Hive.V1.BattleResult
                {
                    IsVictory = true,
                    Stars = 3,
                    ClearTime = 1000
                };

                var resp = await NetManager.Instance.Battle.EndAsync(inGameParams.SessionId, battleResult);
                if (resp != null && resp.IsSuccess)
                {
                    clearedCount++;
                }
                else
                {
                    failedCount++;
                    Debug.LogError($"[치트] 스테이지 {stage.stage_id} 종료 실패");
                }
            }
            catch (Exception e)
            {
                failedCount++;
                Debug.LogError($"[치트] 스테이지 {stage.stage_id} 클리어 실패: {e.Message}");
            }
        }

        ToastManager.Instance.ShowToast($"일괄 클리어 완료: 성공 {clearedCount}, 실패 {failedCount}");
    }


    #endregion

    #region Idle 전투

    private bool _idleCombatDebugGUI;

    [Category("Idle 전투")]
    public bool Idle전투_디버그GUI
    {
        get => _idleCombatDebugGUI;
        set => _idleCombatDebugGUI = value;
    }

    [Category("Idle 전투")]
    public bool 아군무적
    {
        get => CookApps.AutoChess.DamageSystem.PlayerInvincible;
        set => CookApps.AutoChess.DamageSystem.PlayerInvincible = value;
    }

    #endregion
}
#endif