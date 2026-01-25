using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;
using CharacterController = CookApps.BattleSystem.CharacterController;

// TODO 클레이 베리어 snd_sfx_ingame_shield ✅
// TODO 아트레시아 궁 snd_sfx_skill_a_3401 ✅
// TODO 유니 궁 snd_sfx_skill_a_2102 ✅
// TODO 필리아 궁 snd_sfx_skill_a_2401_01 snd_sfx_skill_a_2401_01 ✅
// TODO 아트레시아 슈퍼노바 1 + 기모으기 snd_sfx_synergy_nova_spirit ✅
// TODO 마리에 궁  snd_sfx_skill_a_3405_01, snd_sfx_skill_a_3405_02 ✅
// TODO 아트레시아 슈퍼노바 3 snd_sfx_synergy_nova_spirit ✅
// TODO 아트레시아 최종 궁 snd_sfx_skill_a_3401✅ 

namespace CookApps.AutoBattler.Prologue
{
    /// <summary>
    /// 프롤로그 전투 Flow State.
    /// 게임 시작 시 재생되는 스크립트 기반 컷신 전투를 관리한다.
    /// 11단계의 시나리오 시퀀스를 통해 라플라스 마녀와의 전투를 연출한다.
    ///
    /// 시나리오 흐름:
    /// 1. 마녀의 선제공격 준비
    /// 2. 클레이의 방어 (스킬로 마녀 공격 막기)
    /// 3. 클레이 그로기 상태 진입
    /// 4. 아트레시아/유니/필리아 자유 공격 (3초)
    /// 5. 마녀의 광역 역습 (파티원 다운)
    /// 6. 마리에 합류
    /// 7. 마리에 희생 (마녀에게 공격받아 사망)
    /// 8. 아트레시아 초신성 모드 진입
    /// 9. 마녀 체력 회복 및 최종 공격 준비
    /// 10. 마녀 최종 공격 이펙트
    /// 11. 아트레시아 방어 후 전투 종료 (암전)
    /// </summary>
    public class FlowStatePrologueCombat : StateCombatBase
    {
        #region 필드

        /// <summary>캐릭터 리스트 캐싱용 (ListPool 사용)</summary>
        private List<CharacterController> characters;

        /// <summary>전투 종료 여부</summary>
        private bool _isEndCombat;

        /// <summary>승리 여부</summary>
        private bool _isWin;

        /// <summary>스테이지 클리어 여부 (미사용)</summary>
        private bool _isClearStage;

        // ===== 플레이어 캐릭터 =====
        /// <summary>클레이 - 탱커, 2단계에서 방어 스킬 발동</summary>
        private CharacterController _clayCharacter;
        /// <summary>유니 - 딜러, 4단계 자유 공격 참여</summary>
        private CharacterController _yuniCharacter;
        /// <summary>필리아 - 딜러, 4단계 자유 공격 참여</summary>
        private CharacterController _philiaCharacter;
        /// <summary>아트레시아 - 주인공, 8단계 초신성 각성</summary>
        private CharacterController _artesiaCharacter;
        /// <summary>마리에 - 6단계 합류, 7단계 희생</summary>
        private CharacterController _marieCharacter;

        // ===== 적 캐릭터 =====
        /// <summary>라플라스 마녀 - 보스</summary>
        private CharacterController _witchCharacter;

        /// <summary>마녀 공격 준비 이펙트 (1단계 생성, 2단계 제거)</summary>
        private InGameVfx _witchAttackPrepareFx;

        #endregion

        /// <summary>
        /// 전투 상태 초기화.
        /// 시너지 FX 정리, UI 초기화, 캐릭터 HP 갱신, 카메라 세팅.
        /// </summary>
        public override void StateInit(object target)
        {
            characters = ListPool<CharacterController>.Get();

            InGameSynergyManager.Instance.ClearSynergyFx();
            InGameMain.GetInGameMain().SetActiveObjectMover(false);
            InGameMain.GetInGameMain().InitCombatStateUI();
            InGameObjectManager.Instance.SaveStartingPlayerCharacter();
            InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Player);
            InGameObjectManager.Instance.UpdateSumMaxHp(AllianceType.Enemy);
        }

