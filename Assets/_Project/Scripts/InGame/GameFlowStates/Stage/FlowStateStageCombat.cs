using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStateStageCombat : StateCombatBase
{
    private List<CharacterController> characters;
    private bool isEndCombat;
    private bool isWin;

    public override void StateInit(object target)
    {
        characters = ListPool<CharacterController>.Get();

        InGameObjectManager.Instance.ClearSynergyFx();
        InGameMain.GetInGameMain().InitCombatStateUI();
        InGameObjectManager.Instance.SaveStartingPlayerCharacter();
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Player);
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Enemy);

        InGameObjectManager.Instance.InGameStage.GraduallyChangeBoardColor(Color.gray, 1.0f);
        
        bool isSize75 = InGameManager.Instance.SpecStage.chapter_id == 1 || InGameManager.Instance.SpecStage.chapter_id == 2; // [TODO] 나중에 데이터로 
        if (isSize75)
            InGameCommanderManager.Instance.InGameCamera.SetCameraSize(6.5f, new Vector3(0, 2.5f, -10), 1.0f).Forget();
        else
            InGameCommanderManager.Instance.InGameCamera.SetCameraSize(7.0f, new Vector3(0, 2.0f, -10), 1.0f).Forget();

        InGameObjectManager.Instance.ClearTargetLine();
    }

    public override void StateStart()
    {
        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Player))
        {
            character.AddSynergyEffectCode();
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
            var effectCodes =character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
                EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Enemy))
        {
            character.AddSynergyEffectCode();
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
            var effectCodes =character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
                EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Neutral))
        {
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
        }

        InGameManager.Instance.AddSynergyEffectCode(AllianceType.Player);
        InGameManager.Instance.AddSynergyEffectCode(AllianceType.Enemy);

        {
            var effectCodes =
                InGameManager.Instance.EffectCodeContainer.GetCharacterEffectCodesByFlag(
                    EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }

        StartAsync().Forget();
    }

    private async UniTask StartAsync()
    {
        // 전투 시작 후 1초는 대기
        await UniTask.Delay(1000);

        // 모든 캐릭터 락 해제
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Player, characters);
        foreach (CharacterController charac in characters)
        {
            if (charac.SpecCharacter.character_position_type == CharacterPositionType.ASSASSIN)
                charac.AddNextState<CharacterStateAssassinFirstMove>();
            else
                charac.AddNextState<CharacterStateIdle>();

            charac.Target = InGameObjectManager.Instance.GetNearestTargetOnce(charac);
        }
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, characters);
        foreach (CharacterController charac in characters)
        {
            if (charac.SpecCharacter.character_position_type == CharacterPositionType.ASSASSIN)
                charac.AddNextState<CharacterStateAssassinFirstMove>();
            else
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
        if (isEndCombat)
            return;

        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Player, characters);
        if (characters.Count == 0)
        {
            isEndCombat = true;
            isWin = false;
        }

        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, characters);
        if (characters.Count == 0)
        {
            isEndCombat = true;
            isWin = true;
        }

        if (InGameMain.GetInGameMain().InGameTime <= 0)
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_INGAME_TIME_OVER");
            isEndCombat = true;
            isWin = false;
        }

        if (isEndCombat)
            ChangeNextState(isWin).Forget();
    }

    public override void StateEnd(bool isForced)
    {
        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Player))
        {
            character.RemoveSynergyEffectCode();
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Enemy))
        {
            character.RemoveSynergyEffectCode();
        }

        ListPool<CharacterController>.Release(characters);
        characters = null;
    }

    private async UniTask ChangeNextState(bool isWin)
    {
        if (isWin)
            SceneUILayerManager.Instance.PushUILayerAsync<IdleRewardIncreasedPopup>().Forget();
        
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
