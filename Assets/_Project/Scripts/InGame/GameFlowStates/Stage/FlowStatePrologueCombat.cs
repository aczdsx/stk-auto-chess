using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
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

        InGameCommanderManager.Instance.InGameCamera.SetCameraSize(7.0f, new Vector3(0, 2.5f, -10), 1.0f).Forget();

        InGameObjectManager.Instance.ClearTargetLine();
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

        foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Neutral))
        {
            character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
        }

        // InGameManager.Instance.AddSynergyEffectCode(AllianceType.Player);
        // InGameManager.Instance.AddSynergyEffectCode(AllianceType.Enemy);

        {
            var effectCodes =
                InGameManager.Instance.EffectCodeContainer.GetCharacterEffectCodesByFlag(
                    EffectCodeInheritFlag.UseOnCombatStart);
            EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
        }


        InitializePrologueScenario().Forget();
    }

    private async UniTask StartAllCharacters()
    {
        // 전투 시작 후 1초는 대기
        await UniTask.Delay(1000);

        // 모든 캐릭터 락 해제
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Player, characters);
        foreach (CharacterController charac in characters)
        {
            if (charac.SpecCharacter.character_position_type == SynergyType.ASSASSIN)
                charac.AddNextState<CharacterStateAssassinFirstMove>();
            else
                charac.AddNextState<CharacterStateIdle>();

            charac.Target = InGameObjectManager.Instance.GetNearestTargetOnce(charac);
        }
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, characters);
        foreach (CharacterController charac in characters)
        {
            if (charac.SpecCharacter.character_position_type == SynergyType.ASSASSIN)
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

    // 모든 캐릭터를 Ready 상태로 변경하여 공격을 멈춤
    private async UniTask StopAllCharacters()
    {
        // 전투 시작 후 1초는 대기
        await UniTask.Delay(1000);

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

        // 중립 캐릭터들 멈춤
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Neutral, characters);
        foreach (CharacterController charac in characters)
        {
            charac.AddNextState<CharacterStateReady>();
        }
    }


    private async UniTask InitializePrologueScenario()
    {
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
        // 1단계: 마녀의 선제공격 (DialogueID : 10001)
        new PrologueScenarioData { step = 1, dialogueID = 2001, guideText = "", actionType = PrologueActionType.WitchAttackPrepare },
        
        // 2단계: 클레이의 방어 (DialogueID : 10002)
        new PrologueScenarioData { step = 2, dialogueID = 2001, guideText = "", actionType = PrologueActionType.ClaySkill },
        
        // 3단계: 클레이 탈락 (DialogueID : 10003)
        new PrologueScenarioData { step = 3, dialogueID = 2001, guideText = "라플라스 마녀의 강력한 공격을 클레이의 스킬로 막아냈습니다.\n클레이의 스킬은 쉴드와 회복 효과를 지니고 있습니다.", actionType = PrologueActionType.WitchAttack2AndClayDown },
        
        // 4단계: 유니&필리아의 반격 (DialogueID : 10004)
        new PrologueScenarioData { step = 4, dialogueID = 2001, guideText = "라플라스 마녀가 지친 지금이 공격할 타이밍 입니다.\n유니와 필리아의 스킬은 강력한 물리/마법 공격력을 지닙니다.", actionType = PrologueActionType.YuniPhiliaSkillAndWitchGroggy },
        
        // 5단계: 마녀의 역습 (DialogueID : 10005)
        new PrologueScenarioData { step = 5, dialogueID = 2001, guideText = "", actionType = PrologueActionType.WitchAoEAttackAndMarieJoin },
        
        // 6단계: 마리에 합류 (DialogueID : 10006)
        new PrologueScenarioData { step = 6, dialogueID = 2001, guideText = "마리에의 스킬은 적의 공격력과 방어력을 낮추는 효과를 지니고 있습니다.", actionType = PrologueActionType.MarieSkillAndArtesiaSupernova },
        
        // 7단계: 아트레시아의 각성 (DialogueID : 10007)
        new PrologueScenarioData { step = 7, dialogueID = 2001, guideText = "아트레시아의 스킬은 강력한 범위 공격을 진행합니다.", actionType = PrologueActionType.ArtesiaSkill },
        
        // 8단계: 마녀의 진정한 힘 (DialogueID : 10008)
        new PrologueScenarioData { step = 8, dialogueID = 2001, guideText = "", actionType = PrologueActionType.WitchFinalPrepare },
        
        // 9단계: 최후의 순간 (DialogueID : 10009)
        new PrologueScenarioData { step = 9, dialogueID = 2001, guideText = "", actionType = PrologueActionType.EndCombat }
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

            await StartAllCharacters();

            // 가이드 문구 표시 (있는 경우)
            if (!string.IsNullOrEmpty(scenarioData.guideText))
            {
                ToastManager.Instance?.ShowToast(scenarioData.guideText, true);
                await UniTask.Delay(1000); // 가이드 문구 표시 대기
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
            case PrologueActionType.EndCombat:
                _isEndCombat = true;
                _isWin = true;
                InGameManager.Instance.AppEventResult = "clear";
                InGameManager.Instance.AppEventReason = "prologue_clear";
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
            InGameVfxNameType.fx_common_cast_darkness,
            _witchCharacter.SkillRootTransformFollowable);

        await UniTask.Delay(1000); // 1초 대기
    }

    // 2단계: 클레이 스킬 + 마녀 공격/준비 이펙트 Off
    private async UniTask TriggerClaySkill()
    {
        if (_clayCharacter == null) return;

        // 클레이 스킬 강제 발동
        ActivateCharacterSkill(_clayCharacter, "클레이 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(1000); // 스킬 애니메이션 대기 (1초)
    }

    // 3단계: 마녀 공격2 + 클레이 죽음
    private async UniTask TriggerWitchAttack2AndClayDown()
    {
        if (_witchCharacter == null) return;

        // 마녀 공격 준비 이펙트 제거
        if (_witchAttackPrepareFx != null)
        {
            InGameVfxManager.Instance.RemoveInGameVfx(_witchAttackPrepareFx);
            _witchAttackPrepareFx = null;
        }

        // 마녀 공격2 스킬 발동
        ActivateCharacterSkill(_witchCharacter, "마녀 공격2 스킬을 찾을 수 없습니다.");

        await UniTask.Delay(1000); // 공격 애니메이션 대기

        // 클레이 죽음
        if (_clayCharacter != null && _clayCharacter.IsAlive)
        {
            _clayCharacter.AddNextState<CharacterStateDead>();
        }

        await UniTask.Delay(2000); // 총 1초 대기
    }

    // 4단계: 유니/필리아 스킬 + 마녀 그로기
    private async UniTask TriggerYuniPhiliaSkillAndWitchGroggy()
    {
        // 유니 스킬 발동
        ActivateCharacterSkill(_yuniCharacter);
        // 필리아 스킬 발동
        ActivateCharacterSkill(_philiaCharacter);
        // 다른 캐릭터들도 라플라스 마녀 계속 공격
        StartAllCharacters().Forget();

        await UniTask.Delay(2000); // 스킬 애니메이션 대기

        if (_witchCharacter != null && _witchCharacter.IsAlive)
        {
            // [TODO] 그로기 상태 애니메이션/이펙트
            // 예: _witchCharacter.GetCharacterView().PlayAnimation("Groggy");
            Debug.LogColor("라플라스 마녀 그로기 상태, HP 1로 설정");
        }

        await UniTask.Delay(1000); // 총 1초 대기
    }

    // 5단계: 마녀 광역 공격 + 유니/필리아 죽음 + 마리에 합류
    private async UniTask TriggerWitchAoEAttackAndMarieJoin()
    {
        if (_witchCharacter == null) return;

        // 마녀 최종 스킬 발동
        ActivateCharacterSkill(_witchCharacter);

        await UniTask.Delay(1000); // 스킬 애니메이션 대기

        // 유니, 필리아 죽음
        if (_yuniCharacter != null && _yuniCharacter.IsAlive)
            _yuniCharacter.AddNextState<CharacterStateDead>();
        if (_philiaCharacter != null && _philiaCharacter.IsAlive)
            _philiaCharacter.AddNextState<CharacterStateDead>();

        await UniTask.Delay(1000);

        // 마리에 마녀 뒤로 전투 합류
        // 먼저 마리에가 이미 필드에 있는지 확인
        _marieCharacter = FindCharacterById(AllianceType.Player, 130501); // [TODO] 실제 마리에 캐릭터 ID로 변경 필요

        if (_marieCharacter == null && _witchCharacter != null)
        {
            var witchTile = _witchCharacter.CurrentTile;
            // 마녀 뒤쪽 위치 계산 (적 진영은 Y가 큰 쪽이 뒤, 플레이어 진영은 Y가 작은 쪽이 앞)
            // 마녀 위치에서 Y+1 위치를 찾되, 없으면 주변 빈 타일 찾기
            int2 behindPosition = new int2(witchTile.X, witchTile.Y + 1);
            InGameTile spawnTile = null;

            // 마녀 바로 뒤 타일 확인 (플레이어 진영)
            if (behindPosition.y < InGameObjectManager.Instance.InGameGrid.Height)
            {
                var candidateTile = InGameObjectManager.Instance.InGameGrid.GetTile(behindPosition);
                if (candidateTile != null && candidateTile.OccupiedCharacter == null &&
                    candidateTile.View.AllianceType == AllianceType.Player)
                {
                    spawnTile = candidateTile;
                }
            }

            // 바로 뒤가 없으면 주변 빈 타일 찾기
            if (spawnTile == null)
            {
                // 마녀 주변 1칸 거리 내 빈 타일 찾기
                var nearbyTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByManhattanDistance(witchTile, 1);
                foreach (var tile in nearbyTiles)
                {
                    if (tile.OccupiedCharacter == null && tile.View.AllianceType == AllianceType.Player)
                    {
                        // Y가 더 큰(뒤쪽) 타일 우선
                        if (spawnTile == null || tile.Y > spawnTile.Y)
                        {
                            spawnTile = tile;
                        }
                    }
                }
            }

            // 여전히 없으면 플레이어 진영의 빈 타일 중 하나 선택
            if (spawnTile == null)
            {
                spawnTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile(AllianceType.Player);
            }

            if (spawnTile != null)
            {
                // 마리에 소환
                int marieCharacterId = 130501; // [TODO] 실제 마리에 캐릭터 ID로 변경 필요
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

        await UniTask.Delay(1000); // 총 1초 대기
    }

    // 6단계: 마리에 스킬 + 아트레시아 초신성 모드
    private async UniTask TriggerMarieSkillAndArtesiaSupernova()
    {
        // 마리에 스킬 발동
        ActivateCharacterSkill(_marieCharacter);

        await UniTask.Delay(1000);

        // 아트레시아 초신성 모드
        // [TODO] 초신성 모드 활성화 (버프/이펙트 등)
        // 예: _artesiaCharacter.GetEffectCodeContainer().AddEffectCode(...);

        await UniTask.Delay(2000); // 총 1초 대기
    }

    // 7단계: 아트레시아 스킬
    private async UniTask TriggerArtesiaSkill()
    {
        ActivateCharacterSkill(_artesiaCharacter);

        await UniTask.Delay(1000); // 1초 대기
    }

    // 8단계: 마녀 최종 공격 준비 이펙트
    private async UniTask TriggerWitchFinalPrepare()
    {
        if (_witchCharacter == null) return;

        // 라플라스 마녀 스킬 발동 준비 이펙트
        // [TODO] 실제 이펙트 시스템 연동 필요
        // 예: _witchCharacter.GetCharacterView().PlayEffect("WitchFinalPrepare");

        await UniTask.Delay(1000); // 1초 대기
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
