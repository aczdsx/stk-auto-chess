using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStateStageCombat : StateCombatBase
{
    private List<CharacterController> characters;
    private bool _isEndCombat;
    private bool _isWin;
    private bool _isClearStage;

    public override void StateInit(object target)
    {
        characters = ListPool<CharacterController>.Get();
        
        InGameSynergyManager.Instance.ClearSynergyFx();
        InGameMain.GetInGameMain().SetActiveObjectMover(false);
        InGameMain.GetInGameMain().InitCombatStateUI(); 
        InGameObjectManager.Instance.SaveStartingPlayerCharacter();
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Player);
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Enemy);

        InGameObjectManager.Instance.InGameStage.GraduallyChangeBoardColor(Color.gray, 1.0f);
        
        bool isSize75 = InGameManager.Instance.SpecStage.chapter_id == 1 || InGameManager.Instance.SpecStage.chapter_id == 2; // [TODO] 나중에 데이터로 
        if (isSize75)
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(6.0f, new Vector3(0, 3.0f, -10), 1.0f).Forget();
        else
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(7.0f, new Vector3(0, 2.5f, -10), 1.0f).Forget();

        InGameObjectManager.Instance.ClearTargetLine();
    }

    public override void StateStart()
    {
        // 전투 시작 전까지 아이템이 부여되지 않은 아이템들의 콜백 호출
        InGameSynergyManager.Instance.CheckAndHandleNotAppliedItemsBeforeCombat();
        
        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Player))
        {
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
            var effectCodes = character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
                EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Enemy))
        {
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
            var effectCodes =character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
                EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Neutral))
        {
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
        }



        {
            var effectCodes =
                InGameManager.Instance.TeamEcc.GetCharacterEffectCodesByFlag(
                    EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }

        StartAsync().Forget();
    }

    private async UniTask StartAsync()
    {
        // 서버에 전투 시작 요청
        var response = await NetManager.Instance.Battle.StartAsync((uint)InGameManager.Instance.SpecStage.stage_id);
        if (response != null && response.IsSuccess)
        {
            InGameManager.Instance.BattleSessionId = response.BattleSessionId;
        }

        // 전투 시작 후 1초는 대기
        await UniTask.Delay(1000);

        // 모든 캐릭터 락 해제
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Player, characters);
        foreach (CharacterController charac in characters)
        {
            charac.AddNextState<CharacterStateIdle>();

            charac.Target = InGameObjectManager.Instance.GetNearestTargetOnce(charac);
        }
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, characters);
        foreach (CharacterController charac in characters)
        {
            charac.AddNextState<CharacterStateIdle>();

            charac.Target = InGameObjectManager.Instance.GetNearestTargetOnce(charac);
        }

        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Neutral, characters);
        foreach (CharacterController charac in characters)
        {
            charac.AddNextState<CharacterStateIdle>();
        }
    }

    public override void StateRunning(float dt)
    {
        if (_isEndCombat)
            return;

        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Player, characters);
        if (characters.Count == 0)
        {
            _isEndCombat = true;
            _isWin = false;
            InGameManager.Instance.AppEventResult = "fail";
            InGameManager.Instance.AppEventReason = "dead";
        }

        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, characters);
        if (characters.Count == 0)
        {
            _isClearStage = ServerDataManager.Instance.Battle.IsStageCleared((uint)InGameManager.Instance.SpecStage.stage_id);
            _isEndCombat = true;
            _isWin = true;
            InGameManager.Instance.AppEventResult = (_isClearStage) ? "clear" : "pass"; 
            InGameManager.Instance.AppEventReason =  (_isClearStage) ? "clear" : "pass"; 
        }

        if (InGameMain.GetInGameMain().InGameTime <= 0)
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_INGAME_TIME_OVER");
            _isEndCombat = true;
            _isWin = false;
            InGameManager.Instance.AppEventResult = "fail";
            InGameManager.Instance.AppEventReason = "time_out";
        }

        if (_isEndCombat)
        {
            InGameManager.Instance.IsInGameCombat = false;
            ChangeNextState(_isWin).Forget();
        }
    }

    public override void StateEnd(bool isForced)
    {
        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Player))
        {
            character.RemoveSynergyEffectCodeALL();
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Enemy))
        {
            character.RemoveSynergyEffectCodeALL();
        }

        ListPool<CharacterController>.Release(characters);
        characters = null;
    }

    private async UniTask ChangeNextState(bool isWin)
    {
        if (isWin && !_isClearStage)
        {
            bool isUsePopup = InGameManager.Instance.SpecStage.chapter_id > 1;
            if (isUsePopup)
                SceneUILayerManager.Instance.PushUILayerAsync<IdleRewardIncreasedPopup>().Forget();
        }
        
        InGameMainFlowManager.Instance.SetPlaySpeed(0.4f);
        await UniTask.Delay(1500);
        InGameMainFlowManager.Instance.SetInGameSpeed(false);
        
        if (isWin)
        {
            InGameMainFlowManager.Instance.AddNextState<FlowStateStageClear>();
        }
        else
        {
            InGameMainFlowManager.Instance.AddNextState<FlowStateStageFail>();
        }
    }
}
