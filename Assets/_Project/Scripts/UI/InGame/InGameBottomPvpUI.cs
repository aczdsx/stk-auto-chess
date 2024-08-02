using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

public class InGameBottomPvpUI : InGameBottomUI
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
        var userLevelData =
            SpecDataManager.Instance.SpecAccountLevelExp.Get(UserDataManager.Instance.UserBasicData.Level);
        
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
        
        // PVP 티켓 소모
        UserDataManager.Instance.DecreaseItem(ItemType.PVP_TICKET, 0, 1, true, false);

        return true;
    }
}
