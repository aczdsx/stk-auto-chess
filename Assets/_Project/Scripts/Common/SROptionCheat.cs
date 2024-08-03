#if !RELEASE || UNITY_EDITOR || ENABLE_CHEAT
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

[Serializable]
public partial class SROptions
{
    #region 유저 정보 관련

    [Category("유저 정보 관련")]
    public void 유저계정레벨최대()
    {
        UserDataManager.Instance.UserBasicData.Level = SpecDataManager.Instance.GetAccountMaxLevel();
        UserDataManager.Instance.UserBasicData.Exp = 18600;

        UserDataManager.Instance.SaveUserBasic();
    }

    [Category("유저 정보 관련")]
    public void 가이드미션진행상태초기화()
    {
        UserDataManager.Instance.UserMissionData.GuideMissionCurrentOrder = 1;
        UserDataManager.Instance.UserMissionData.UserGuideMissions.Clear();

        UserDataManager.Instance.SaveUserMissionData();
    }

    [Category("유저 정보 관련")]
    public void 다이얼로그히스토리초기화()
    {
        UserDataManager.Instance.UserMissionData.UserDialogueGroupIds.Clear();

        UserDataManager.Instance.SaveUserMissionData();
    }

    [Category("유저 정보 관련")]
    public void 유저가챠횟수초기화()
    {
        UserDataManager.Instance.UserBasicData.TotalGachaCount = 0;

        UserDataManager.Instance.SaveUserBasic();
    }

    [Category("유저 정보 관련")]
    public void 캐릭터리셋횟수초기화()
    {
        UserDataManager.Instance.SetResetCharacterCount(0, false, true);
    }

    [Category("유저 정보 관련")]
    public void 유저레벨데이터초기화()
    {
        UserDataManager.Instance.CheatResetUserLevelData();

        var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
        if (lobbyMain != null) lobbyMain.RefreshUI(LobbyMainRefreshType.CHARACTER_LAYER);
    }

    [Category("유저 정보 관련")]
    public void 유저경험치증가()
    {
        UserDataManager.Instance.PrevAccountLevel = UserDataManager.Instance.UserBasicData.Level;

        UserDataManager.Instance.AddUserLevelExp(추가유저경험치);

        var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
        if (lobbyMain != null) lobbyMain.RefreshUI(LobbyMainRefreshType.CHARACTER_LAYER);
    }

    [Category("유저 정보 관련")] public int 추가유저경험치 { get; set; } = 0;

    #endregion


    //////////////////////////////////////////////////////////////////////////////////

    #region 아이템 관련

    [Category("아이템 관련")]
    public void 아이템추가()
    {
        if (원하는아이템갯수 <= 0) return;

        UserDataManager.Instance.IncreaseItem(원하는아이템타입, 0, 원하는아이템갯수, true, true);
    }

    [Category("아이템 관련")]
    public void 아이템제거()
    {
        if (원하는아이템갯수 <= 0) return;

        UserDataManager.Instance.DecreaseItem(원하는아이템타입, 0, 원하는아이템갯수, true, true);
    }

    [Category("아이템 관련")] public ItemType 원하는아이템타입 { get; set; } = ItemType.GOLD;
    [Category("아이템 관련")] public int 원하는아이템갯수 { get; set; } = 0;

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
    public void 모든캐릭터획득()
    {
        // UserDataManager.Instance.AddAllCharacters();
    }


    [Category("캐릭터 관련")]
    public void 캐릭터조각추가()
    {
        if (원하는캐릭터ID <= 0) return;
        if (원하는캐릭터조각갯수 <= 0) return;

        UserDataManager.Instance.IncreaseKnightPieceCount(원하는캐릭터ID, 원하는캐릭터조각갯수);
    }

    [Category("캐릭터 관련")]
    public void 모든캐릭터최대레벨설정()
    {
        var userCharacterList = SpecDataManager.Instance.GetCharacterListByCharacterType(CharacterType.CHARACTER);

        var maxLevel = SpecDataManager.Instance.GetCharacterMaxLevel();

        foreach (var userCharacter in userCharacterList)
        {
            if (UserDataManager.Instance.IsHaveCharacter(userCharacter.character_id) == false)
                UserDataManager.Instance.AddNewCharacter(userCharacter.character_id);

            UserDataManager.Instance.SetCharacterLevel(userCharacter.character_id, maxLevel);
        }
    }

    [Category("캐릭터 관련")]
    public void 캐릭터레벨설정()
    {
        if (원하는캐릭터ID <= 0) return;
        if (원하는캐릭터레벨 <= 0) return;

        UserDataManager.Instance.SetCharacterLevel(원하는캐릭터ID, 원하는캐릭터레벨);
    }

    [Category("캐릭터 관련")]
    public void 캐릭터초월레벨설정()
    {
        if (원하는캐릭터ID <= 0) return;
        if (원하는캐릭터초월레벨 <= 0) return;

        UserDataManager.Instance.SetTranscendenceLevel(원하는캐릭터ID, 원하는캐릭터초월레벨);
    }

