#if !RELEASE || UNITY_EDITOR || ENABLE_CHEAT
using System;
using System.ComponentModel;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
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

    #endregion

    #region UI 테스트

    [Category("UI 테스트")]
    public void SafeArea테스트()
    {
        var safeAreas = Object.FindObjectsByType<SafeArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < safeAreas.Length; i++)
        {
            safeAreas[i].Refresh(true);
        }

        var safeAreaMargins = Object.FindObjectsByType<SafeAreaMarginBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < safeAreaMargins.Length; i++)
        {
            safeAreaMargins[i].Refresh(true);
        }
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

    [Category("가이드 미션 stage")]
    public void 가이드미션()
    {
        var gdb = new GuideMissionDataBridge();
        gdb.AddActionAsync(GuideMissionType.CLEAR_STAGE, 1, 스테이지아이디).Forget();
    }
    [Category("가이드 미션 stage")]
    public int 스테이지아이디 { get; set; } = 20003;

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

    #endregion
}
#endif