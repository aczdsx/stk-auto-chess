#if !RELEASE || UNITY_EDITOR || ENABLE_CHEAT
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cookapps.Stkauto.V1;
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
        LanguageManager.Instance.SetGameLanguage(원하는언어);

        ToastManager.Instance.ShowToast("TEST - 치트 적용");

        InGameManager.Instance.EndInGame();
        SceneTransition.Create<SceneTransition_FadeInOut>();
        SceneTransition.FadeInAsync().Forget();
        SceneLoading.GoToNextScene("Title");
    }

    [Category("시스템 관련")]
    public LanguageType 원하는언어 { get; set; } = LanguageType.KR;

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

    [Category("캐릭터 테스트")]
    public void 유저캐릭터가라로생성하기우주가보이면눌러주세요()
    {
        UserDataManager.Instance.CreateAllCharacter();
    }

    [Category("캐릭터 테스트")]
    public void 특정_캐릭터_생성()
    {
        UserDataManager.Instance.CreateCharacterByID(캐릭터_ID);
    }

    [Category("캐릭터 테스트")]
    public int 캐릭터_ID { get; set; } = 117513401;

    

    #endregion
}
#endif