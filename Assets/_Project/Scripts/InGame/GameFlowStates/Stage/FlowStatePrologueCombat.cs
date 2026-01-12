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

        InGameSynergyManager.Instance.ClearSynergyFx();
        InGameMain.GetInGameMain().SetActiveObjectMover(false);
        InGameMain.GetInGameMain().InitCombatStateUI();
        InGameObjectManager.Instance.SaveStartingPlayerCharacter();
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Player);
        InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Enemy);

        InGameObjectManager.Instance.InGameStage.GraduallyChangeBoardColor(Color.gray, 1.0f);

        ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(5.0f, new Vector3(-20, 2.5f, -10), 1.0f).Forget();
        // ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(7.0f, new Vector3(0, 2.5f, -10), 1.0f).Forget();
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
            var effectCodes = character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
                EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }

        {
            var effectCodes =
                InGameManager.Instance.TeamEcc.GetCharacterEffectCodesByFlag(
                    EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }


        // 캐릭터 찾기
        _clayCharacter = FindCharacterById(AllianceType.Player, 3404); // 클레이
        _yuniCharacter = FindCharacterById(AllianceType.Player, 2102); // 유니
        _philiaCharacter = FindCharacterById(AllianceType.Player, 2401); // 필리아
        _artesiaCharacter = FindCharacterById(AllianceType.Player, 3401); // 아트레시아
        // 마리에는 6단계에서 합류하므로 초기에는 찾지 않음

        // 라플라스 마녀 찾기 (적)
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, characters);
        if (characters.Count > 0)
        {
            _witchCharacter = characters[0];
        }

        // 캐릭터 초기 이동 후 시나리오 시작
        InitializeCharacterPositionsAndStartScenario().Forget();
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

        if (characters == null)
            return;

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


    /// <summary>
    /// 캐릭터 초기 위치 설정 및 시나리오 시작
    /// </summary>
    private async UniTask InitializeCharacterPositionsAndStartScenario()
    {
        // 모든 캐릭터를 Ready 상태로 변경하여 이동 가능하도록 함
        await StopAllCharacters();

        ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(7.0f, new Vector3(0, 2.5f, -10), 3.0f).Forget();

        MoveCharacterToDirection(_clayCharacter, 0, 4, 0.4f);
        MoveCharacterToDirection(_yuniCharacter, 0, 4, 0.4f);
        MoveCharacterToDirection(_philiaCharacter, 0, 4, 0.4f);
        MoveCharacterToDirection(_artesiaCharacter, 0, 4, 0.4f);
        // 이동 완료 후 모든 캐릭터 멈춤
        await UniTask.Delay(2000);
        await StopAllCharacters();

        // 시나리오 시작
        await InitializePrologueScenario();
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
    private bool MoveCharacterToDirection(CharacterController character, int deltaX, int deltaY, float? customMoveSpeed = null)
    {
        if (character == null || character.CurrentTile == null)
            return false;

        int2 targetPosition = new int2(character.CurrentTile.X + deltaX, character.CurrentTile.Y + deltaY);
        Debug.LogColor($"{character.CharacterId} 캐릭터 이동 시작 -> ({targetPosition.x}, {targetPosition.y})");
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
        // if (character.GetCurrentState() is CharacterStateReady)
        // {
        //     character.AddNextState<CharacterStateIdle>();
        // }

        // MoveToTile을 사용하여 최적의 경로로 이동
        character.ForceMoveTile(targetTile, customMoveSpeed);
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
            var specData = SpecDataManager.Instance.GetSpecCharacter(character.CharacterId);
            if (specData.skill_ids.Any(skillId => skillId == effectCode.CodeId))
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
        WitchAttackPrepare,              // 1단계: 마녀 스킬 준비 모션/이펙트
        ClaySkillAndWitchAttack,         // 2단계: 클레이 중앙 이동, 스킬 발동, 마녀 스킬 막기
        ClayGroggy,                      // 3단계: 클레이 그로기 모션
        FreeAttack3Seconds,              // 4단계: 클레이+유니+필리아 자유 공격 3초, 마녀 지친 모션
        WitchAoEAndCharactersDown,       // 5단계: 마녀 광역 스킬, 유니/필리아/클레이 다운
        MarieJoin,                       // 6단계: 마리에 합류 + 스킬 이펙트
        MarieDown,                       // 7단계: 마리에 다운 (아트레시아 기모은 후 따라감, 마리에 먼저 공격)
        ArtesiaSupernovaAndSkill,        // 8단계: 아트레시아 초신성 모드 진입 + 스킬 발동
        WitchHpRecoverAndFinalPrepare,  // 9단계: 마녀 체력 회복, 최후 스킬 대기 모션
        WitchFinalPrepareFx,             // 10단계: 마녀 최후 공격 준비 이펙트
        WitchFinalAttackAndArtesiaDefend // 11단계: 마녀 최후 공격, 아트레시아 방어, 전투 종료
    }

    private static readonly PrologueScenarioData[] PrologueScenarioSteps = new PrologueScenarioData[]
    {
        // 1단계: 마녀의 선제공격 (DialogueGroupID : 200001)
        new PrologueScenarioData { step = 1, dialogueID = 200001, actionType = PrologueActionType.WitchAttackPrepare },
        
        // 2단계: 클레이의 방어 (DialogueGroupID : 200002)
        new PrologueScenarioData { step = 2, dialogueID = 200002, actionType = PrologueActionType.ClaySkillAndWitchAttack },
        
        // 3단계: 클레이의 맹세 (DialogueGroupID : 200003)
        new PrologueScenarioData { step = 3, dialogueID = 200003, actionType = PrologueActionType.ClayGroggy },
        
        // 4단계: 유니 & 필리아의 반격 (DialogueGroupID : 200004)
        new PrologueScenarioData { step = 4, dialogueID = 200004, actionType = PrologueActionType.FreeAttack3Seconds },
        
        // 5단계: 마녀의 역습 (DialogueGroupID : 200005)
        new PrologueScenarioData { step = 5, dialogueID = 200005, actionType = PrologueActionType.WitchAoEAndCharactersDown },
        
        // 6단계: 마리에 합류 (DialogueGroupID : 200006)
        new PrologueScenarioData { step = 6, dialogueID = 200006, actionType = PrologueActionType.MarieJoin },
        
        // 7단계: 마리에의 희생과 아트레시아의 각성 (DialogueGroupID : 200007)
        new PrologueScenarioData { step = 7, dialogueID = 200007, actionType = PrologueActionType.MarieDown },
        
        // 8단계: 마리에의 희생 (DialogueGroupID : 200008)
        new PrologueScenarioData { step = 8, dialogueID = 200008, actionType = PrologueActionType.ArtesiaSupernovaAndSkill },
        
        // 9단계: 아트레시아의 결의 (DialogueGroupID : 200009)
        new PrologueScenarioData { step = 9, dialogueID = 200009, actionType = PrologueActionType.WitchHpRecoverAndFinalPrepare },
        
        // 10단계: 마녀의 진정한 힘 (DialogueGroupID : 200010)
        new PrologueScenarioData { step = 10, dialogueID = 200010, actionType = PrologueActionType.WitchFinalPrepareFx },
        
        // 11단계: 최후의 순간과 아트레시아의 결의 (DialogueGroupID : 200011)
        new PrologueScenarioData { step = 11, dialogueID = 200011, actionType = PrologueActionType.WitchFinalAttackAndArtesiaDefend }
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
            case PrologueActionType.ClaySkillAndWitchAttack:
                await TriggerClaySkillAndWitchAttack();
                break;
            case PrologueActionType.ClayGroggy:
                await TriggerClayGroggy();
                break;
            case PrologueActionType.FreeAttack3Seconds:
                await TriggerFreeAttack3Seconds();
                break;
            case PrologueActionType.WitchAoEAndCharactersDown:
                await TriggerWitchAoEAndCharactersDown();
                break;
            case PrologueActionType.MarieJoin:
                await TriggerMarieJoin();
                break;
            case PrologueActionType.MarieDown:
                await TriggerMarieDown();
                break;
            case PrologueActionType.ArtesiaSupernovaAndSkill:
                await TriggerArtesiaSupernovaAndSkill();
                break;
            case PrologueActionType.WitchHpRecoverAndFinalPrepare:
                await TriggerWitchHpRecoverAndFinalPrepare();
                break;
            case PrologueActionType.WitchFinalPrepareFx:
                await TriggerWitchFinalPrepareFx();
                break;
            case PrologueActionType.WitchFinalAttackAndArtesiaDefend:
                await TriggerWitchFinalAttackAndArtesiaDefend();
                break;
            case PrologueActionType.None:
            default:
                await UniTask.Delay(2000); // 기본 대기 시간 (2초)
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
            InGameVfxNameType.fx_common_synergy_lightning_01,
            _witchCharacter.SkillRootTransformFollowable);

        await UniTask.Delay(1500); // 1.5초 대기
    }

    // 2단계: 클레이 중앙 포지션 이동, 스킬 발동, 마녀 스킬 막기
    private async UniTask TriggerClaySkillAndWitchAttack()
    {
        if (_clayCharacter == null) return;

        // 클레이 중앙으로 포지션 이동
        if (MoveCharacterToDirection(_clayCharacter, 0, 1))
        {
            // 이동 완료 대기
            await WaitForCharacterMoveComplete(_clayCharacter);
        }

        // 클레이 스킬 발동
        ActivateCharacterSkill(_clayCharacter, "클레이 스킬을 찾을 수 없습니다.");

        // 라플라스 마녀 공격 준비 이펙트 종료
        if (_witchAttackPrepareFx != null)
        {
            InGameVfxManager.Instance.RemoveInGameVfx(_witchAttackPrepareFx);
            _witchAttackPrepareFx = null;
        }

        // 라플라스 마녀 스킬 발동 (스킬 - 중)
        ActivateCharacterSkill(_witchCharacter, "마녀 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(1500); // 1.5초 대기
    }

    // 3단계: 클레이 그로기 모션
    private async UniTask TriggerClayGroggy()
    {
        if (_clayCharacter == null) return;

        // 클레이 반동으로 그로기 → 모션 (쉬는 모션)
        // [TODO] 그로기 모션 재생
        Debug.LogColor("클레이 그로기 모션 재생");

        await UniTask.Delay(1000); // 1초 대기
    }

    // 4단계: 아트레시아+유니+필리아 자유 공격 3초, 마녀 지친 모션
    private async UniTask TriggerFreeAttack3Seconds()
    {
        // 아트레시아 + 유니 + 필리아 자유롭게 공격 (Idle 상태)
        if (_artesiaCharacter != null && _artesiaCharacter.IsAlive)
        {
            _artesiaCharacter.AddNextState<CharacterStateIdle>();
            _artesiaCharacter.Target = _witchCharacter;
        }
        if (_yuniCharacter != null && _yuniCharacter.IsAlive)
        {
            _yuniCharacter.AddNextState<CharacterStateIdle>();
            _yuniCharacter.Target = _witchCharacter;
        }
        if (_philiaCharacter != null && _philiaCharacter.IsAlive)
        {
            _philiaCharacter.AddNextState<CharacterStateIdle>();
            _philiaCharacter.Target = _witchCharacter;
        }

        // 라플라스 마녀도 평타 공격
        if (_witchCharacter != null && _witchCharacter.IsAlive)
        {
            _witchCharacter.AddNextState<CharacterStateIdle>();
        }

        await UniTask.Delay(3000); // 3초 자유 전투

        // 마녀 지친 모션 (아직 state 개발 안됨)
        if (_witchCharacter != null && _witchCharacter.IsAlive)
        {
            // [TODO] 지친 모션 재생
            Debug.LogColor("라플라스 마녀 지친 모션");
        }

        // 모든 캐릭터 정지
        await StopAllCharacters();
    }

    // 5단계: 마녀 광역 스킬, 유니/필리아/클레이 다운
    private async UniTask TriggerWitchAoEAndCharactersDown()
    {
        if (_witchCharacter == null) return;

        // 마녀 지친 모션 -> 아이들로 변경
        _witchCharacter.AddNextState<CharacterStateIdle>();

        await UniTask.Delay(500); // 0.5초 대기

        // 라플라스 마녀 광역 스킬 발동
        ActivateCharacterSkill(_witchCharacter, "마녀 광역 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(2000); // 2초 대기

        // 유니, 필리아, 클레이 → Dead 상태
        if (_yuniCharacter != null && _yuniCharacter.IsAlive)
            _yuniCharacter.AddNextState<CharacterStateDead>();
        if (_philiaCharacter != null && _philiaCharacter.IsAlive)
            _philiaCharacter.AddNextState<CharacterStateDead>();
        if (_clayCharacter != null && _clayCharacter.IsAlive)
            _clayCharacter.AddNextState<CharacterStateDead>();

        await UniTask.Delay(1000); // 1초 대기
    }

    // 6단계: 마리에 합류 + 스킬 이펙트
    private async UniTask TriggerMarieJoin()
    {
        // 마리에 마녀 뒤쪽에서 전투 합류
        _marieCharacter = FindCharacterById(AllianceType.Player, 130501);

        if (_marieCharacter == null)
        {
            // 마녀 뒤쪽 위치 찾기 (마녀 위치 기준)
            int2 spawnPosition = new int2(2, 7);
            InGameTile spawnTile = InGameObjectManager.Instance.InGameGrid.GetTile(spawnPosition);

            // 위치가 점유되어 있으면 주변 빈 타일 찾기
            if (spawnTile == null || spawnTile.OccupiedCharacter != null)
            {
                spawnTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile(AllianceType.Player);
            }

            if (spawnTile != null)
            {
                // 마리에 소환
                int marieCharacterId = 117563405;
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

        // 거의 동시에 마리에 스킬 이펙트 (마리에 디버프 스킬 발동)
        ActivateCharacterSkill(_marieCharacter, "마리에 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(1500); // 1.5초 대기
    }

    // 7단계: 마리에 다운 (아트레시아 기모은 후 따라감, 마리에 먼저 공격)
    private async UniTask TriggerMarieDown()
    {
        // 아트레시아 0.5초 대기 후 마녀 공격
        if (_artesiaCharacter != null && _artesiaCharacter.IsAlive)
        {
            await UniTask.Delay(500);
            _artesiaCharacter.AddNextState<CharacterStateIdle>();
            _artesiaCharacter.Target = _witchCharacter;
        }

        // 마리에 마녀 공격
        if (_marieCharacter != null && _marieCharacter.IsAlive)
        {
            _marieCharacter.AddNextState<CharacterStateIdle>();
            _marieCharacter.Target = _witchCharacter;
        }

        // 마녀 → 마리에 공격
        if (_witchCharacter != null && _witchCharacter.IsAlive)
        {
            _witchCharacter.AddNextState<CharacterStateIdle>();
            _witchCharacter.Target = _marieCharacter;
        }

        await UniTask.Delay(1000); // 1초 대기

        // 마리에 → Dead 상태
        if (_marieCharacter != null && _marieCharacter.IsAlive)
        {
            _marieCharacter.AddNextState<CharacterStateDead>();
        }

        // 마녀 그로기로 변경 (state 아직 개발 안됨)
        if (_witchCharacter != null && _witchCharacter.IsAlive)
        {
            // [TODO] 그로기 모션 재생
            Debug.LogColor("라플라스 마녀 그로기 모션");
        }

        await UniTask.Delay(1000); // 1초 대기
    }

    // 8단계: 아트레시아 초신성 모드 진입 + 스킬 발동
    private async UniTask TriggerArtesiaSupernovaAndSkill()
    {
        // 아트레시아 초신성 모드 진입
        // [TODO] 초신성 모드 활성화 (버프/이펙트 등)
        if (_artesiaCharacter != null)
        {
            Debug.LogColor("아트레시아 초신성 모드 진입");
        }

        // 아트레시아 스킬 발동
        ActivateCharacterSkill(_artesiaCharacter, "아트레시아 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(1500); // 1.5초 대기
    }

    private InGameVfx _witchFinalPrepareFx; // 마녀 최후의 공격 준비 이펙트

    // 9단계: 마녀 체력 회복, 최후 스킬 대기 모션
    private async UniTask TriggerWitchHpRecoverAndFinalPrepare()
    {
        if (_witchCharacter == null) return;

        // 라플라스 마녀 체력 회복
        // [TODO] 체력 회복 처리
        Debug.LogColor("라플라스 마녀 체력 회복");

        // 라플라스 마녀 최후 스킬 대기 모션
        // [TODO] 대기 모션 재생
        Debug.LogColor("라플라스 마녀 최후 스킬 대기 모션");

        await UniTask.Delay(1500); // 1.5초 대기
    }

    // 10단계: 마녀 최후 공격 준비 이펙트
    private async UniTask TriggerWitchFinalPrepareFx()
    {
        if (_witchCharacter == null) return;

        // 라플라스 마녀 최후의 공격 준비 이펙트 재생
        // 암흑 힘 응집 이펙트 (마녀를 감싸는 이펙트)
        _witchFinalPrepareFx = InGameVfxManager.Instance.AddInGameVfx(
            InGameVfxNameType.fx_common_synergy_lightning_01,
            _witchCharacter.SkillRootTransformFollowable);

        // [TODO] 필요시 더 강력한 이펙트로 변경
        // 예: _witchCharacter.GetCharacterView().PlayEffect("WitchFinalPrepare");

        await UniTask.Delay(1000); // 1초 대기
    }

    // 11단계: 마녀 최후 공격, 아트레시아 방어, 전투 종료
    private async UniTask TriggerWitchFinalAttackAndArtesiaDefend()
    {
        if (_witchCharacter == null || _artesiaCharacter == null) return;

        // 마녀 최후의 공격 준비 이펙트 제거
        if (_witchFinalPrepareFx != null)
        {
            InGameVfxManager.Instance.RemoveInGameVfx(_witchFinalPrepareFx);
            _witchFinalPrepareFx = null;
        }

        // 라플라스 마녀 최후의 공격 시전
        ActivateCharacterSkill(_witchCharacter, "마녀 최후의 공격 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(1000); // 공격 애니메이션 대기

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
            character.RemoveSynergyEffectCodeALL();
        }

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Enemy))
        {
            character.RemoveSynergyEffectCodeALL();
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
        InGameMainFlowManager.Instance.AddNextState<FlowStatePrologueClear>();
    }
}
