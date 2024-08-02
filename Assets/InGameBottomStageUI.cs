using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InGameBottomStageUI : InGameBottomUI
{
    protected void Awake()
    {
        _startButton?.onClick.AddListener(OnStartButtonClicked);
        _statisticButton?.onClick.AddListener(OnClickStatisticButton);
    }
    
    protected override bool IsCheckStartBattle()
    {
        // 전투 인원 0명 검사
        if (InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count == 0)
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_INGAME_CHAR_NOT_SET");
            return false;
        }

        // 전투 인원 최대 인원 미배치 검사
        var userGrade = SpecDataManager.Instance.SpecUserGrade.Get(UserDataManager.Instance.UserBasicData.MaxSquadCount);
        if (InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count < userGrade.maximum_character_count)
        {
            bool isAvailableCharacter = _characterItemList.Exists(l => l.StatData != null);
            if (isAvailableCharacter)
            {
                string contentText = LanguageManager.Instance.GetLanguageText("SYSTEM_MSG_MAX_CHARACTER_ALERT");

                SystemConfirmPopupData newPopupData = new SystemConfirmPopupData();
                newPopupData.SetPopupData("시스템 알림", contentText, "확인", "취소", () => StartInGameBattle(_combatType));

                SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData).Forget();

                return false;
            }
        }

        // 지휘자 스킬 장착 확인
        if (_isOpenCommanderSkill)
        {
            var isEquippedCommanderSkill = UserDataManager.Instance.IsAllCommanderSkillsEquipped(_specUserGrade.maximum_commander_skill_count);
            if (!isEquippedCommanderSkill)
            {
                string contentText = LanguageManager.Instance.GetLanguageText("MSG_ALERT_EQUIP_COMMAND_SKILL");

                SystemConfirmPopupData newPopupData = new SystemConfirmPopupData();
                newPopupData.SetPopupData("시스템 알림", contentText, "확인", "취소", null);

                SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData).Forget();

                return false;
            }
        }

        return true;
    }
}
