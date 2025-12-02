using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStatePrologueCombat : StateCombatBase
{
    private List<CharacterController> characters;
    private bool _isEndCombat;
    private bool _isWin;
    private bool _isClearStage;

    private CharacterController _clayCharacter;      // 클레이
    private CharacterController _yuniCharacter;     // 유니
    private CharacterController _philiaCharacter;   // 필리아
    private CharacterController _artesiaCharacter;  // 아트레시아
    private CharacterController _marieCharacter;    // 마리에
    private CharacterController _witchCharacter;    // 라플라스 마녀

    private InGameVfx _witchAttackPrepareFx;       // 마녀 공격 준비 이펙트

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

        ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(7.0f, new Vector3(0, 2.5f, -10), 1.0f).Forget();
    }

    public override void StateStart()
    {
        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Player))
        {
            // character.AddSynergyEffectCode();
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
            var effectCodes = character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
                EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Enemy))
        {
            // character.AddSynergyEffectCode();
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
            var effectCodes = character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
                EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }

        // InGameManager.Instance.AddSynergyEffectCode(AllianceType.Player);
        // InGameManager.Instance.AddSynergyEffectCode(AllianceType.Enemy);

        {
            var effectCodes =
                InGameManager.Instance.EffectCodeContainer.GetCharacterEffectCodesByFlag(
                    EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }


        // 캐릭터 찾기
        _clayCharacter = FindCharacterById(AllianceType.Player, 140103); // 클레이
        _yuniCharacter = FindCharacterById(AllianceType.Player, 130601); // 유니
        _philiaCharacter = FindCharacterById(AllianceType.Player, 130402); // 필리아
        _artesiaCharacter = FindCharacterById(AllianceType.Player, 140101); // 아트레시아
        // 마리에는 5단계에서 합류하므로 초기에는 찾지 않음

        // 라플라스 마녀 찾기 (적)
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, characters);
        if (characters.Count > 0)
        {
            _witchCharacter = characters[0];
        }

        InitializePrologueScenario().Forget();
    }

    private async UniTask StartAllCharacters()
    {
        // 전투 시작 후 0.5초는 대기
        await UniTask.Delay(500);

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
    }

    // 모든 캐릭터를 Ready 상태로 변경하여 공격을 멈춤
    private async UniTask StopAllCharacters()
    {
        // 전투 시작 후 0.5초는 대기
        await UniTask.Delay(500);

        // 플레이어 캐릭터들 멈춤
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Player, characters);
        foreach (CharacterController charac in characters)
        {
            charac.AddNextState<CharacterStateReady>();
            charac.Target = null; // 타겟 제거하여 공격하지 않도록
        }

        // 적 캐릭터들 멈춤
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, characters);
        foreach (CharacterController charac in characters)
        {
            charac.AddNextState<CharacterStateReady>();
            charac.Target = null;
        }
    }


    private async UniTask InitializePrologueScenario()
    {
        // 시나리오 시작
        await StartPrologueScenario();
    }

    private CharacterController FindCharacterById(AllianceType allianceType, int characterId)
    {
        var characterList = InGameObjectManager.Instance.GetCharacterList(allianceType);
        foreach (var character in characterList)
        {
            if (character.GetCharacterStat().CharacterID == characterId)
            {
                return character;
            }
        }
        return null;
    }

    /// <summary>
    /// 캐릭터를 지정된 방향으로 이동시킵니다.
    /// </summary>
    /// <param name="character">이동시킬 캐릭터</param>
    /// <param name="deltaX">X 방향 이동 거리 (-1: 왼쪽, 0: 이동 없음, 1: 오른쪽)</param>
    /// <param name="deltaY">Y 방향 이동 거리 (-1: 아래, 0: 이동 없음, 1: 위)</param>
    /// <returns>이동 성공 여부</returns>
    private bool MoveCharacterToDirection(CharacterController character, int deltaX, int deltaY)
    {
        if (character == null || character.CurrentTile == null)
            return false;

        int2 targetPosition = new int2(character.CurrentTile.X + deltaX, character.CurrentTile.Y + deltaY);
        InGameTile targetTile = InGameObjectManager.Instance.InGameGrid.GetTile(targetPosition);

        if (targetTile == null)
        {
            Debug.LogWarning($"MoveCharacterToDirection: 타일을 찾을 수 없습니다. ({targetPosition.x}, {targetPosition.y})");
            return false;
        }

        // 타일이 이미 점유되어 있는지 확인
        if (targetTile.OccupiedCharacter != null)
        {
            Debug.LogWarning($"MoveCharacterToDirection: 타일이 이미 점유되어 있습니다. ({targetPosition.x}, {targetPosition.y})");
            return false;
        }

        // 이동 가능한지 확인 (move_speed > 0)
        if (character.GetCharacterStat().MoveSpeed <= 0)
        {
            Debug.LogWarning($"MoveCharacterToDirection: 캐릭터의 이동 속도가 0입니다. ({character.CharacterId})");
            return false;
        }

        // Ready 상태에서 Idle로 변경하여 이동 가능하도록 함
        if (character.GetCurrentState() is CharacterStateReady)
        {
            character.AddNextState<CharacterStateIdle>();
        }

        // MoveToTile을 사용하여 최적의 경로로 이동
        character.MoveToTile(targetTile);
        return true;
    }

    /// <summary>
    /// 캐릭터의 이동이 완료될 때까지 대기합니다.
    /// </summary>
    /// <param name="character">이동을 감지할 캐릭터</param>
    /// <param name="timeoutMs">타임아웃 시간 (밀리초, 기본값: 5000ms)</param>
    /// <returns>이동 완료 여부 (타임아웃 시 false)</returns>
    private async UniTask<bool> WaitForCharacterMoveComplete(CharacterController character, int timeoutMs = 5000)
    {
        if (character == null)
            return false;

        // 이미 이동 중이 아닌 경우 즉시 완료
        if (!(character.GetCurrentState() is CharacterStateMove))
            return true;

        var startTime = Time.time;
        var timeoutSeconds = timeoutMs / 1000f;

        while (character != null && character.GetCurrentState() is CharacterStateMove)
        {
            if (Time.time - startTime > timeoutSeconds)
            {
                Debug.LogWarning($"캐릭터 이동 타임아웃: {character.CharacterId}");
                return false;
            }

            await UniTask.Yield();
        }

        return character != null;
    }

    /// <summary>
    /// 캐릭터의 스킬을 강제로 활성화합니다.
    /// </summary>
    /// <param name="character">스킬을 활성화할 캐릭터</param>
    /// <param name="warningMessage">스킬을 찾지 못했을 때 출력할 경고 메시지 (null이면 출력하지 않음)</param>
    /// <returns>스킬 활성화 성공 여부</returns>
    private bool ActivateCharacterSkill(CharacterController character, string warningMessage = null)
    {
        if (character == null) return false;

        foreach (var effectCode in character.GetEffectCodeContainer().EffectCodes)
        {
            var characterData = SpecDataManager.Instance.GetCharacterData(character.CharacterId);
            if (characterData.skill_ids.Any(skillId => skillId == effectCode.CodeId))
            {
                if (effectCode is EffectCodeCharacterBase characterEffectCode)
                {
                    characterEffectCode.Activate();
                    return true;
                }
            }
        }

        if (!string.IsNullOrEmpty(warningMessage))
        {
            Debug.LogWarning(warningMessage);
        }

        return false;
    }

    struct PrologueScenarioData
    {
        public int step;
        public int dialogueID;
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
        WitchFinalAttack,        // 마녀 최후의 공격 시전
        ArtesiaDefendAndEndCombat // 아트레시아 방어 + 전투 종료
    }

    private static readonly PrologueScenarioData[] PrologueScenarioSteps = new PrologueScenarioData[]
    {
        // 1단계: 마녀의 선제공격 (DialogueGroupID : 200001)
        new PrologueScenarioData { step = 1, dialogueID = 200001, actionType = PrologueActionType.WitchAttackPrepare },
        
        // 2단계: 클레이의 방어 (DialogueGroupID : 200002)
        new PrologueScenarioData { step = 2, dialogueID = 200002, actionType = PrologueActionType.ClaySkill },
        
        // 3단계: 클레이 탈락 (DialogueGroupID : 200003)
        new PrologueScenarioData { step = 3, dialogueID = 200003, actionType = PrologueActionType.WitchAttack2AndClayDown },
        
        // 4단계: 유니&필리아의 반격 (DialogueGroupID : 200004)
        new PrologueScenarioData { step = 4, dialogueID = 200004, actionType = PrologueActionType.YuniPhiliaSkillAndWitchGroggy },
        
        // 5단계: 마녀의 역습 (DialogueGroupID : 200005)
        new PrologueScenarioData { step = 5, dialogueID = 200005, actionType = PrologueActionType.WitchAoEAttackAndMarieJoin },
        
        // 6단계: 마리에 합류 (DialogueGroupID : 200006)
        new PrologueScenarioData { step = 6, dialogueID = 200006, actionType = PrologueActionType.MarieSkillAndArtesiaSupernova },
        
        // 7단계: 아트레시아의 각성 (DialogueGroupID : 200007)
        new PrologueScenarioData { step = 7, dialogueID = 200007, actionType = PrologueActionType.ArtesiaSkill },
        
        // 8단계: 마녀의 진정한 힘 (DialogueGroupID : 200008)
        new PrologueScenarioData { step = 8, dialogueID = 200008, actionType = PrologueActionType.WitchFinalPrepare },
        
        // 9단계: 최후의 순간 (DialogueGroupID : 200009)
        new PrologueScenarioData { step = 9, dialogueID = 200009, actionType = PrologueActionType.WitchFinalAttack },
        
        // 10단계: 아트레시아의 결의 (DialogueGroupID : 200010)
        new PrologueScenarioData { step = 10, dialogueID = 200010, actionType = PrologueActionType.ArtesiaDefendAndEndCombat }
    };

    private async UniTask StartPrologueScenario()
    {
        foreach (var scenarioData in PrologueScenarioSteps)
        {
            // 다이얼로그 전에 모든 캐릭터 멈춤
            await StopAllCharacters();

            // 다이얼로그 표시 및 완료 대기 (dialogueID가 0이 아닌 경우만)
            if (scenarioData.dialogueID > 0)
            {
                await ShowDialogueAndWait(scenarioData.dialogueID);
            }

            // 액션 실행 (액션 타입에 따라 필요한 캐릭터들만 활성화)
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
            case PrologueActionType.WitchFinalAttack:
                await TriggerWitchFinalAttack();
                break;
            case PrologueActionType.ArtesiaDefendAndEndCombat:
                await TriggerArtesiaDefendAndEndCombat();
                break;
            case PrologueActionType.None:
            default:
                await UniTask.Delay(1000); // 기본 대기 시간 (1초)
                break;
        }
    }

    // 1단계: 마녀 공격 준비 이펙트 Loop
    private async UniTask TriggerWitchAttackPrepare()
    {
        if (_witchCharacter == null) return;

        // 라플라스 마녀의 공격 준비 이펙트 생성 및 캐릭터에 붙이기
        // 다크 속성 캐스팅 이펙트 사용 (필요시 다른 이펙트로 변경 가능)
        _witchAttackPrepareFx = InGameVfxManager.Instance.AddInGameVfx(
            InGameVfxNameType.fx_prologue_boss_prepare_01,
            _witchCharacter.SkillRootTransformFollowable);

        await UniTask.Delay(2000); // 2초 대기
    }

    // 2단계: 클레이 스킬 + 마녀 공격/준비 이펙트 Off
    private async UniTask TriggerClaySkill()
    {
        if (_clayCharacter == null) return;

        // 클레이를 y축으로 한 칸 앞으로 이동
        if (MoveCharacterToDirection(_clayCharacter, 0, 1))
        {
            // 이동 완료 대기
            await WaitForCharacterMoveComplete(_clayCharacter);
        }

        // 클레이 스킬 강제 발동
        ActivateCharacterSkill(_clayCharacter, "클레이 스킬을 찾을 수 없습니다.");

        // 라플라스 마녀 공격 준비 이펙트 종료
        if (_witchAttackPrepareFx != null)
        {
            InGameVfxManager.Instance.RemoveInGameVfx(_witchAttackPrepareFx);
            _witchAttackPrepareFx = null;
        }

        // 라플라스 마녀 스킬 발동
        ActivateCharacterSkill(_witchCharacter, "마녀 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(1000); // 1초 대기

        // 가이드 문구 토스트 표시
        ToastManager.Instance.ShowToast("라플라스 마녀의 강력한 공격을 클레이의 스킬로 막아냈습니다. 클레이의 스킬은 쉴드와 회복 효과를 지니고 있습니다.");

        await UniTask.Delay(2000); // 2초 대기
    }

    // 3단계: 마녀 공격2 + 클레이 죽음
    private async UniTask TriggerWitchAttack2AndClayDown()
    {
        if (_witchCharacter == null) return;

        // 마녀 공격2 스킬 발동
        ActivateCharacterSkill(_witchCharacter, "마녀 공격2 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(1000); // 공격 애니메이션 대기

        // 클레이 죽음
        if (_clayCharacter != null && _clayCharacter.IsAlive)
        {
            _clayCharacter.AddNextState<CharacterStateDead>();
        }

        // 라플라스 마녀는 스킬 반동으로 그로기 상태
        // [TODO] 그로기 상태 처리

        await UniTask.Delay(2000); // 2초 대기
    }

    // 4단계: 유니/필리아 스킬 + 마녀 그로기
    private async UniTask TriggerYuniPhiliaSkillAndWitchGroggy()
    {
        // 유니 스킬 발동
        ActivateCharacterSkill(_yuniCharacter, "유니 스킬을 찾을 수 없습니다.");
        // 필리아 스킬 발동
        ActivateCharacterSkill(_philiaCharacter, "필리아 스킬을 찾을 수 없습니다.");
        
        // 다른 캐릭터들도 라플라스 마녀 계속 공격
        StartAllCharacters().Forget();

        await UniTask.Delay(1000); // 스킬 애니메이션 대기

        // 가이드 문구 토스트 표시
        ToastManager.Instance.ShowToast("라플라스 마녀가 지친 지금이 공격할 타이밍 입니다. 유니와 필리아의 스킬은 강력한 물리/마법 공격력을 지닙니다.");

        // await UniTask.Delay(1000); // 추가 대기

        // // 라플라스 마녀 여전히 그로기 상태 (HP는 이미 1로 설정되어 있을 수 있음)
        // if (_witchCharacter != null && _witchCharacter.IsAlive)
        // {
        //     var currentHp = _witchCharacter.CurrentHp;
        //     if (currentHp > 1)
        //     {
        //         _witchCharacter.ForceSetHp(1);
        //         Debug.LogColor($"라플라스 마녀 그로기 상태, HP {currentHp} -> 1로 설정");
        //     }
        // }

        await UniTask.Delay(2000); // 2초 대기

        // 다른 캐릭터들 공격 멈춤
        await StopAllCharacters();
    }

    // 5단계: 마녀 광역 공격 + 유니/필리아 죽음 + 마리에 합류
    private async UniTask TriggerWitchAoEAttackAndMarieJoin()
    {
        if (_witchCharacter == null) return;

        // 라플라스 마녀 그로기 -> 스킬 모션으로
        // [TODO] 그로기 상태에서 스킬 모션으로 전환

        // 라플라스 마녀 광역 최종 스킬 발동
        ActivateCharacterSkill(_witchCharacter, "마녀 광역 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(1000); // 스킬 애니메이션 대기

        // 유니, 필리아 죽음
        if (_yuniCharacter != null && _yuniCharacter.IsAlive)
            _yuniCharacter.AddNextState<CharacterStateDead>();
        if (_philiaCharacter != null && _philiaCharacter.IsAlive)
            _philiaCharacter.AddNextState<CharacterStateDead>();

        await UniTask.Delay(2000); // 2초 대기

        // 마리에 마녀 뒤쪽에서 전투 합류 (5단계 종료 후 추가 행동)
        // 스폰 위치: (2, 3)
        _marieCharacter = FindCharacterById(AllianceType.Player, 130501);

        if (_marieCharacter == null)
        {
            int2 spawnPosition = new int2(2, 3);
            InGameTile spawnTile = InGameObjectManager.Instance.InGameGrid.GetTile(spawnPosition);

            // (2, 3) 위치가 점유되어 있으면 주변 빈 타일 찾기
            if (spawnTile == null || spawnTile.OccupiedCharacter != null)
            {
                spawnTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile(AllianceType.Player);
            }

            if (spawnTile != null)
            {
                // 마리에 소환
                int marieCharacterId = 130501;
                int marieLevel = 1; // [TODO] 레벨 설정 필요

                var marieStat = new CharacterStatData(marieCharacterId, marieLevel,
                    GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());

                _marieCharacter = await InGameObjectManager.Instance.AddCharacterToField(
                    marieStat,
                    new int2(spawnTile.X, spawnTile.Y),
                    AllianceType.Player,
                    typeof(CharacterStateIdle),
                    true,
                    HpBarType.Synergy);

                // 마녀를 타겟으로 설정
                if (_marieCharacter != null && _witchCharacter != null)
                {
                    _marieCharacter.Target = _witchCharacter;
                }

                Debug.LogColor($"마리에 합류: {marieCharacterId} at ({spawnTile.X}, {spawnTile.Y})");
            }
        }
        else if (_marieCharacter != null)
        {
            // 이미 필드에 있는 경우 마녀를 타겟으로 설정
            _marieCharacter.Target = _witchCharacter;
            _marieCharacter.AddNextState<CharacterStateIdle>();
            Debug.LogColor($"마리에 이미 필드에 존재, 타겟 설정 완료");
        }
    }

    // 6단계: 마리에 스킬 + 아트레시아 초신성 모드
    private async UniTask TriggerMarieSkillAndArtesiaSupernova()
    {
        // 모든 플레이어 캐릭터를 Idle로 변경하여 공격 재개
        StartAllCharacters().Forget();

        // 마리에 디버프 스킬 발동
        ActivateCharacterSkill(_marieCharacter, "마리에 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(1000);

        // 가이드 문구 토스트 표시
        ToastManager.Instance.ShowToast("마리에의 스킬은 적의 공격력과 방어력을 낮추는 효과를 지니고 있습니다.");

        // 아트레시아 초신성 모드 진입
        // [TODO] 초신성 모드 활성화 (버프/이펙트 등)
        // 예: _artesiaCharacter.GetEffectCodeContainer().AddEffectCode(...);
        if (_artesiaCharacter != null)
        {
            Debug.LogColor("아트레시아 초신성 모드 진입");
        }

        await UniTask.Delay(2000); // 2초 대기
    }

    // 7단계: 아트레시아 스킬
    private async UniTask TriggerArtesiaSkill()
    {
        // 가이드 문구 토스트 표시
        ToastManager.Instance.ShowToast("아트레시아의 스킬은 강력한 범위 공격을 진행합니다.");

        // 아트레시아 범위 스킬 발동
        ActivateCharacterSkill(_artesiaCharacter, "아트레시아 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(2000); // 2초 대기
    }

    private InGameVfx _witchFinalPrepareFx; // 마녀 최후의 공격 준비 이펙트

    // 8단계: 마녀 최종 공격 준비 이펙트
    private async UniTask TriggerWitchFinalPrepare()
    {
        if (_witchCharacter == null) return;

        // 라플라스 마녀 최후의 공격 준비 이펙트 재생
        // 암흑 힘 응집 이펙트 (마녀를 감싸는 이펙트)
        _witchFinalPrepareFx = InGameVfxManager.Instance.AddInGameVfx(
            InGameVfxNameType.fx_prologue_boss_prepare_02,
            _witchCharacter.SkillRootTransformFollowable);

        // [TODO] 필요시 더 강력한 이펙트로 변경
        // 예: _witchCharacter.GetCharacterView().PlayEffect("WitchFinalPrepare");

        await UniTask.Delay(3000); // 3초 대기
    }

    // 9단계: 마녀 최후의 공격 시전
    private async UniTask TriggerWitchFinalAttack()
    {
        if (_witchCharacter == null) return;

        // 마녀 최후의 공격 준비 이펙트 제거
        if (_witchFinalPrepareFx != null)
        {
            InGameVfxManager.Instance.RemoveInGameVfx(_witchFinalPrepareFx);
            _witchFinalPrepareFx = null;
        }

        // 라플라스 마녀 최후의 공격 스킬 발동
        ActivateCharacterSkill(_witchCharacter, "마녀 최후의 공격 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(3000); // 3초 대기
    }

    // 10단계: 아트레시아 방어 + 전투 종료
    private async UniTask TriggerArtesiaDefendAndEndCombat()
    {
        if (_artesiaCharacter == null) return;

        // 아트레시아가 마지막 공격을 막아냄
        // [TODO] 방어/막기 애니메이션/이펙트 재생
        // 예: _artesiaCharacter.GetCharacterView().PlayAnimation("Defend");
        Debug.LogColor("아트레시아가 마지막 공격을 막아냄");

        await UniTask.Delay(1000); // 방어 애니메이션 대기

        // 전투 종료
        _isEndCombat = true;
        _isWin = true;
        InGameManager.Instance.AppEventResult = "clear";
        InGameManager.Instance.AppEventReason = "prologue_clear";
    }

    public override void StateRunning(float dt)
    {
        if (_isEndCombat)
        {
            InGameManager.Instance.IsInGameCombat = false;
            ChangeNextState(_isWin).Forget();
            return;
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

        // 마녀 공격 준비 이펙트 정리
        if (_witchAttackPrepareFx != null)
        {
            InGameVfxManager.Instance.RemoveInGameVfx(_witchAttackPrepareFx);
            _witchAttackPrepareFx = null;
        }

        // 마녀 최후의 공격 준비 이펙트 정리
        if (_witchFinalPrepareFx != null)
        {
            InGameVfxManager.Instance.RemoveInGameVfx(_witchFinalPrepareFx);
            _witchFinalPrepareFx = null;
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
