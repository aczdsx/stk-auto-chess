#if !RELEASE || UNITY_EDITOR
using System;
using System.ComponentModel;
using CookApps.AutoBattler;
using CookApps.TeamBattle.UIManagements;
using UnityEngine.Tilemaps;

[Serializable]
public partial class SROptions
{
    #region 유저 정보 관련

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
        UserDataManager.Instance.IncreaseItem(원하는아이템타입, 원하는아이템갯수, true);
    }

    [Category("아이템 관련")]
    public void 아이템제거()
    {
        UserDataManager.Instance.DecreaseItem(원하는아이템타입, 원하는아이템갯수, true);
    }

    [Category("아이템 관련")]
    public ItemType 원하는아이템타입 { get; set; } = ItemType.GOLD;
    [Category("아이템 관련")]
    public int 원하는아이템갯수 { get; set; } = 0;

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
