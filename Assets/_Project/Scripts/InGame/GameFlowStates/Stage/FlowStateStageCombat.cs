using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStateStageCombat : StateBase
{
    private List<CharacterController> characters;
    private bool isEndCombat;
    private bool isWin;

    public override void StateInit(object target)
    {
        characters = ListPool<CharacterController>.Get();

        // 최대 종합체력 업데이트
        InGameObjectManager.Instance.SaveStartingPlayerCharacter();
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Player);
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Enemy);

        InGameObjectManager.Instance.InGameStage.GraduallyChangeBoardColor(Color.gray, 1.0f);
        InGameCommanderManager.Instance.InGameCamera.SetCameraSize(6.0f, 1.5f, 1.0f).Forget();

        InGameMain.GetInGameMain().OpenStatisticPop();
    }

    public override void StateStart()
    {
        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Player))
        {
            character.AddSynergyEffectCode();
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Enemy))
        {
            character.AddSynergyEffectCode();
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
        }

        InGameManager.Instance.AddSynergyEffectCode(AllianceType.Player);
        InGameManager.Instance.AddSynergyEffectCode(AllianceType.Enemy);

        var effectCodes =
            InGameManager.Instance.EffectCodeContainer.GetCharacterEffectCodesByFlag(
                EffectCodeInheritFlag.UseOnCombatStart);
        EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);

        StartAsync().Forget();
    }

    private async UniTask StartAsync()
    {
        // 전투 시작 후 1초는 대기
        await UniTask.Delay(1000);

        // 모든 캐릭터 락 해제
        InGameObjectManager.Instance.GetAllAliveCharacters(AllianceType.Player, characters);
        foreach (CharacterController charac in characters)
        {
            if (charac.SpecCharacter.character_position_type == CharacterPositionType.ASSASSIN)
                charac.AddNextState<CharacterStateAssassinFirstMove>();
            else
                charac.AddNextState<CharacterStateIdle>();

            charac.Target = InGameObjectManager.Instance.GetNearestTargetOnce(charac);
        }
        InGameObjectManager.Instance.GetAllAliveCharacters(AllianceType.Enemy, characters);
        foreach (CharacterController charac in characters)
        {
            if (charac.SpecCharacter.character_position_type == CharacterPositionType.ASSASSIN)
                charac.AddNextState<CharacterStateAssassinFirstMove>();
            else
                charac.AddNextState<CharacterStateIdle>();

            charac.Target = InGameObjectManager.Instance.GetNearestTargetOnce(charac);
        }
    }

    public override void StateRunning(float dt)
    {
        if (isEndCombat)
            return;

        InGameObjectManager.Instance.GetAllAliveCharacters(AllianceType.Player, characters);
        if (characters.Count == 0)
        {
            isEndCombat = true;
            isWin = false;
        }

        InGameObjectManager.Instance.GetAllAliveCharacters(AllianceType.Enemy, characters);
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
        InGameMainFlowManager.Instance.SetPlaySpeed(0.4f);
        await UniTask.Delay(1200);
        InGameMainFlowManager.Instance.SetPlaySpeed(1.0f);
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
