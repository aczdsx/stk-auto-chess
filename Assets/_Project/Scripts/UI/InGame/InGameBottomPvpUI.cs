using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class InGameBottomPvpUI : InGameBottomCharacterUI
{
    protected void Awake()
    {
        _startButton?.onClick.AddListener(OnPvPSaveButtonClicked);
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
