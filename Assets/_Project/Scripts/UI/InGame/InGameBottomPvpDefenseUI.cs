using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InGameBottomPvpDefenseUI : InGameBottomUI
{
    [SerializeField] protected InGameObstacleItem _ingameObstacleItemPrefab;
    [SerializeField] protected Transform _inGameObstacleItemTransform;
    [SerializeField] protected GameObject _obstacleListBody;
    [SerializeField] protected CAButton _changeButton;
    [SerializeField] protected GameObject _obstacleTipObj;
    
    private List<InGameObstacleItem> _obstacleItemList = new List<InGameObstacleItem>();

    protected void Awake()
    {
        _startButton?.onClick.AddListener(OnPvPSaveButtonClicked);
        _changeButton?.onClick.AddListener(OnChangeButtonClicked);
    }

    public void InitData(List<UserPVPObstacleBattleDeck> obstacleBattleDecks)
    {
        base.InitData();
        List<int> obstacleIDs = new List<int>();
        obstacleIDs.Add(100001);
        obstacleIDs.Add(100001);
        obstacleIDs.Add(100001);

        foreach (var obstacleID in obstacleIDs)
        {
            // SpecDataManager.Instance.GetSpecObstacleList(obstacleID);
        }
    }
    
    protected override bool IsCheckStartBattle()
    {
        return false;
    }
    
    private void OnPvPSaveButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
        
        // string contentText = LanguageManager.Instance.GetLanguageText("MSG_ALERT_EQUIP_COMMAND_SKILL");
        string contentText = "방어덱을 저장하시겠습니까?(TEST)";

        SystemConfirmPopupData newPopupData = new SystemConfirmPopupData();
        newPopupData.SetPopupData("시스템 알림", contentText, "확인", "취소", () =>
        {
            PvPSaveProcess().Forget();
        });

        SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData).Forget();
    }
    
    private void OnChangeButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
        
        _obstacleListBody.SetActive(!_obstacleListBody.activeSelf);
        _characterListBody.SetActive(!_characterListBody.activeSelf);
        
        _obstacleTipObj.SetActive(!_obstacleTipObj.activeSelf);
        _characterTipObj.SetActive(!_characterTipObj.activeSelf);
    }
    
    private async UniTask PvPSaveProcess()
    {
        var characterControllers = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);
        double attrText = InGameObjectManager.Instance.GetAttr(AllianceType.Player);
        
        await PVPManager.Instance.SavePVPProfileData((int)attrText, characterControllers);

        InGameManager.Instance.EndInGame();
        int lastPlayStageID = UserDataManager.Instance.GetLastPlayStageID();
        var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);
        var transition = SceneTransition_FadeInOut.Create();
        await SceneLoading.GoToNextScene("Lobby",  (int)specLastStageData.chapter_id, transition);
    }
}