    [Category("캐릭터 관련")] public int 원하는캐릭터ID { get; set; } = 0;

    [Category("캐릭터 관련")] public int 원하는캐릭터조각갯수 { get; set; } = 0;
    [Category("캐릭터 관련")] public int 원하는캐릭터레벨 { get; set; } = 0;
    [Category("캐릭터 관련")] public int 원하는캐릭터초월레벨 { get; set; } = 0;

    #endregion


    //////////////////////////////////////////////////////////////////////////////////

    #region 스테이지 관련

    [Category("스테이지 관련")]
    public void 스테이지클리어()
    {
        if (원하는스테이지ID <= 0) return;
        if (스테이지클리어별갯수 <= 0) return;

        var targetSpecStageData = SpecDataManager.Instance.GetStageData(원하는스테이지ID);

        // 행동력 검사
        if (!UserDataManager.Instance.CheckEnoughItem(ItemType.AP, 0, targetSpecStageData.need_ap, false))
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_GUIDE_IDLE_REWARD_AP");
            return;
        }

        // 스테이지 데이터저장
        UserDataManager.Instance.SetUserStage(원하는스테이지ID, 스테이지클리어별갯수);
        GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.CLEAR_STAGE, 원하는스테이지ID, 1);

        // 보상 지급
        var rewardList = SpecDataManager.Instance.GetSpecStageReward(targetSpecStageData.reward_id)
            .FindAll(l => l.difficulty_type == targetSpecStageData.difficulty_type);

        var rewardItemList = SpecDataManager.Instance.GetRewardItemListByStageRewardList(rewardList);

        // 보상 데이터 저장
        if (rewardList.Count > 0) UserDataManager.Instance.IncreaseRewardItemList(rewardItemList, true);

        SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(rewardItemList).Forget();

        // 행동력 소모 처리
        UserDataManager.Instance.DecreaseItem(ItemType.AP, 0, targetSpecStageData.need_ap, true, false);

        var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
        if (lobbyMain != null)
        {
            lobbyMain.RefreshUI(LobbyMainRefreshType.STAGE);
            lobbyMain.RefreshUI(LobbyMainRefreshType.GUIDE_MISSION);
        }
    }

    [Category("스테이지 관련")]
    public void 튜토리얼스테이지클리어()
    {
        var tutoStageDataList = SpecDataManager.Instance.GetStageList(1);
        foreach (var stageData in tutoStageDataList)
        {
            UserDataManager.Instance.SetUserStage(stageData.stage_id, 3);
            GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.CLEAR_STAGE, stageData.stage_id,
                1);
        }
    }

    [Category("스테이지 관련")] public int 원하는스테이지ID { get; set; } = 0;

    [Category("스테이지 관련")] public int 스테이지클리어별갯수 { get; set; } = 0;

    #endregion


    //////////////////////////////////////////////////////////////////////////////////

    #region 이벤트 관련

    // [Category("이벤트 관련")]
    // public void 이벤트전체초기화()
    // {
    //
    // }

    [Category("이벤트 관련")]
    public void 타겟ID이벤트초기화()
    {
        if (대상이벤트ID == 0) return;

        UserDataManager.Instance.ResetEventData(대상이벤트ID, true);
    }

    [Category("이벤트 관련")]
    public void 타겟이벤트액션카운트변경()
    {
        if (대상이벤트ID == 0) return;

        UserDataManager.Instance.SetUserEventActionCount(대상이벤트ID, 이벤트완료횟수, false, true);
    }

    [Category("이벤트 관련")] public int 대상이벤트ID { get; set; } = 0;

    [Category("이벤트 관련")] public int 이벤트완료횟수 { get; set; } = 0;

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////

    #region 미션 관련

    [Category("미션 관련")]
    public void 모든가이드미션클리어()
    {
        var allGuideMissionList = SpecDataManager.Instance.SpecGuideMission.All.ToList();

        foreach (var guideMission in allGuideMissionList)
        {
            if (guideMission.id >= 30)
            {
                UserDataManager.Instance.SetGuideMissionState(guideMission.guide_mission_type, guideMission.sub_key,
                    MissionStateType.NONE);
                if(guideMission.id == 30)
                    UserDataManager.Instance.SetGuideMissionState(guideMission.guide_mission_type, guideMission.sub_key,
                        MissionStateType.REWARD);
            }
            else
            {
                UserDataManager.Instance.SetGuideMissionState(guideMission.guide_mission_type, guideMission.sub_key,
                    MissionStateType.CLEAR);
            }
        }
    }

    [Category("미션 관련")]
    public void 현재가이드미션클리어()
    {
        var currentGuideMission =
            SpecDataManager.Instance.SpecGuideMission.Get(UserDataManager.Instance.UserMissionData
                .GuideMissionCurrentOrder);

        GuideMissionManager.Instance.ChangeGuideMissionState(currentGuideMission.guide_mission_type,
            currentGuideMission.sub_key, MissionStateType.REWARD);
    }

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////

    #region 퀘스트 관련

    [Category("퀘스트 관련")]
    public void 일일퀘스트전체초기화()
    {
        UserDataManager.Instance.ResetQuestDataList(TermType.DAILY);
    }

    [Category("퀘스트 관련")]
    public void 주간퀘스트전체초기화()
    {
        UserDataManager.Instance.ResetQuestDataList(TermType.WEEKLY);
    }

    [Category("퀘스트 관련")]
    public void 타겟퀘스트액션카운트변경()
    {
        if (대상퀘스트ID == 0) return;

        UserDataManager.Instance.SetUserQuestActionCount(대상퀘스트ID, 퀘스트완료횟수, false, true);

        // 퀘스트 팝업 UI 갱신
        var questPopup = SceneUILayerManager.Instance.GetUILayer("QuestPopup");
        if (questPopup != null) questPopup.GetComponent<QuestPopup>()?.RefreshPopup();
    }

    [Category("퀘스트 관련")]
    public void 타겟퀘스트상태변경()
    {
        if (대상퀘스트ID == 0) return;

        UserDataManager.Instance.SetUserQuestState(대상퀘스트ID, 퀘스트상태, true);

        // 퀘스트 팝업 UI 갱신
        var questPopup = SceneUILayerManager.Instance.GetUILayer("QuestPopup");
        if (questPopup != null) questPopup.GetComponent<QuestPopup>()?.RefreshPopup();
    }

    [Category("퀘스트 관련")] public int 대상퀘스트ID { get; set; } = 0;

    [Category("퀘스트 관련")] public int 퀘스트완료횟수 { get; set; } = 0;

    [Category("퀘스트 관련")] public QuestStateType 퀘스트상태 { get; set; } = 0;

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////

    #region PVP 관련

    [Category("PVP 관련")]
    public async void 유저PVP전체프로필초기화()
    {
        // 심플 정보 세팅
        UserPVPBattleSimpleData simpleData = new UserPVPBattleSimpleData();
        simpleData.PlayerId = UserDataManager.Instance.UserBasicData.PlayerId;
        simpleData.ServerId = UserDataManager.Instance.UserBasicData.ServerId;
        simpleData.RankId = UserDataManager.Instance.UserPVP.RankId;
        simpleData.RankPoint = UserDataManager.Instance.UserPVP.RankPoint;
        simpleData.Ranking = UserDataManager.Instance.UserPVP.Ranking;
        simpleData.Nickname = UserDataManager.Instance.UserBasicData.Nickname;
        simpleData.PlayerLv = UserDataManager.Instance.UserBasicData.Level;
        var serializedSimpleData = BMUtil.ConvertToJsonSerialize(simpleData);
            
        // 디테일 정보 세팅
        UserPVPBattleDetailData detailData = new UserPVPBattleDetailData();
        detailData.PlayerId = UserDataManager.Instance.UserBasicData.PlayerId;
        detailData.ServerId = UserDataManager.Instance.UserBasicData.ServerId;
        detailData.RankId = UserDataManager.Instance.UserPVP.RankId;
        detailData.RankPoint = UserDataManager.Instance.UserPVP.RankPoint;
        detailData.Ranking = UserDataManager.Instance.UserPVP.Ranking;
        detailData.Nickname = UserDataManager.Instance.UserBasicData.Nickname;
        detailData.PlayerLv = UserDataManager.Instance.UserBasicData.Level;
        //detailData.BattlePoint =

        detailData.PvpDeckList = new UserPVPBattleDeckList();
        var serializedDetailData = BMUtil.ConvertToJsonSerialize(detailData);
            
        var response = await GrpcGame.GameGrpcManager.Instance.UpdatePvpProfile(0, serializedSimpleData, serializedDetailData);
        if (response.IsError)
            return;
    }

    #endregion
    
    ////////////////////////////////////////////////////////////////////////////////////////
    
    #region 스킬 관련

    [Category("스킬 관련")]
    public void 지휘자스킬전체획득()
    {
        var allCommanderSkillList = SpecDataManager.Instance.SpecCommanderSkill.All;

        allCommanderSkillList.GroupBy(data => data.commander_skill_id);

        foreach (var skill in allCommanderSkillList)
            UserDataManager.Instance.AddCommanderSkillData(skill.commander_skill_id, false);

        UserDataManager.Instance.SaveUserCommanderSKillData();
    }

    #endregion
    
    ////////////////////////////////////////////////////////////////////////////////////////
    
    #region 던전 관련

    [Category("던전 관련")]
    public void 타겟던전클리어()
    {
        UserDataManager.Instance.SetTrialDungeonData(대상던전ID, DungeonStateType.CLEAR, true);
        
        ToastManager.Instance.ShowToast("치트 - 사용완료");
    }
    
    [Category("던전 관련")] public int 대상던전ID { get; set; } = 0;

    #endregion
}
#endif