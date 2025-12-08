using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStatePvpCombat : StateCombatBase
{
    private List<CharacterController> characters;
    private bool isEndCombat;
    private bool isWin;

    public override void StateInit(object target)
    {
        base.TidyUpPreviewSynergy(AllianceType.Player);

        characters = ListPool<CharacterController>.Get();

        InGameObjectManager.Instance.ClearSynergyFx();
        InGameMain.GetInGameMain().InitCombatStateUI();
        InGameObjectManager.Instance.SaveStartingPlayerCharacter();
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Player);
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Enemy);

        InGameObjectManager.Instance.InGameStage.GraduallyChangeBoardColor(Color.gray, 1.0f);
        ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(7.0f, new Vector3(0, 2.5f, -10), 1.0f).Forget();
        
        InGameObjectManager.Instance.ClearTargetLine();
    }

    public override void StateStart()
    {
        base.AddSynergy(AllianceType.Player);
        base.AddSynergy(AllianceType.Enemy);

        base.AddPassive(AllianceType.Player);
        base.AddPassive(AllianceType.Enemy);

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Player))
        {
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Enemy))
        {
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Neutral))
        {
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
        }

        var effectCodes = InGameManager.Instance.TeamEcc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnCombatStart);
        EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);

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
            ToastManager.Instance.ShowToastByTokenKey("MSG_PVP_BATTLE_TIME_OVER");
            var playerHpRate = InGameObjectManager.Instance.GetHpRate(AllianceType.Player);
            var enemyHpRate = InGameObjectManager.Instance.GetHpRate(AllianceType.Enemy);
            isEndCombat = true;
            isWin = playerHpRate >= enemyHpRate;
        }

        if (isEndCombat)
        {
            InGameManager.Instance.IsInGameCombat = false;
            ChangeNextState(isWin).Forget();
        }
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
        await UniTask.Delay(1500);
        InGameMainFlowManager.Instance.SetInGameSpeed(false);
        if (isWin)
        {
            InGameMainFlowManager.Instance.AddNextState<FlowStatePvpClear>();
        }
        else
        {
            InGameMainFlowManager.Instance.AddNextState<FlowStatePvpFail>();
        }
    }
}
