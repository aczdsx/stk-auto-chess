using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStateTrialDungeonCombat : StateCombatBase
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

        InGameObjectManager.Instance.InGameStage.GraduallyChangeBoardColor(Color.gray, 1.0f, true);
        
        if (InGameManager.Instance.SpecDungeonTrial.dungeon_map_id == 1)
            InGameCommanderManager.Instance.InGameCamera.SetCameraSize(9.0f, new Vector3(0, 1.5f, -10), 1.0f).Forget();
        else
            InGameCommanderManager.Instance.InGameCamera.SetCameraSize(8.0f, new Vector3(0, 3f, -10), 1.0f).Forget();
        
        InGameObjectManager.Instance.ClearTargetLine();
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

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Neutral))
        {
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
        await UniTask.Delay(1000);

        if (InGameManager.Instance.SpecDungeonTrial.dungeon_map_id == 1)
        {
            HandleCharacters(AllianceType.Player, idleState: true);
            HandleCharacters(AllianceType.Enemy, idleState: true);
            HandleCharacters(AllianceType.Neutral, idleState: true, findTarget: false);
        }
        else
        {
            HandleCharacters(AllianceType.Player, idleState: false);
            HandleCharacters(AllianceType.Enemy, idleState: false);
            HandleCharacters(AllianceType.Neutral, idleState: true, findTarget: false);
        }
    }

    private void HandleCharacters(AllianceType allianceType, bool idleState, bool findTarget = true)
    {
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(allianceType, characters);
        foreach (CharacterController charac in characters)
        {
            if (idleState || charac.SpecCharacter.character_position_type != CharacterPositionType.ASSASSIN)
            {
                charac.AddNextState<CharacterStateIdle>();
            }
            else
            {
                charac.AddNextState<CharacterStateAssassinFirstMove>();
            }

            if (findTarget)
            {
                charac.Target = InGameObjectManager.Instance.GetNearestTargetOnce(charac);
            }
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
            InGameManager.Instance.AppEventResult = "fail";
            InGameManager.Instance.AppEventReason = "dead";
        }

        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, characters);
        if (characters.Count == 0)
        {
            isEndCombat = true;
            isWin = true;
            InGameManager.Instance.AppEventResult = "pass";
            InGameManager.Instance.AppEventReason = "pass";
        }

        if (InGameMain.GetInGameMain().InGameTime <= 0)
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_INGAME_TIME_OVER");
            isEndCombat = true;
            isWin = false;
            InGameManager.Instance.AppEventResult = "fail";
            InGameManager.Instance.AppEventReason = "time_out";
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
            InGameMainFlowManager.Instance.AddNextState<FlowStateTrialDungeonClear>();
        }
        else
        {
            InGameMainFlowManager.Instance.AddNextState<FlowStateTrialDungeonFail>();
        }
    }
}
