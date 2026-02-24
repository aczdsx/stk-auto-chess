using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;

public class InGameBottomStageUI : InGameBottomUI
{
    protected override void Awake()
    {
        base.Awake();
        _startButton?.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnStartButtonClickedAsync(), AwaitOperation.Drop).AddTo(this);
        _statisticButton?.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickStatisticButton()).AddTo(this);
        _recommendButton?.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickRecommend()).AddTo(this);
        _speedUpButton?.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickSpeedUp()).AddTo(this);
        
        _tabCharacterButton?.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickTabCharacterButton()).AddTo(this);
        _tabBattleItemButton?.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickTabBattleItemButton()).AddTo(this);
    }
    
    protected override async UniTask<bool> IsCheckStartBattle()
    {
        // 전투 인원 0명 검사
        if (InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count == 0)
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_INGAME_CHAR_NOT_SET");
            return false;
        }

        // 전투 인원 최대 인원 미배치 검사
        var userKnightCount = SpecDataManager.Instance.GetUserKnightCountByNestCount();
        if (userKnightCount != null && InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Count < userKnightCount.maximum_character_count)
        {
            bool isAvailableCharacter = _characterItemList.Exists(l => l.StatData != null);
            if (isAvailableCharacter)
            {
                var newPopupData = new SystemConfirmPopupData("시스템 알림", "SYSTEM_MSG_MAX_CHARACTER_ALERT", "확인", "취소");
                var popup = await SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData);
                var isConfirmed = await popup.WaitForExit();
                return isConfirmed is true;
            }
        }

        // 지휘자 스킬 장착 확인
        // if (_isOpenCommanderSkill)
        // {
        //     var isEquippedCommanderSkill = ServerDataManager.Instance.CommanderSkill.IsAllCommanderSkillsEquipped(_specUserGrade.maximum_commander_skill_count);
        //     if (!isEquippedCommanderSkill)
        //     {
        //         var newPopupData = new SystemConfirmPopupData("시스템 알림", "MSG_ALERT_EQUIP_COMMAND_SKILL", "확인", "취소");
        //         SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData).Forget();
        //         return false;
        //     }
        // }

        return true;
    }
}