        /// <summary>
        /// 전투 시작.
        /// 전투 시작 이펙트 코드 호출, 캐릭터 참조 획득, 프롤로그 시나리오 시작.
        /// </summary>
        public override void StateStart()
        {
            // 전투 시작 전까지 아이템이 부여되지 않은 아이템들의 콜백 호출
            InGameSynergyManager.Instance.CheckAndHandleNotAppliedItemsBeforeCombat();

            // 플레이어 캐릭터 전투 시작 이펙트 코드 호출
            foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Player))
            {
                character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
                var effectCodes = character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
                    EffectCodeInheritFlag.UseOnCombatStart);
                EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
            }

            // 적 캐릭터 전투 시작 이펙트 코드 호출
            foreach (var character in InGameObjectManager.Instance.GetCharacterList(AllianceType.Enemy))
            {
                character.GetHpBarView().SetHpBarType(HpBarType.HpBar | HpBarType.Buff);
                var effectCodes = character.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(
                    EffectCodeInheritFlag.UseOnCombatStart);
                EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
            }

            // 팀 이펙트 코드 호출
            {
                var effectCodes =
                    InGameManager.Instance.TeamEcc.GetCharacterEffectCodesByFlag(
                        EffectCodeInheritFlag.UseOnCombatStart);
                EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCombatStartLambda);
            }


            // 플레이어 캐릭터 참조 획득 (마리에 제외 - 6단계에서 동적 합류)
            _clayCharacter = FindCharacterById(AllianceType.Player, PrologueID.프롤로그클레이ID);
            _yuniCharacter = FindCharacterById(AllianceType.Player, PrologueID.프롤로그유니ID);
            _philiaCharacter = FindCharacterById(AllianceType.Player, PrologueID.프롤로그필리아ID);
            _artesiaCharacter = FindCharacterById(AllianceType.Player, PrologueID.프롤로그아트레시아ID);

            // 적 캐릭터 참조 획득 (라플라스 마녀)
            InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, characters);
            if (characters.Count > 0)
            {
                _witchCharacter = characters[0];
            }

            // 캐릭터 초기 이동 후 시나리오 시작
            InitializeCharacterPositionsAndStartScenario().Forget();
        }

        /// <summary>
        /// 모든 캐릭터를 Idle 상태로 전환하여 자유 행동을 시작한다.
        /// </summary>
        private async UniTask StartAllCharacters()
        {
            await UniTask.Delay(500);

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

        /// <summary>
        /// 모든 캐릭터를 Ready 상태로 전환하여 행동을 정지시킨다.
        /// 그로기 상태인 캐릭터는 제외.
        /// </summary>
        private async UniTask StopAllCharacters()
        {
            await UniTask.Delay(500);

            if (characters == null)
                return;

            InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Player, characters);
            foreach (CharacterController charac in characters)
            {
                if (charac.GetCurrentState() is CharacterStateGroggy) continue;
                charac.AddNextState<CharacterStateReady>();
                charac.Target = null;
            }

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



            // 초기 카메라 위치 설정 (왼쪽으로 치우침)
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(6, new Vector3(-6, 6.0f, -5), 4).Forget();

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
            (EffectCodeCharacterBase effectcode, bool actRes) activateSkillRes = ActivateCharacterSkill(character, warningMessage, skillID);
            if (activateSkillRes.actRes)
            {
                await UniTask.WaitUntil(() => { return (character.GetCurrentState() is CharacterStateSkill); });
                await UniTask.WaitWhile(() => { return (character.GetCurrentState() is CharacterStateSkill); });
            }
        }



        private (EffectCodeCharacterBase, bool) ActivateCharacterSkill(CharacterController character, string warningMessage = null, int skillID = 0)
        {
            if (character == null) return (null, false);

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
                return (effectCodes[skillID], true);
            }

            if (!string.IsNullOrEmpty(warningMessage))
            {
                Debug.LogWarning(warningMessage);
            }

            return (null, false);
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
            ArtesiaSupernova,               // 8단계: 아트레시아 초신성 모드 진입 + 스킬 발동
            ArtesiaSkill,                   // 9단계: 아트레시아 초신성 모드 진입 + 스킬 발동
            WitchHpRecoverAndFinalPrepare,  // 10단계: 마녀 체력 회복, 최후 스킬 대기 모션
            WitchFinalPrepareFx,             // 11단계: 마녀 최후 공격 준비 이펙트
            WitchFinalAttackAndArtesiaDefend // 12단계: 마녀 최후 공격, 아트레시아 방어, 전투 종료
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
        
        // 7단계: 마리에의 희생과  (DialogueGroupID : 200007)
        new PrologueScenarioData { step = 7, dialogueID = 200007, actionType = PrologueActionType.MarieDown },
        
        // 8단계: 아트레시아 각성 (DialogueGroupID : 200008)
        new PrologueScenarioData { step = 8, dialogueID = 200008, actionType = PrologueActionType.ArtesiaSupernova },
        // 9단계: 아트레시아 슈퍼노바 시킬  (DialogueGroupID : 200009)
        new PrologueScenarioData { step = 9, dialogueID = 200009, actionType = PrologueActionType.ArtesiaSkill },
        // 10단계: 아트레시아의 결의 (DialogueGroupID : 200010)
        new PrologueScenarioData { step = 10, dialogueID = 200010, actionType = PrologueActionType.WitchHpRecoverAndFinalPrepare },
        
        // 11단계: 마녀의 진정한 힘 (DialogueGroupID : 200011)
        new PrologueScenarioData { step = 11, dialogueID = 200011, actionType = PrologueActionType.WitchFinalPrepareFx },
        
        // 12단계: 최후의 순간과 아트레시아의 결의 (DialogueGroupID : 200012)
        new PrologueScenarioData { step = 12, dialogueID = 200012, actionType = PrologueActionType.WitchFinalAttackAndArtesiaDefend }
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
                case PrologueActionType.ArtesiaSupernova:
                    await TriggerArtesiaSupernova();
                    break;
                case PrologueActionType.ArtesiaSkill:
                    await TriggerArtesiaSkill();
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


        private InGameVfx laplasSkill1;
        private InGameVfx9002 inGameVfx9002;
        // 1단계: 마녀 공격 준비 이펙트 Loop

        private async UniTask TriggerWitchAttackPrepare()
        {
            if (_witchCharacter == null) return;
            _witchCharacter.GetCharacterView().PlayAnimation(AnimationKey.SKLPRE);
            await UniTask.Delay(750);
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_prolog_battle_laplace01);
            await UniTask.Delay(250);
            _witchCharacter.GetCharacterView().PlayAnimation(AnimationKey.SKLLOOP);

            var vfxTileIdx = _artesiaCharacter.CurrentTile.Int2Index + new int2(0, 1);
            laplasSkill1 = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.Skill_9002_1,
                InGameObjectManager.Instance.InGameGrid.GetTile(vfxTileIdx).View.CachedTr.position);
            inGameVfx9002 = laplasSkill1.GetComponent<InGameVfx9002>();
            await UniTask.Delay(1500); // 1.5초 대기
        }

        List<InGameVfx> clayShieldList;
        // 2단계: 클레이 중앙 포지션 이동, 스킬 발동, 마녀 스킬 막기
        private async UniTask TriggerClaySkillAndWitchAttack()
        {
            if (_clayCharacter == null) return;

            {
                List<UniTask> charaSwitching = new();
                charaSwitching.Add(UniTask.Create(async () =>
                {
                    MoveCharacterToDirection(_clayCharacter, 0, 2);
                    {
                        await UniTask.WaitUntil(() => { return (_clayCharacter.GetCurrentState() is CharacterStateForceMove) || (_clayCharacter.GetCurrentState() is CharacterStateMove); });
                        await UniTask.WaitWhile(() => { return (_clayCharacter.GetCurrentState() is CharacterStateForceMove) || (_clayCharacter.GetCurrentState() is CharacterStateMove); });
                    }
                }));
                charaSwitching.Add(UniTask.Create(async () =>
                {

                    MoveCharacterToDirection(_artesiaCharacter, 0, -1);
                    {
                        await UniTask.WaitUntil(() => { return (_artesiaCharacter.GetCurrentState() is CharacterStateForceMove) || (_artesiaCharacter.GetCurrentState() is CharacterStateMove); });
                        await UniTask.WaitWhile(() => { return (_artesiaCharacter.GetCurrentState() is CharacterStateForceMove) || (_artesiaCharacter.GetCurrentState() is CharacterStateMove); });
                    }

                }));
                await UniTask.WhenAll(charaSwitching);
            }
            // 클레이 중앙으로 포지션 이동
            await UniTask.NextFrame();
            _artesiaCharacter.GetCharacterView().LookAt(_artesiaCharacter.CurrentTile, _witchCharacter.CurrentTile);
            await UniTask.NextFrame();
            _artesiaCharacter.AddNextState<CharacterStateReady>();
            await UniTask.NextFrame();

            _clayCharacter.Target = _clayCharacter;

            clayShieldList = new();

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_shield);
            foreach (var ally in InGameObjectManager.Instance.GetCharacterList(_clayCharacter.AllianceType))
            {
                clayShieldList.Add(InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_prologue_shield_01, ally.SkillMiddleFXTransformFollowable));
            }
        }

        // 3단계: 클레이 그로기 모션
        private async UniTask TriggerClayGroggyObsolete()
        {
            // 라플라스 마녀 공격 준비 이펙트 종료
            if (_witchAttackPrepareFx != null)
            {
                InGameVfxManager.Instance.RemoveInGameVfx(_witchAttackPrepareFx);
                _witchAttackPrepareFx = null;
            }
            _witchCharacter.GetCharacterView().PlayAnimation(AnimationKey.SKLEND);
            inGameVfx9002.SetPlay();
            await UniTask.WaitUntil(inGameVfx9002.IsFinished);
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).ShakeCamera(0.5f, 0.1f);
            foreach (var vfx in clayShieldList)
            {
                vfx.Remove();
            }
            {
                List<UniTask> knockbacks = new();
                knockbacks.Add(YuniCharacterKnockback());
                knockbacks.Add(PhilliaCharacterKnockback());
                knockbacks.Add(ClaycharacterKnockback());
                await UniTask.WhenAll(knockbacks);
            }

            await UniTask.NextFrame();
            _yuniCharacter.GetCharacterView().LookAt(_yuniCharacter.CurrentTile, _witchCharacter.CurrentTile);
            _philiaCharacter.GetCharacterView().LookAt(_philiaCharacter.CurrentTile, _witchCharacter.CurrentTile);
            _clayCharacter.GetCharacterView().LookAt(_clayCharacter.CurrentTile, _witchCharacter.CurrentTile);
            _yuniCharacter.AddNextState<CharacterStateReady>();
            _philiaCharacter.AddNextState<CharacterStateReady>();
            _clayCharacter.AddNextState<CharacterStateReady>();
            await UniTask.NextFrame();

            await UniTask.Delay(150);
            _clayCharacter.AddNextState<CharacterStateGroggy>();

            _witchCharacter.AddNextState<CharacterStateReady>();
            await UniTask.Delay(1500); // 1.5초 대기

        }


        // 4단계: 아트레시아+유니+필리아 자유 공격 3초, 마녀 지친 모션
        private async UniTask TriggerFreeAttack3Seconds()
        {
            await ManualFreeMoveActions();
            _artesiaCharacter.Target = _witchCharacter;
            ActivateCharacterSkill(_artesiaCharacter);
            _yuniCharacter.Target = _artesiaCharacter;
            ActivateCharacterSkill(_yuniCharacter);

            await UniTask.Delay(1000); // 1초 대기

            if (_artesiaCharacter != null && _artesiaCharacter.IsAlive)
            {
                _artesiaCharacter.AddNextState<CharacterStateIdle>();
                _artesiaCharacter.Target = _witchCharacter;
            }
            if (_yuniCharacter != null && _yuniCharacter.IsAlive)
            {
                _yuniCharacter.AddNextState<CharacterStateIdle>();
                _yuniCharacter.Target = _artesiaCharacter;
            }

            var cts = new CancellationTokenSource();
            UniTask.Create(async cst =>
            {
                while (!cts.IsCancellationRequested)
                {
                    _philiaCharacter.Target = _witchCharacter;
                    await ActivateCharacterSkillWithWait(_philiaCharacter);
                    _yuniCharacter.AddNextState<CharacterStateReady>();
                }
            }, cts.Token).Forget();
            // 라플라스 마녀도 평타 공격 (공격 속도 2.0배)
            if (_witchCharacter != null && _witchCharacter.IsAlive)
            {
                _witchCharacter.AddNextState<CharacterStateAttack>(15f);
                _witchCharacter.Target = _artesiaCharacter;
            }


            await UniTask.Delay(2000); // 3초 자유 전투
            {
                List<UniTask> uniTasks = new();
                uniTasks.Add(ActivateCharacterSkillWithWait(_artesiaCharacter));
                uniTasks.Add(ActivateCharacterSkillWithWait(_yuniCharacter));

                await UniTask.WhenAll(uniTasks);
            }
            cts.Cancel();

            await StopAllCharacters();

            if (_witchCharacter != null && _witchCharacter.IsAlive)
            {
                _witchCharacter.AddNextState<CharacterStateGroggy>();
            }
        }

        // async Task<UniTask> PhilliaAttackTimeing()
        // {
        //     for(int i = 0; i < 2; i++)
        //     {
        //         await UniTask.WaitUntil(() => {return (_philiaCharacter.GetCurrentState() is CharacterStateAttack);} );
        //         await UniTask.
        //     }
        // }

        // 5단계: 마녀 광역 스킬, 유니/필리아/클레이 다운
        private async UniTask TriggerWitchAoEAndCharactersDown()
        {
            if (_witchCharacter == null) return;

            // 마녀 지친 모션 -> 아이들로 변경
            _witchCharacter.AddNextState<CharacterStateIdle>();

            await UniTask.Delay(500); // 0.5초 대기

            // 라플라스 마녀 광역 스킬 발동
            _witchCharacter.Target = _clayCharacter;
            UniTask.Create(async () =>
            {
                await UniTask.Delay(3000);
                ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).ShakeCamera(1.0f, 0.5f);
            }).Forget();
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_prolog_battle_laplace03);
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
                int2 spawnPosition = _witchCharacter.CurrentTile.Int2Index + new int2(-2, 0);
                InGameTile spawnTile = InGameObjectManager.Instance.InGameGrid.GetTile(spawnPosition);

                // 위치가 점유되어 있으면 주변 빈 타일 찾기
                if (spawnTile == null || spawnTile.OccupiedCharacter != null)
                {
                    spawnTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile(AllianceType.Player);
                }

                if (spawnTile != null)
                {
                    await UniTask.NextFrame();
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
                    _marieCharacter.GetCharacterView().LookAt(spawnTile, _witchCharacter.CurrentTile);
                    _marieCharacter.AddNextState<CharacterStateReady>();
                    PrologueUtility.FindChildRecursive(_marieCharacter.GetCharacterView().CachedTr, "Synergy").gameObject.SetActive(false);

                    Debug.LogColor($"마리에 합류: {marieCharacterId} at ({spawnTile.X}, {spawnTile.Y})");
                    await UniTask.NextFrame();
                    if (_marieCharacter != null && _marieCharacter.IsAlive)
                    {
                        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_skill_a_3405_01);
                        _marieCharacter.GetCharacterView().PlayAnimation(AnimationKey.SKL);
                        // 거의 동시에 마리에 스킬 이펙트 (마리에 디버프 스킬 발동)
                        UniTask.Create(async () =>
                        {
                            await UniTask.Delay(500);
                            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.Skill_17563405, _witchCharacter.SkillBottomFXTransformFollowable);
                            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_skill_a_3405_02);
                            for (int i = 1; i <= 3; i++)
                            {
                                await UniTask.Delay(100);
                                CharacterController.DamageInfo damageInfo = CharacterController.DamageInfo.Create(
                                    damageAmount: (51 * i) + i * i,
                                    source: 0,
                                    attackerType: AttackerType.CHARCTER,
                                    isAD: _marieCharacter.SpecCharacter.atk_type is AtkType.AD ? true : false,
                                    isCritical: false,
                                    isDoubleCritical: false
                                );
                                _witchCharacter.GetDamaged(damageInfo, _marieCharacter);
                            }
                        }).Forget();
                        await ActivateCharacterSkillWithWait(_marieCharacter, "마리에 스킬을 찾을 수 없습니다.");
                        _marieCharacter.Target = _witchCharacter;
                    }
                    _marieCharacter.AddNextState<CharacterStateReady>();
                }
            }
        }

        InGameVfx artesiaChargeVFX;
        InGameVfxArtesiaCharge artesiaChargeVFX2;

        // 7단계: 마리에 다운 (아트레시아 기모은 후 따라감, 마리에 먼저 공격)
        private async UniTask TriggerMarieDown()
        {
            // 아트레시아 0.5초 대기 후 마녀 공격
            if (_artesiaCharacter != null && _artesiaCharacter.IsAlive)
            {
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_synergy_nova_spirit);
                artesiaChargeVFX = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_sn_aura_01, _artesiaCharacter.SkillRootTransformFollowable);
                artesiaChargeVFX2 = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_prologue_artesia_charge, _artesiaCharacter.SkillRootTransformFollowable) as InGameVfxArtesiaCharge;
                Debug.Log($"artesiaChargeVFX2 is null {artesiaChargeVFX2 == null}");
                await UniTask.Delay(600);
                _artesiaCharacter.AddNextState<CharacterStateIdle>();
                _artesiaCharacter.Target = _witchCharacter;
            }
            _marieCharacter.AddNextState<CharacterStateIdle>();
            _marieCharacter.Target = _witchCharacter;

            // 마녀 → 마리에 공격
            if (_witchCharacter != null && _witchCharacter.IsAlive)
            {
                _witchCharacter.AddNextState<CharacterStateIdle>();
                _witchCharacter.Target = _marieCharacter;
            }

            await UniTask.Delay(2200); // 1초 대기
                                       // 마리에 → Dead 상태
            if (_marieCharacter != null && _marieCharacter.IsAlive)
            {
                _marieCharacter.AddNextState<CharacterStateDead>();
            }

            await StopAllCharacters();
            await UniTask.NextFrame();
            _witchCharacter.GetCharacterView().LookAt(_witchCharacter.CurrentTile, _artesiaCharacter.CurrentTile);
            _witchCharacter.AddNextState<CharacterStateGroggy>();
            await UniTask.NextFrame();

            await UniTask.Delay(1000); // 1초 대기
        }

        // 8단계: 아트레시아 초신성 모드 진입 + 스킬 발동
        private async UniTask TriggerArtesiaSupernova()
        {
            if (_artesiaCharacter != null)
            {
                artesiaChargeVFX.Remove();
                artesiaChargeVFX2.TriggerExplosion();
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_synergy_nova_spirit);
                artesiaChargeVFX = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_sn_aura_03, _artesiaCharacter.SkillRootTransformFollowable);
            }
            await UniTask.Delay(1000); // 1초 대기
            artesiaChargeVFX2.Clear();
            artesiaChargeVFX2.Remove();
        }


        // 9 단계 : 추후 하나의 다이얼로그 더 추가해서 해치웠을까요 물어보자.
        private async UniTask TriggerArtesiaSkill()
        {

            _artesiaCharacter.Target = _witchCharacter;
            await ActivateCharacterSkillWithWait(_artesiaCharacter, "아트레시아 스킬을 찾을 수 없습니다.", 1);
            _artesiaCharacter.AddNextState<CharacterStateReady>();
            await UniTask.Delay(500); // 1.5초 대기
            artesiaChargeVFX.Remove();
            await UniTask.Delay(500); // 1.5초 대기
        }

        private InGameVfx _witchFinalPrepareFx; // 마녀 최후의 공격 준비 이펙트

        // 10 단계: 마녀 체력 회복, 최후 스킬 대기 모션
        private async UniTask TriggerWitchHpRecoverAndFinalPrepare()
        {
            // 아트레시아 스킬 발동
            if (_witchCharacter == null) return;

            Debug.LogColor("라플라스 마녀 체력 회복");
            var vfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.Skill_9002_4, _witchCharacter.SkillBottomFXTransformFollowable);
            vfx.CachedTr.localPosition += new UnityEngine.Vector3(0, 0, -4);
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_prolog_battle_laplace04);
            await UniTask.Delay(1000);
            _witchCharacter.AddNextState<CharacterStateReady>();
            _witchCharacter.GetHealed(9999.0f, _witchCharacter, 0);
            await UniTask.Delay(1000);

            // 라플라스 마녀 최후 스킬 대기 모션
            // [TODO] 대기 모션 재생
            Debug.LogColor("라플라스 마녀 최후 스킬 대기 모션");

            await UniTask.Delay(1500); // 1.5초 대기
        }

        // 11단계: 마녀 최후 공격 준비 이펙트
        private async UniTask TriggerWitchFinalPrepareFx()
        {
            if (_witchCharacter == null) return;
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).ShakeCamera(600, 0.1f);
            // 라플라스 마녀 최후의 공격 준비 이펙트 재생
            // 암흑 힘 응집 이펙트 (마녀를 감싸는 이펙트)
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_prolog_battle_laplace05);
            _witchAttackPrepareFx = InGameVfxManager.Instance.AddInGameVfx(
                InGameVfxNameType.Skill_9002_3,
                _witchCharacter.SkillMiddleFXTransformFollowable);
            _witchCharacter.GetCharacterView().PlayAnimation(AnimationKey.SKL2PRE);


            // [TODO] 필요시 더 강력한 이펙트로 변경
            // 예: _witchCharacter.GetCharacterView().PlayEffect("WitchFinalPrepare");

            await UniTask.Delay(2000); // 1초 대기
            _witchCharacter.GetCharacterView().PlayAnimation(AnimationKey.SKL2LOOP);

        }

        // 12단계: 마녀 최후 공격, 아트레시아 방어, 전투 종료
        private async UniTask TriggerWitchFinalAttackAndArtesiaDefend()
        {
            // ! 암전
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_prolog_battle_endtransition);
            var whiteOutImage = GameObject.Find("WhiteOutUICover").GetComponent<UnityEngine.UI.Image>();
            SoundManager.Instance.StopBGM(1.2f);
            var fadeTween = whiteOutImage.DOFade(1f, 1.5f);
            fadeTween.onComplete += ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).StopShakingCamera;

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
            List<UniTask> moveTasks = new();
            if (_artesiaCharacter != null && _artesiaCharacter.IsAlive)
                moveTasks.Add(ArtesiaMovement());
            if (_yuniCharacter != null && _yuniCharacter.IsAlive)
                moveTasks.Add(YuniMovement());
            if (_philiaCharacter != null && _philiaCharacter.IsAlive)
                moveTasks.Add(PhilliaMovement());
            await UniTask.WhenAll(moveTasks);
        }

        private async UniTask YuniCharacterKnockback()
        {
            KnockbackHelper.ApplyKnockback(_witchCharacter, _yuniCharacter, tileIdx: new int2(-1, -1), duration: 0.4f);

            // await UniTask.WaitUntil(() => { return (_yuniCharacter.GetCurrentState() is CharacterStateForceMove) || (_yuniCharacter.GetCurrentState() is CharacterStateMove); });
            await UniTask.Delay(400);
            await UniTask.WaitUntil(() => { return (_yuniCharacter.GetCurrentState() is CharacterStateIdle); });
            _yuniCharacter.AddNextState<CharacterStateReady>();
        }

        private async UniTask PhilliaCharacterKnockback()
        {

            KnockbackHelper.ApplyKnockback(_witchCharacter, _philiaCharacter, tileIdx: new int2(1, -1), duration: 0.4f);

            // await UniTask.WaitUntil(() => { return (_philiaCharacter.GetCurrentState() is CharacterStateForceMove) || (_philiaCharacter.GetCurrentState() is CharacterStateMove); });
            await UniTask.Delay(400);
            await UniTask.WaitUntil(() => { return (_philiaCharacter.GetCurrentState() is CharacterStateIdle); });
            _philiaCharacter.AddNextState<CharacterStateReady>();
        }

        private async UniTask ClaycharacterKnockback()
        {
            KnockbackHelper.ApplyKnockback(_witchCharacter, _clayCharacter, tileIdx: new int2(0, -1), duration: 0.4f);

            // await UniTask.WaitUntil(() => { return (_clayCharacter.GetCurrentState() is CharacterStateForceMove) || (_clayCharacter.GetCurrentState() is CharacterStateMove); });
            await UniTask.Delay(400);
            await UniTask.WaitUntil(() => { return (_clayCharacter.GetCurrentState() is CharacterStateIdle); });
            _clayCharacter.AddNextState<CharacterStateReady>();
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

    public static class KnockbackHelper
    {
        /// <summary>
        /// 캐릭터를 넉백시킵니다.
        /// </summary>
        /// <param name="attacker">공격자 (방향 계산용)</param>
        /// <param name="target">넉백 대상</param>
        /// <param name="tileCount">밀려나는 칸 수</param>
        /// <param name="duration">넉백 지속 시간 (기본 0.3초)</param>
        /// <param name="height">공중 높이 (기본 2.5f)</param>
        /// <param name="ease">이동 이징 (기본 OutExpo)</param>
        /// <param name="onComplete">완료 콜백 (선택)</param>
        /// <returns>생성된 넉백 이펙트 코드 (실패시 null)</returns>
        public static EffectCodeCrowdControlKnockback ApplyKnockback(
            CharacterController attacker,
            CharacterController target,
            int2 tileIdx,
            float duration = 0.3f,
            float height = 0,
            Ease ease = Ease.OutExpo,
            Action<InGameTile> onComplete = null)
        {
            if (target == null || !target.IsAlive || target.CurrentTile == null)
                return null;

            var grid = InGameObjectManager.Instance.InGameGrid;
            var attackerTile = attacker?.CurrentTile ?? target.CurrentTile;
            var knockBackTile = grid.GetTile(target.CurrentTile.Int2Index + tileIdx);

            Span<double> eccStats = stackalloc double[4];
            eccStats[0] = duration;
            eccStats[1] = height;
            eccStats[2] = knockBackTile.View.ID;
            eccStats[3] = (int)ease;

            var effectCode = EffectCodeHelper.AddOrMergeEffectCode(
                EffectCodeNameType.CC_KNOCKBACK,
                target,
                eccStats,
                attacker as IEffectCodeSource
            );

            if (effectCode is EffectCodeCrowdControlKnockback knockback && onComplete != null)
            {
                knockback.SetOnKnockbackEndHandler(onComplete);
            }

            return effectCode as EffectCodeCrowdControlKnockback;
        }
    }
}