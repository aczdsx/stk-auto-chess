using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Unity.Mathematics;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;
using System.Threading.Tasks;
using CookApps.Obfuscator;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using UnityEditor.Localization.Plugins.XLIFF.V20;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Microsoft.Unity.VisualStudio.Editor;
using DG.Tweening;

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
        _clayCharacter = FindCharacterById(AllianceType.Player, PrologueID.프롤로그클레이ID); // 클레이
        _yuniCharacter = FindCharacterById(AllianceType.Player, PrologueID.프롤로그유니ID); // 유니
        _philiaCharacter = FindCharacterById(AllianceType.Player, PrologueID.프롤로그필리아ID); // 필리아
        _artesiaCharacter = FindCharacterById(AllianceType.Player, PrologueID.프롤로그아트레시아ID); // 아트레시아
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
            if (charac.GetCurrentState() is CharacterStateGroggy) continue;
            charac.AddNextState<CharacterStateReady>();
            charac.Target = null; // 타겟 제거하여 공격하지 않도록
        }

        // 적 캐릭터들 멈춤
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, characters);
        foreach (CharacterController charac in characters)
        {
            if (charac.GetCurrentState() is CharacterStateGroggy) continue;
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

    private async UniTask ActivateCharacterSkillWithWait(CharacterController character, string warningMessage = null, int skillID = 0)
    {
        if(ActivateCharacterSkill(character, warningMessage, skillID))
        {
            await UniTask.WaitUntil(() => { return (character.GetCurrentState() is CharacterStateSkill); });
            await UniTask.WaitWhile(() => { return (character.GetCurrentState() is CharacterStateSkill); });
        }
    }

    private bool ActivateCharacterSkill(CharacterController character, string warningMessage = null, int skillID = 0)
    {
        if (character == null) return false;

        List<EffectCodeCharacterBase> effectCodes = new();

        foreach (var effectCode in character.GetEffectCodeContainer().EffectCodes)
        {
            var specData = SpecDataManager.Instance.GetSpecCharacter(character.CharacterId);
            if (specData.skill_ids.Any(skillID => skillID == effectCode.CodeId))
            {
                if (effectCode is EffectCodeCharacterBase characterEffectCode)
                {
                    effectCodes.Add(characterEffectCode);
                }
            }
        }


        if (effectCodes.Count > 0)
        {
            effectCodes.Sort((a, b) => string.Compare(a.GetType().Name, b.GetType().Name, System.StringComparison.Ordinal));
            effectCodes[skillID].Activate();
            return true;
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
            // await StopAllCharacters();

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
                await TriggerClayGroggyObsolete();
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
            InGameVfxNameType.Skill_9002_3,
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
            await UniTask.WaitUntil(() => { return (_clayCharacter.GetCurrentState() is CharacterStateForceMove) || (_clayCharacter.GetCurrentState() is CharacterStateMove); });
            await UniTask.WaitWhile(() => { return (_clayCharacter.GetCurrentState() is CharacterStateForceMove) || (_clayCharacter.GetCurrentState() is CharacterStateMove); });
        }

        _clayCharacter.Target = _clayCharacter;
        await ActivateCharacterSkillWithWait(_clayCharacter, "클레이 스킬을 찾을 수 없습니다.");
        _clayCharacter.AddNextState<CharacterStateReady>();

        await UniTask.Delay(1000); // 1.5초 대기

        // 라플라스 마녀 공격 준비 이펙트 종료
        if (_witchAttackPrepareFx != null)
        {
            InGameVfxManager.Instance.RemoveInGameVfx(_witchAttackPrepareFx);
            _witchAttackPrepareFx = null;
        }

        // 라플라스 마녀 스킬 발동 (스킬 - 중)
        _witchCharacter.Target = _clayCharacter;
        ActivateCharacterSkill(_witchCharacter, "마녀 스킬을 찾을 수 없습니다.");
        await UniTask.WaitUntil(() => { return (_witchCharacter.GetCurrentState() is CharacterStateSkill); });

        await UniTask.Delay(2000);
        {
            List<UniTask> knockbacks = new();
            knockbacks.Add(YuniCharacterKnockback());
            knockbacks.Add(PhilliaCharacterKnockback());
            knockbacks.Add(ClaycharacterKnockback());
            await UniTask.WhenAll(knockbacks);
        }
        await UniTask.Delay(150);
        _clayCharacter.AddNextState<CharacterStateGroggy>();

        _witchCharacter.AddNextState<CharacterStateReady>();
        await UniTask.Delay(1500); // 1.5초 대기
    }

    // 3단계: 클레이 그로기 모션
    private async UniTask TriggerClayGroggyObsolete()
    {
        await UniTask.Delay(1000); // 1초 대기
        await ManualFreeMoveActions();
    }

    // 4단계: 아트레시아+유니+필리아 자유 공격 3초, 마녀 지친 모션
    private async UniTask TriggerFreeAttack3Seconds()
    {
        {
            List<UniTask> uniTasks = new ();
            uniTasks.Add(ActivateCharacterSkillWithWait(_artesiaCharacter));
            uniTasks.Add(ActivateCharacterSkillWithWait(_philiaCharacter));

            await UniTask.WhenAll(uniTasks);
        }
        
        if (_artesiaCharacter != null && _artesiaCharacter.IsAlive)
        {
            _artesiaCharacter.AddNextState<CharacterStateAttack>();
            _artesiaCharacter.Target = _witchCharacter;
        }
        if (_yuniCharacter != null && _yuniCharacter.IsAlive)
        {
            _yuniCharacter.AddNextState<CharacterStateAttack>();
            _yuniCharacter.Target = _witchCharacter;
        }
        if (_philiaCharacter != null && _philiaCharacter.IsAlive)
        {
            _philiaCharacter.AddNextState<CharacterStateAttack>();
            _philiaCharacter.Target = _witchCharacter;
        }
        // 라플라스 마녀도 평타 공격
        if (_witchCharacter != null && _witchCharacter.IsAlive)
        {
            _witchCharacter.AddNextState<CharacterStateAttack>();
            _witchCharacter.Target = _artesiaCharacter;
        }

        await UniTask.Delay(3000); // 3초 자유 전투
        {
            List<UniTask> uniTasks = new ();
            uniTasks.Add(ActivateCharacterSkillWithWait(_artesiaCharacter));
            uniTasks.Add(ActivateCharacterSkillWithWait(_philiaCharacter));
            
            await UniTask.WhenAll(uniTasks);
        }

        await StopAllCharacters();

        if (_witchCharacter != null && _witchCharacter.IsAlive)
        {
            _witchCharacter.AddNextState<CharacterStateGroggy>();
        }
    }

    // 5단계: 마녀 광역 스킬, 유니/필리아/클레이 다운
    private async UniTask TriggerWitchAoEAndCharactersDown()
    {
        if (_witchCharacter == null) return;

        // 마녀 지친 모션 -> 아이들로 변경
        _witchCharacter.AddNextState<CharacterStateIdle>();

        await UniTask.Delay(500); // 0.5초 대기

        // 라플라스 마녀 광역 스킬 발동
        _witchCharacter.Target = _clayCharacter;
        await ActivateCharacterSkillWithWait(_witchCharacter, "마녀 광역 작동 안함", 1);
        _witchCharacter.AddNextState<CharacterStateReady>();

        await UniTask.Delay(1000); // 1초 대기
    }

    // 6단계: 마리에 합류 + 스킬 이펙트
    private async UniTask TriggerMarieJoin()
    {
        // 마리에 마녀 뒤쪽에서 전투 합류
        _marieCharacter = FindCharacterById(AllianceType.Player, PrologueID.프롤로그마리에ID);

        if (_marieCharacter == null)
        {
            // 마녀 뒤쪽 위치 찾기 (마녀 위치 기준)
            int2 spawnPosition = _artesiaCharacter.CurrentTile.Int2Index +  new int2(-1, 0);
            InGameTile spawnTile = InGameObjectManager.Instance.InGameGrid.GetTile(spawnPosition);

            // 위치가 점유되어 있으면 주변 빈 타일 찾기
            if (spawnTile == null || spawnTile.OccupiedCharacter != null)
            {
                spawnTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile(AllianceType.Player);
            }

            if (spawnTile != null)
            {
                // 마리에 소환
                int marieCharacterId = PrologueID.프롤로그마리에ID;
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
                _marieCharacter.AddNextState<CharacterStateReady>();

                Debug.LogColor($"마리에 합류: {marieCharacterId} at ({spawnTile.X}, {spawnTile.Y})");
            }
        }
        _marieCharacter.AddNextState<CharacterStateReady>();

        await UniTask.Delay(1500); // 1.5초 대기
    }

    InGameVfx artesiaChargeVFX;

    // 7단계: 마리에 다운 (아트레시아 기모은 후 따라감, 마리에 먼저 공격)
    private async UniTask TriggerMarieDown()
    {
        // 아트레시아 0.5초 대기 후 마녀 공격
        if (_artesiaCharacter != null && _artesiaCharacter.IsAlive)
        {
            artesiaChargeVFX.Remove();
            artesiaChargeVFX = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_sn_aura_01, _artesiaCharacter.SkillRootTransformFollowable);
            await UniTask.Delay(500);
            _artesiaCharacter.AddNextState<CharacterStateIdle>();
            _artesiaCharacter.Target = _witchCharacter;
        }

        // 마리에 마녀 공격
        if (_marieCharacter != null && _marieCharacter.IsAlive)
        {
            // 거의 동시에 마리에 스킬 이펙트 (마리에 디버프 스킬 발동)
            await ActivateCharacterSkillWithWait(_marieCharacter, "마리에 스킬을 찾을 수 없습니다.");
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

        await StopAllCharacters();
        await UniTask.NextFrame();
        _witchCharacter.GetCharacterView().LookAt(_witchCharacter.CurrentTile, _artesiaCharacter.CurrentTile);
        await UniTask.NextFrame();

        _witchCharacter.AddNextState<CharacterStateGroggy>();

        await UniTask.Delay(1000); // 1초 대기
    }

    // 8단계: 아트레시아 초신성 모드 진입 + 스킬 발동
    private async UniTask TriggerArtesiaSupernovaAndSkill()
    {
        // 아트레시아 초신성 모드 진입
        // ✅ 스킬 추가 ✅
        // [TODO] 초신성 모드 활성화 (버프/이펙트 등)
        if (_artesiaCharacter != null)
        {
            artesiaChargeVFX = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_sn_aura_03, _artesiaCharacter.SkillRootTransformFollowable);
        }

        // 아트레시아 스킬 발동
        _artesiaCharacter.Target = _witchCharacter;
        await ActivateCharacterSkillWithWait(_artesiaCharacter, "아트레시아 스킬을 찾을 수 없습니다.", 1);
        _artesiaCharacter.AddNextState<CharacterStateReady>();
        await UniTask.Delay(1500); // 1.5초 대기
        artesiaChargeVFX.Remove();
    }

    private InGameVfx _witchFinalPrepareFx; // 마녀 최후의 공격 준비 이펙트

    // 9단계: 마녀 체력 회복, 최후 스킬 대기 모션
    private async UniTask TriggerWitchHpRecoverAndFinalPrepare()
    {
        if (_witchCharacter == null) return;

        // 라플라스 마녀 체력 회복
        // [TODO] 체력 회복 처리
        Debug.LogColor("라플라스 마녀 체력 회복");
        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.Skill_9002_4, _witchCharacter.SkillRootTransformFollowable);
        await UniTask.Delay(1000);
        _witchCharacter.AddNextState<CharacterStateReady>();
        _witchCharacter.GetHealed(9999.0f, _witchCharacter, 0);
        await UniTask.Delay(1000);

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
            InGameVfxNameType.Skill_9002_3,
            _witchCharacter.SkillRootTransformFollowable);

        // [TODO] 필요시 더 강력한 이펙트로 변경
        // 예: _witchCharacter.GetCharacterView().PlayEffect("WitchFinalPrepare");

        await UniTask.Delay(1000); // 1초 대기
    }

    // 11단계: 마녀 최후 공격, 아트레시아 방어, 전투 종료
    private async UniTask TriggerWitchFinalAttackAndArtesiaDefend()
    {
        // ! 암전

        var whiteOutImage = GameObject.Find("WhiteOutUICover").GetComponent<UnityEngine.UI.Image>();
        whiteOutImage.DOFade(1f, 1.5f);

        // if (_witchCharacter == null || _artesiaCharacter == null) return;

        // 마녀 최후의 공격 준비 이펙트 제거
        if (_witchFinalPrepareFx != null)
        {
            InGameVfxManager.Instance.RemoveInGameVfx(_witchFinalPrepareFx);
            _witchFinalPrepareFx = null;
        }

        // // 라플라스 마녀 최후의 공격 시전
        // ActivateCharacterSkill(_witchCharacter, "마녀 최후의 공격 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(1000); // 공격 애니메이션 대기

        // 아트레시아가 마지막 공격을 막아냄
        // [TODO] 방어/막기 애니메이션/이펙트 재생
        _artesiaCharacter.GetCharacterView().PlayAnimation(AnimationKey.PARRY);
        Debug.LogColor("아트레시아가 마지막 공격을 막아냄");


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

    private async UniTask ManualFreeMoveActions()
    {
        if (_artesiaCharacter != null && _artesiaCharacter.IsAlive)
            ArtesiaMovement().Forget();
        if (_yuniCharacter != null && _yuniCharacter.IsAlive)
            YuniMovement().Forget();
        if (_philiaCharacter != null && _philiaCharacter.IsAlive)
            PhilliaMovement().Forget();

    }

    private async UniTask YuniCharacterKnockback()
    {
        MoveCharacterToDirection(_yuniCharacter, -1, -1, 10);

        await UniTask.WaitUntil(() => { return (_yuniCharacter.GetCurrentState() is CharacterStateForceMove) || (_yuniCharacter.GetCurrentState() is CharacterStateMove); });
        await UniTask.WaitWhile(() => { return (_yuniCharacter.GetCurrentState() is CharacterStateForceMove) || (_yuniCharacter.GetCurrentState() is CharacterStateMove); });
        await UniTask.NextFrame();
        _yuniCharacter.GetCharacterView().LookAt(_yuniCharacter.CurrentTile, _witchCharacter.CurrentTile);
        await UniTask.NextFrame();
    }
    
    private async UniTask PhilliaCharacterKnockback()
    {
        MoveCharacterToDirection(_philiaCharacter, 1, -1, 10);

        await UniTask.WaitUntil(() => { return (_philiaCharacter.GetCurrentState() is CharacterStateForceMove) || (_philiaCharacter.GetCurrentState() is CharacterStateMove); });
        await UniTask.WaitWhile(() => { return (_philiaCharacter.GetCurrentState() is CharacterStateForceMove) || (_philiaCharacter.GetCurrentState() is CharacterStateMove); });
        await UniTask.NextFrame();
        _philiaCharacter.GetCharacterView().LookAt(_philiaCharacter.CurrentTile, _witchCharacter.CurrentTile);
        await UniTask.NextFrame();
    }
    
    private async UniTask ClaycharacterKnockback()
    {
        MoveCharacterToDirection(_clayCharacter, 0, -1, 10);

        await UniTask.WaitUntil(() => { return (_clayCharacter.GetCurrentState() is CharacterStateForceMove) || (_clayCharacter.GetCurrentState() is CharacterStateMove); });
        await UniTask.WaitWhile(() => { return (_clayCharacter.GetCurrentState() is CharacterStateForceMove) || (_clayCharacter.GetCurrentState() is CharacterStateMove); });

        await UniTask.NextFrame();
        _clayCharacter.GetCharacterView().LookAt(_clayCharacter.CurrentTile, _witchCharacter.CurrentTile);
        await UniTask.NextFrame();
    }
    
    private async UniTask ArtesiaMovement()
    {
        // 클레이 중앙으로 포지션 이동
        MoveCharacterToDirection(_artesiaCharacter, -1, 1, 3);
        {
            await UniTask.WaitUntil(() => { return (_artesiaCharacter.GetCurrentState() is CharacterStateForceMove) || (_artesiaCharacter.GetCurrentState() is CharacterStateMove); });
            await UniTask.WaitWhile(() => { return (_artesiaCharacter.GetCurrentState() is CharacterStateForceMove) || (_artesiaCharacter.GetCurrentState() is CharacterStateMove); });
        }
        // 클레이 중앙으로 포지션 이동
        MoveCharacterToDirection(_artesiaCharacter, 0, 1, 3);
        {
            await UniTask.WaitUntil(() => { return (_artesiaCharacter.GetCurrentState() is CharacterStateForceMove) || (_artesiaCharacter.GetCurrentState() is CharacterStateMove); });
            await UniTask.WaitWhile(() => { return (_artesiaCharacter.GetCurrentState() is CharacterStateForceMove) || (_artesiaCharacter.GetCurrentState() is CharacterStateMove); });
        }
        // 클레이 중앙으로 포지션 이동
        if (MoveCharacterToDirection(_artesiaCharacter, 1, 1, 3))
        {
            await UniTask.WaitUntil(() => { return (_artesiaCharacter.GetCurrentState() is CharacterStateForceMove) || (_artesiaCharacter.GetCurrentState() is CharacterStateMove); });
            await UniTask.WaitWhile(() => { return (_artesiaCharacter.GetCurrentState() is CharacterStateForceMove) || (_clayCharacter.GetCurrentState() is CharacterStateMove); });
        }
        else
        {
            var destTileIdx = _artesiaCharacter.CurrentTile.Int2Index + new Unity.Mathematics.int2(1, 1);
            var tile = InGameObjectManager.Instance.InGameGrid.GetTile(destTileIdx);
        }

    }

    private async UniTask YuniMovement()
    {
        MoveCharacterToDirection(_yuniCharacter, 0, 2, 0.666f);
        await UniTask.WaitUntil(() => { return (_yuniCharacter.GetCurrentState() is CharacterStateForceMove) || (_yuniCharacter.GetCurrentState() is CharacterStateMove); });
        await UniTask.WaitWhile(() => { return (_yuniCharacter.GetCurrentState() is CharacterStateForceMove) || (_yuniCharacter.GetCurrentState() is CharacterStateMove); });

    }

    private async UniTask PhilliaMovement()
    {
        MoveCharacterToDirection(_philiaCharacter, 0, 2, 0.666f);
        await UniTask.WaitUntil(() => { return (_philiaCharacter.GetCurrentState() is CharacterStateForceMove) || (_philiaCharacter.GetCurrentState() is CharacterStateMove); });
        await UniTask.WaitWhile(() => { return (_philiaCharacter.GetCurrentState() is CharacterStateForceMove) || (_philiaCharacter.GetCurrentState() is CharacterStateMove); });
    }

}