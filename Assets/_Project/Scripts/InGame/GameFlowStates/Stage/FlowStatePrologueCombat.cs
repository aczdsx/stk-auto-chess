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

public class FlowStatePrologueCombat : StateCombatBase
{
    private List<CharacterController> characters;
    private bool _isEndCombat;
    private bool _isWin;
    private bool _isClearStage;

    // 프롤로그 시나리오 관련
    private int _scenarioStep = 0;
    private bool _isScenarioMode = false;
    private CharacterController _clayCharacter;      // 클레이
    private CharacterController _yuniCharacter;     // 유니
    private CharacterController _philiaCharacter;   // 필리아
    private CharacterController _artesiaCharacter;  // 아트레시아
    private CharacterController _marieCharacter;    // 마리에
    private CharacterController _witchCharacter;    // 라플라스 마녀

    public override void StateInit(object target)
    {
        characters = ListPool<CharacterController>.Get();

        InGameObjectManager.Instance.ClearSynergyFx();
        InGameMain.GetInGameMain().SetActiveObjectMover(false);
        InGameMain.GetInGameMain().InitCombatStateUI();
        InGameObjectManager.Instance.SaveStartingPlayerCharacter();
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Player);
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Enemy);

        InGameObjectManager.Instance.InGameStage.GraduallyChangeBoardColor(Color.gray, 1.0f);

        InGameCommanderManager.Instance.InGameCamera.SetCameraSize(7.0f, new Vector3(0, 2.5f, -10), 1.0f).Forget();

        InGameObjectManager.Instance.ClearTargetLine();
    }

    public override void StateStart()
    {
        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Player))
        {
            character.AddSynergyEffectCode();
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
            var effectCodes = character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
                EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Enemy))
        {
            character.AddSynergyEffectCode();
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
            var effectCodes = character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
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


        InitializePrologueScenario().Forget();
        // StartAsync().Forget();
    }
    private async UniTask InitializePrologueScenario()
    {
        // 캐릭터 찾기 (ID는 실제 캐릭터 ID로 변경 필요)
        _clayCharacter = FindCharacterById(AllianceType.Player, 140103); // 클레이 ID 예시
        _yuniCharacter = FindCharacterById(AllianceType.Player, 130601); // 유니 ID 예시
        _philiaCharacter = FindCharacterById(AllianceType.Player, 130402); // 필리아 ID 예시
        _artesiaCharacter = FindCharacterById(AllianceType.Player, 140101); // 아트레시아 ID 예시
        // _marieCharacter = FindCharacterById(AllianceType.Player, 130501); // 마리에 ID 예시

        // 라플라스 마녀 찾기 (적)
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, characters);
        if (characters.Count > 0)
        {
            _witchCharacter = characters[0]; // 첫 번째 적이 마녀
        }

        // 시나리오 시작
        await StartPrologueScenario();
    }

    private CharacterController FindCharacterById(AllianceType allianceType, int characterId)
    {
        var characterList = InGameObjectManager.Instance.GetCharacterList(allianceType);
        foreach (var character in characterList)
        {
            if (character.GetCharacterStat().Spec.id == characterId)
            {
                return character;
            }
        }
        return null;
    }

    struct PrologueScenarioData
    {
        public int step;
        public int dialogueID;
        public string guideText;
        public PrologueActionType actionType;
    }

    enum PrologueActionType
    {
        None,
        WitchAttackPrepare,      // 마녀 공격 준비 이펙트
        ClaySkill,               // 클레이 스킬 + 마녀 공격/준비 이펙트 Off
        WitchAttack2AndClayDown,  // 마녀 공격2 + 클레이 죽음
        YuniPhiliaSkillAndWitchGroggy, // 유니/필리아 스킬 + 마녀 그로기 + HP 1
        WitchAoEAttackAndMarieJoin, // 마녀 광역 공격 + 유니/필리아 죽음 + 마리에 합류
        MarieSkillAndArtesiaSupernova, // 마리에 스킬 + 아트레시아 초신성 모드
        ArtesiaSkill,            // 아트레시아 스킬
        WitchFinalPrepare,       // 마녀 최종 공격 준비 이펙트
        EndCombat
    }

    private static readonly PrologueScenarioData[] PrologueScenarioSteps = new PrologueScenarioData[]
    {
        // 1단계: 마녀의 선제공격
        new PrologueScenarioData { step = 1, dialogueID = 2001, guideText = "", actionType = PrologueActionType.WitchAttackPrepare },
        
        // 2단계: 클레이의 방어
        new PrologueScenarioData { step = 2, dialogueID = 2001, guideText = "", actionType = PrologueActionType.ClaySkill },
        
        // 3단계: 클레이 탈락
        new PrologueScenarioData { step = 3, dialogueID = 2001, guideText = "라플라스 마녀의 강력한 공격을 클레이의 스킬로 막아냈습니다.(쉴드+회복 효과)", actionType = PrologueActionType.WitchAttack2AndClayDown },
        
        // 4단계: 유니&필리아의 반격
        new PrologueScenarioData { step = 4, dialogueID = 2001, guideText = "라플라스 마녀가 지친 지금이 공격할 타이밍입니다. (물리/마법 공격력)", actionType = PrologueActionType.YuniPhiliaSkillAndWitchGroggy },
        
        // 5단계: 마녀의 역습
        new PrologueScenarioData { step = 5, dialogueID = 2001, guideText = "", actionType = PrologueActionType.WitchAoEAttackAndMarieJoin },
        
        // 6단계: 마리에 합류
        new PrologueScenarioData { step = 6, dialogueID = 2001, guideText = "마리에의 스킬은 적의 공격력과 방어력을 낮춥니다.", actionType = PrologueActionType.MarieSkillAndArtesiaSupernova },
        
        // 7단계: 아트레시아의 각성
        new PrologueScenarioData { step = 7, dialogueID = 2001, guideText = "아트레시아의 스킬은 강력한 범위 공격을 진행합니다.", actionType = PrologueActionType.ArtesiaSkill },
        
        // 8단계: 마녀의 진정한 힘
        new PrologueScenarioData { step = 8, dialogueID = 2001, guideText = "", actionType = PrologueActionType.WitchFinalPrepare },
        
        // 9단계: 최후의 순간
        new PrologueScenarioData { step = 9, dialogueID = 2001, guideText = "", actionType = PrologueActionType.EndCombat }
    };

    private async UniTask StartPrologueScenario()
    {
        foreach (var scenarioData in PrologueScenarioSteps)
        {
            // 다이얼로그 표시 및 완료 대기 (dialogueID가 0이 아닌 경우만)
            if (scenarioData.dialogueID > 0)
            {
                await ShowDialogueAndWait(scenarioData.dialogueID);
            }

            // 가이드 문구 표시 (있는 경우)
            if (!string.IsNullOrEmpty(scenarioData.guideText))
            {
                ToastManager.Instance?.ShowToast(scenarioData.guideText, true);
                await UniTask.Delay(1000); // 가이드 문구 표시 대기
            }

            // 액션 실행
            await ExecuteAction(scenarioData.actionType);
        }
    }

    private async UniTask ShowDialogueAndWait(int dialogueGroupID)
    {
        var tcs = new UniTaskCompletionSource();

        object param = (dialogueGroupID, (Action)(() =>
        {
            tcs.TrySetResult();
        }));

        SceneUILayerManager.Instance.PushUILayerAsync<DialoguePopup>(param).Forget();

        await tcs.Task;
    }

    private async UniTask ExecuteAction(PrologueActionType actionType)
    {
        switch (actionType)
        {
            case PrologueActionType.WitchAttackPrepare:
                await TriggerWitchAttackPrepare();
                break;
            case PrologueActionType.ClaySkill:
                await TriggerClaySkill();
                break;
            case PrologueActionType.WitchAttack2AndClayDown:
                await TriggerWitchAttack2AndClayDown();
                break;
            case PrologueActionType.YuniPhiliaSkillAndWitchGroggy:
                await TriggerYuniPhiliaSkillAndWitchGroggy();
                break;
            case PrologueActionType.WitchAoEAttackAndMarieJoin:
                await TriggerWitchAoEAttackAndMarieJoin();
                break;
            case PrologueActionType.MarieSkillAndArtesiaSupernova:
                await TriggerMarieSkillAndArtesiaSupernova();
                break;
            case PrologueActionType.ArtesiaSkill:
                await TriggerArtesiaSkill();
                break;
            case PrologueActionType.WitchFinalPrepare:
                await TriggerWitchFinalPrepare();
                break;
            case PrologueActionType.EndCombat:
                _isEndCombat = true;
                _isWin = true;
                InGameManager.Instance.AppEventResult = "clear";
                InGameManager.Instance.AppEventReason = "prologue_clear";
                break;
            case PrologueActionType.None:
            default:
                await UniTask.Delay(3000); // 기본 대기 시간 (3초)
                break;
        }
    }

    // 1단계: 마녀 공격 준비 이펙트 Loop
    private async UniTask TriggerWitchAttackPrepare()
    {
        if (_witchCharacter == null) return;

        // 라플라스 마녀의 공격 준비 이펙트 Loop 시작
        // [TODO] 실제 이펙트 시스템 연동 필요
        // 예: _witchCharacter.GetCharacterView().PlayEffect("WitchAttackPrepareLoop");

        await UniTask.Delay(3000); // 3초 대기
    }

    // 2단계: 클레이 스킬 + 마녀 공격/준비 이펙트 Off
    private async UniTask TriggerClaySkill()
    {
        if (_clayCharacter == null) return;

        // 클레이 스킬 강제 발동
        foreach (var effectCode in _clayCharacter.GetEffectCodeContainer().EffectCodes)
        {
            if (effectCode is EffectCodeSkillTemplate skill)
            {
                skill.Activate();
                break;
            }
        }

        // 마녀 공격 + 준비 이펙트 Off
        // [TODO] 실제 이펙트 시스템 연동 필요
        // 예: _witchCharacter.GetCharacterView().StopEffect("WitchAttackPrepareLoop");

        await UniTask.Delay(3000); // 3초 대기
    }

    // 3단계: 마녀 공격2 + 클레이 죽음
    private async UniTask TriggerWitchAttack2AndClayDown()
    {
        if (_witchCharacter == null) return;

        // 마녀 공격2 스킬 발동
        foreach (var effectCode in _witchCharacter.GetEffectCodeContainer().EffectCodes)
        {
            if (effectCode is EffectCodeSkillTemplate skill)
            {
                skill.Activate();
                break;
            }
        }

        await UniTask.Delay(1000); // 공격 애니메이션 대기

        // 클레이 죽음
        if (_clayCharacter != null && _clayCharacter.IsAlive)
        {
            _clayCharacter.AddNextState<CharacterStateDead>();
        }

        await UniTask.Delay(2000); // 총 3초 대기
    }

    // 4단계: 유니/필리아 스킬 + 마녀 그로기 + HP 1
    private async UniTask TriggerYuniPhiliaSkillAndWitchGroggy()
    {
        // 유니 스킬 발동
        if (_yuniCharacter != null)
        {
            foreach (var effectCode in _yuniCharacter.GetEffectCodeContainer().EffectCodes)
            {
                if (effectCode is EffectCodeSkillTemplate skill)
                {
                    skill.Activate();
                    break;
                }
            }
        }

        // 필리아 스킬 발동
        if (_philiaCharacter != null)
        {
            foreach (var effectCode in _philiaCharacter.GetEffectCodeContainer().EffectCodes)
            {
                if (effectCode is EffectCodeSkillTemplate skill)
                {
                    skill.Activate();
                    break;
                }
            }
        }

        // 다른 캐릭터들도 라플라스 마녀 계속 공격
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Player, characters);
        foreach (var character in characters)
        {
            if (character != _yuniCharacter && character != _philiaCharacter && character.IsAlive)
            {
                character.Target = _witchCharacter;
                character.AddNextState<CharacterStateIdle>();
            }
        }

        await UniTask.Delay(2000); // 스킬 애니메이션 대기

        // 마녀 그로기 상태 + 체력 1로 설정 (죽지는 않음)
        if (_witchCharacter != null && _witchCharacter.IsAlive)
        {
            _witchCharacter.ForceSetHp(1);
            // [TODO] 그로기 상태 애니메이션/이펙트
            // 예: _witchCharacter.GetCharacterView().PlayAnimation("Groggy");
        }

        await UniTask.Delay(1000); // 총 3초 대기
    }

    // 5단계: 마녀 광역 공격 + 유니/필리아 죽음 + 마리에 합류
    private async UniTask TriggerWitchAoEAttackAndMarieJoin()
    {
        if (_witchCharacter == null) return;

        // 마녀 최종 스킬 발동
        foreach (var effectCode in _witchCharacter.GetEffectCodeContainer().EffectCodes)
        {
            if (effectCode is EffectCodeSkillTemplate skill)
            {
                skill.Activate();
                break;
            }
        }

        await UniTask.Delay(1000); // 스킬 애니메이션 대기

        // 유니, 필리아 죽음
        if (_yuniCharacter != null && _yuniCharacter.IsAlive)
            _yuniCharacter.AddNextState<CharacterStateDead>();
        if (_philiaCharacter != null && _philiaCharacter.IsAlive)
            _philiaCharacter.AddNextState<CharacterStateDead>();

        await UniTask.Delay(1000);

        // 마리에 마녀 뒤로 전투 합류
        // [TODO] 마리에가 이미 필드에 있다고 가정하거나, 필요시 AddCharacterToField 호출
        // 마녀 뒤 위치 계산 및 마리에 배치
        if (_marieCharacter != null && _witchCharacter != null)
        {
            var witchTile = _witchCharacter.CurrentTile;
            // 마녀 뒤 위치 찾기 (간단히 마녀 위치 기준으로 계산)
            // [TODO] 실제 위치 계산 로직 필요
        }

        await UniTask.Delay(1000); // 총 3초 대기
    }

    // 6단계: 마리에 스킬 + 아트레시아 초신성 모드
    private async UniTask TriggerMarieSkillAndArtesiaSupernova()
    {
        // 마리에 스킬 발동
        if (_marieCharacter != null)
        {
            foreach (var effectCode in _marieCharacter.GetEffectCodeContainer().EffectCodes)
            {
                if (effectCode is EffectCodeSkillTemplate skill)
                {
                    skill.Activate();
                    break;
                }
            }
        }

        await UniTask.Delay(1000);

        // 아트레시아, 마리에 같이 계속 공격
        if (_artesiaCharacter != null && _artesiaCharacter.IsAlive)
        {
            _artesiaCharacter.Target = _witchCharacter;
            _artesiaCharacter.AddNextState<CharacterStateIdle>();
        }
        if (_marieCharacter != null && _marieCharacter.IsAlive)
        {
            _marieCharacter.Target = _witchCharacter;
            _marieCharacter.AddNextState<CharacterStateIdle>();
        }

        // 아트레시아 초신성 모드
        // [TODO] 초신성 모드 활성화 (버프/이펙트 등)
        // 예: _artesiaCharacter.GetEffectCodeContainer().AddEffectCode(...);

        await UniTask.Delay(2000); // 총 3초 대기
    }

    // 7단계: 아트레시아 스킬
    private async UniTask TriggerArtesiaSkill()
    {
        if (_artesiaCharacter == null) return;

        foreach (var effectCode in _artesiaCharacter.GetEffectCodeContainer().EffectCodes)
        {
            if (effectCode is EffectCodeSkillTemplate skill)
            {
                skill.Activate();
                break;
            }
        }

        await UniTask.Delay(3000); // 3초 대기
    }

    // 8단계: 마녀 최종 공격 준비 이펙트
    private async UniTask TriggerWitchFinalPrepare()
    {
        if (_witchCharacter == null) return;

        // 라플라스 마녀 스킬 발동 준비 이펙트
        // [TODO] 실제 이펙트 시스템 연동 필요
        // 예: _witchCharacter.GetCharacterView().PlayEffect("WitchFinalPrepare");

        await UniTask.Delay(3000); // 3초 대기
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
        if (_isEndCombat)
            return;
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
