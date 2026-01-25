using System.Collections.Generic;
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

    // 고정 시드를 사용할 스테이지 ID 목록
    private static readonly HashSet<int> FixedSeedStageIds = new HashSet<int> { 10001, 10002, 10003 };

    /// <summary>
    /// 캐릭터의 스킬 쿨타임을 특정 비율로 설정합니다.
    /// </summary>
    /// <param name="character">대상 캐릭터</param>
    /// <param name="ratio">쿨타임 충전 비율 (0.0 ~ 1.0, 예: 0.6이면 60% 충전)</param>
    private void SetSkillCooltimeRatio(CharacterController character, float ratio)
    {
        var skillEffectCodes = character.GetEffectCodeContainer()
            .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseIsReadyToActivate);

        foreach (var eccBase in skillEffectCodes)
        {
            if (eccBase is EffectCodeCharacterBase characterEffectCode)
            {
                float durationTime = characterEffectCode.GetDurationTime();
                float newElapsedTime = durationTime * Mathf.Clamp01(ratio);
                characterEffectCode.SetElapsedTime(newElapsedTime);
            }
        }
    }

    public override void StateInit(object target)
    {
        // 특정 스테이지는 고정 시드 적용 (재현 가능한 전투)
        int stageId = InGameManager.Instance.SpecStage.stage_id;
        if (FixedSeedStageIds.Contains(stageId))
        {
            InGameManager.Instance.SetFixedRandomSeed(stageId);
            InGameManager.Instance.RegenerateGlobalRandomSeeds();
        }

        characters = ListPool<CharacterController>.Get();

        InGameSynergyManager.Instance.ClearSynergyFx();
        InGameMain.GetInGameMain().SetActiveObjectMover(false);
        InGameMain.GetInGameMain().InitCombatStateUI();
        InGameObjectManager.Instance.SaveStartingPlayerCharacter();
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Player);
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Enemy);

        InGameObjectManager.Instance.InGameStage.GraduallyChangeBoardColor(Color.gray, 1.0f);

        // bool isSize75 = InGameManager.Instance.SpecStage.chapter_id == 1 || InGameManager.Instance.SpecStage.chapter_id == 2; // [TODO] 나중에 데이터로 
        // if (isSize75)
        //     ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(6.0f, new Vector3(0, 3.0f, -10), 1.0f).Forget();
        // else
        //     ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(7.0f, new Vector3(0, 2.5f, -10), 1.0f).Forget();

        bool isSize75 = InGameManager.Instance.SpecStage.chapter_id == 1 || InGameManager.Instance.SpecStage.chapter_id == 2; // [TODO] 나중에 데이터로 
        if (isSize75)
        {
            // ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(7.5f, new Vector3(0, 2.0f, -10), 1.0f).Forget();
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraPositionMode(InGameCamera.CameraPositionMode.DefaultCombat);
        }
        else
        {
            // ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(8.5f, new Vector3(0, 1.5f, -10), 1.0f).Forget();
            // ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(6.0f, new Vector3(-12.0f, 10.0f, -12f), 1.0f).Forget();
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraPositionMode(InGameCamera.CameraPositionMode.LargeSizeCombat);

        }
        InGameObjectManager.Instance.ClearTargetLine();
        TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.COMBAT_START, "0");
    }

    public override void StateStart()
    {
        // 전투 시작 전까지 아이템이 부여되지 않은 아이템들의 콜백 호출
        InGameSynergyManager.Instance.CheckAndHandleNotAppliedItemsBeforeCombat();

        // 튜토리얼 10001: 캐릭터 3401의 스킬 쿨타임 60% 충전
        int stageId = InGameManager.Instance.SpecStage.stage_id;
        if (stageId == 10001)
        {
            foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Player))
            {
                if (character.CharacterId == 3401)
                {
                    // SetSkillCooltimeRatio(character, 0.4f);
                    SetSkillCooltimeRatio(character, 0.455f);
                    break;
                }
            }
        }

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
            var effectCodes = character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
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
        // 튜토리얼에서 CHARACTER_DEAD 트리거를 기다리는 중이면 패배 처리 건너뛰기
        bool isWaitingCharacterDeadTutorial = TutorialManager.Instance != null
            && TutorialManager.Instance.IsTutorial
            && TutorialManager.Instance.IsTutorialAction(TutorialTriggerType.CHARACTER_DEAD);

        if (characters.Count == 0 && !isWaitingCharacterDeadTutorial)
        {
            InGameManager.Instance.AppEventResult = "fail";
            InGameManager.Instance.AppEventReason = "dead";
            EndCombat(false);
            return;
        }

        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, characters);
        if (characters.Count == 0)
        {
            _isClearStage = ServerDataManager.Instance.Battle.IsStageCleared((uint)InGameManager.Instance.SpecStage.stage_id);
            InGameManager.Instance.AppEventResult = _isClearStage ? "clear" : "pass";
            InGameManager.Instance.AppEventReason = _isClearStage ? "clear" : "pass";

            // ENEMY_DEAD_ALL 튜토리얼 처리 시도 - 튜토리얼이 처리되면 종료는 튜토리얼 완료 시 수행
            bool tutorialHandled = TutorialEnemyDeadAllHandler.TryHandleTutorial(
                () => EndCombat(true));

            if (!tutorialHandled)
            {
                // 튜토리얼이 없으면 바로 종료
                EndCombat(true);
            }
            return;
        }

        if (InGameMain.GetInGameMain().InGameTime <= 0)
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_INGAME_TIME_OVER");
            InGameManager.Instance.AppEventResult = "fail";
            InGameManager.Instance.AppEventReason = "time_out";
            EndCombat(false);
        }
    }

    /// <summary>
    /// 전투 종료 처리 (일원화된 종료 로직)
    /// </summary>
    private void EndCombat(bool isWin)
    {
        if (_isEndCombat)
            return;

        _isEndCombat = true;
        _isWin = isWin;
        InGameManager.Instance.IsInGameCombat = false;
        ChangeNextState(_isWin).Forget();
    }

    public override void StateEnd(bool isForced)
    {
        // 핸들러 상태 정리 (비정상 종료 대비)
        TutorialEnemyDeadAllHandler.Clear();

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
