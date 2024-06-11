#if !RELEASE || UNITY_EDITOR || ENABLE_CHEAT
using System;
using System.ComponentModel;
using CookApps.AutoBattler;
using CookApps.TeamBattle.UIManagements;

[Serializable]
public partial class SROptions
{
    #region 유저 정보 관련

    [Category("유저 정보 관련")]
    public void 유저가챠횟수초기화()
    {
        UserDataManager.Instance.UserBasicData.TotalGachaCount = 0;

        UserDataManager.Instance.SaveUserBasic();
    }

    [Category("유저 정보 관련")]
    public void 유저레벨데이터초기화()
    {
        UserDataManager.Instance.CheatResetUserLevelData();

        var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
        if (lobbyMain != null)
        {
            lobbyMain.RefreshUI(LobbyMainRefreshType.CHARACTER_LAYER);
        }
    }

    [Category("유저 정보 관련")]
    public void 유저경험치증가()
    {
        UserDataManager.Instance.AddUserLevelExp(추가유저경험치);

        var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
        if (lobbyMain != null)
        {
            lobbyMain.RefreshUI(LobbyMainRefreshType.CHARACTER_LAYER);
        }
    }

    [Category("유저 정보 관련")]
    public int 추가유저경험치 { get; set; } = 0;

    #endregion


    //////////////////////////////////////////////////////////////////////////////////

    #region 아이템 관련

    [Category("아이템 관련")]
    public void 아이템추가()
    {
        if (원하는아이템갯수 <= 0) return;

        UserDataManager.Instance.IncreaseItem(원하는아이템타입, 원하는아이템갯수, true);
    }

    [Category("아이템 관련")]
    public void 아이템제거()
    {
        if (원하는아이템갯수 <= 0) return;

        UserDataManager.Instance.DecreaseItem(원하는아이템타입, 원하는아이템갯수, true);
    }

    [Category("아이템 관련")]
    public ItemType 원하는아이템타입 { get; set; } = ItemType.GOLD;
    [Category("아이템 관련")]
    public int 원하는아이템갯수 { get; set; } = 0;

    #endregion

    //////////////////////////////////////////////////////////////////////////////////

    #region 캐릭터 관련

    [Category("캐릭터 관련")]
    public void 캐릭터획득()
    {
        if (원하는캐릭터ID <= 0) return;

        UserDataManager.Instance.AddNewCharacter(원하는캐릭터ID);
    }

    [Category("캐릭터 관련")]
    public void 캐릭터조각추가()
    {
        if (원하는캐릭터ID <= 0) return;
        if (원하는캐릭터조각갯수 <= 0) return;

        UserDataManager.Instance.IncreaseKnightPieceCount(원하는캐릭터ID, 원하는캐릭터조각갯수);
    }

    [Category("캐릭터 관련")]
    public int 원하는캐릭터ID { get; set; } = 0;

    [Category("캐릭터 관련")]
    public int 원하는캐릭터조각갯수 { get; set; } = 0;

    #endregion


    //////////////////////////////////////////////////////////////////////////////////

    #region 스테이지 관련

    [Category("스테이지 관련")]
    public void 스테이지클리어()
    {
        if (원하는스테이지ID <= 0) return;
        if (스테이지클리어별갯수 <= 0) return;

        UserDataManager.Instance.SetUserStage(원하는스테이지ID, 스테이지클리어별갯수);
    }

    [Category("스테이지 관련")]
    public int 원하는스테이지ID { get; set; } = 0;

    [Category("스테이지 관련")]
    public int 스테이지클리어별갯수 { get; set; } = 0;

    #endregion


    //////////////////////////////////////////////////////////////////////////////////

    #region 미션 관련

    [Category("미션 관련")]
    public void 현재가이드미션클리어()
    {
        var currentGuideMission = SpecDataManager.Instance.SpecGuideMission.Get(UserDataManager.Instance.UserMissionData.GuideMissionCurrentOrder);

        GuideMissionManager.Instance.ChangeGuideMissionState(currentGuideMission.guide_mission_type, MissionStateType.REWARD);
    }

    #endregion


}
#endif
