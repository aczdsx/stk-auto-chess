using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.BattleSystem
{
    public partial class CharacterController : IEffectCodeSource
    {
        public int CharacterUId => _characterUId;
        public int CharacterId => _statData?.CharacterId ?? _characterID;
        public ISpecCharacterInfo SpecCharacter => _statData.Spec;

        public static readonly int CharacterActiveSkillStatCnt = 8;
        private Dictionary<Type, Type> _stateTypeMap = new Dictionary<Type, Type>();

        private EffectCodeContainer ecc;
        /// <summary>
        /// 타일 이동 종료 시 호출되는 함수입니다.
        /// 현재 CharacterStateMove의 StateEnd
        /// CharacterController의 MoveTile의 새로운 타일로 CurrentTile을 변경하기 전에 호출합니다.
        /// </summary>
        public void OnTileMoveEnd()
        {
            var tileEcc = CurrentTile.EffectCodeContainer;
            if (tileEcc.GetEffectCode((int)EffectCodeNameType.CHAPTER_TRAP)
                                                is EffectCodeGameBase effectCodeGameBase)
            {
                effectCodeGameBase.OnTileMoveEnd(CurrentTile, this);
            }

            if (tileEcc.GetEffectCode((int)EffectCodeNameType.BATTLE_ITEM_DYNAMITE)
            is EffectCodeGameBase dynamiteEffectCode)
            {
                dynamiteEffectCode.OnTileMoveEnd(CurrentTile, this);
            }

            if (tileEcc.GetEffectCode((int)EffectCodeNameType.CHAPTER_LANDMINE)
            is EffectCodeGameBase landmineEffectCode)
            {
                landmineEffectCode.OnTileMoveEnd(CurrentTile, this);
            }
        }

        public EffectCodeContainer GetEffectCodeContainer()
        {
            return ecc;
        }

        private CharacterStatData _statData;
        public CharacterStatData GetCharacterStat()

        {
            return _statData;
        }

        private AsyncOperationHandle<GameObject> _viewHandle;
        private SpriteCharacterView _view = null;

        public SpriteCharacterView GetCharacterView()
        {
            return _view;
        }

        public HpBarView GetHpBarView()
        {
            return _hpBarView;
        }

        // public InGameTileView GetCharacterDirectionTileView()
        // {
        //     return InGameObjectManager.Instance.InGameGrid.GetDirectionalTile(view);
        // }

        public event Action<CharacterStateBase> OnStateChanged;

        public CharacterController Target { get; set; }
        public InGameTile CurrentTile { get; set; }
        public bool IsAlive { get; set; }

        /// <summary>
        /// 스킬 루트 트랜스폼
        /// view의 scale이 커져도 위치가 변하지 않는 트랜스폼
        /// </summary>
        public IFollowable SkillRootTransformFollowable => new SimpleSkillTransformFollowable(this);

        /// <summary>
        /// 스킬 탑 미들 바텀 FX 트랜스폼
        /// view의 scale이 커지면 위치가 변하는 트랜스폼
        /// </summary>
        public IFollowable SkillTopFXTransformFollowable => new SimpleSkillTopFXTransformFollowable(this);
        public IFollowable SkillMiddleFXTransformFollowable => new SimpleSkillMiddleFXTransformFollowable(this);
        public IFollowable SkillBottomFXTransformFollowable => new SimpleSkillBottomFXTransformFollowable(this);

        /// <summary>
        /// 논리적 위치
        /// </summary>
        private Vector3 position;
        public Vector2 Position
        {
            get => position;
            set => position = value;
        }

        public Vector3 Position3D
        {
            get => position;
            set => position = value;
        }

        /// <summary>
        /// view의 위치, 에어본이나 점프 같은 경우 뷰의 위치와 논리적 위치가 다를 수 있다.
        /// </summary>
        private Vector3 viewPosition;
        public Vector2 ViewPosition
        {
            get => viewPosition;
            set => viewPosition = value;
        }

        public Vector3 ViewPosition3D
        {
            get => viewPosition;
            set => viewPosition = value;
        }

        public bool FlipX { get; set; }

        public float GetAttackCoolTime()
        {
            return _atkCoolTime;
        }

        public void ResetAttackCoolTime()
        {
            _atkCoolTime = 1f;
        }

        public AllianceType AllianceType => _allianceType;
        public double CurrentHp => _currHp;

        private HpBarView _hpBarView;

        private AllianceType _allianceType;

        private CrowdControlType _crowdControlType = CrowdControlType.None;
        private double _currHp;

        private double _currShield
        {
            get
            {
                double shieldAmount = 0d;
                var effectCodeShield =
                    GetEffectCodeContainer().GetEffectCode(EffectCodeBuffShield.CodeId) as EffectCodeBuffShield;

                if (effectCodeShield == null)
                    return 0d;

                var shields = effectCodeShield.Shields;

                foreach (var shield in shields)
                {
                    if (shield == null) continue;
                    shieldAmount += shield.shieldAmount;
                }

                return shieldAmount;
            }
        }

        private CharacterStateBase _currState;
        private CharacterStateBase _nextState;

        private ObfuscatorFloat _atkCoolTime;

        private List<(int codeID, BuffStackData buffStackData)> _buffDebuffs;
        private Dictionary<BuffDebuffType, ObfuscatorInt> _buffDebuffRefCountDict;
        private Dictionary<BuffDebuffType, InGameVfx> _buffDebuffEffectViewDict;

        private static int characUIdInc;
        private int _characterID;
        private int _characterUId;

        private Vector3 SelectedOffSet;
        private InGameVfx _notFoundTargetFx;


        //캐릭터가 가지고있는 스탯 확률을 건드리지않고, 반드시 크리티컬 확률을 증가시키고 싶을 때 사용하는 변수
        private float _fixedCriticalProb = 0f;

        public async UniTask Initialize(InGameTile tile, Transform Playground, int id, AllianceType allianceType)
        {
            _characterID = id;
            _characterUId = characUIdInc++;
            ChangeOccupiedTile(tile);
            _allianceType = allianceType;
            position = tile.View.Position;
            // GameObject viewGo = await Addressables.InstantiateAsync(
            //     $"Obstacle/Stage/{id}/GenerateResources/CharacterView_{id}.prefab");
            //  var spec = SpecDataManager.Instance.GetSpecCharacter(id);

            if (_viewHandle.IsValid())
                Addressables.ReleaseInstance(_viewHandle);
            var handle = _viewHandle = Addressables.InstantiateAsync(SpecDataExtensions.ToObstacleResourcePath(id));
            await handle;

            if (!handle.IsValid())
                return;

            _view = handle.Result.GetComponent<SpriteCharacterView>();
            _view.CachedTr.SetParent(Playground, false);
            _view.CachedTr.position = position;
        }

        public async UniTask Initialize(CharacterStatData statData, InGameTile tile, AllianceType allianceType,
        bool hasSkill, HpBarType type = HpBarType.None)
        {
            _characterUId = characUIdInc++;
            _statData = statData;
            position = tile.View.Position;

            ChangeOccupiedTile(tile);


            if (statData.Spec.size >= 1)
            {
                var tiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(tile, statData.Spec.size);
                tiles.Remove(tile);
                foreach (var occupiedTile in tiles)
                {
                    occupiedTile.SetOccupied(this);
                }
            }

            _allianceType = allianceType;

            if (_viewHandle.IsValid())
                Addressables.ReleaseInstance(_viewHandle);

            var characterResourcePath = statData.Spec.ToCharacterResourcePath();
            var handle = _viewHandle = Addressables.InstantiateAsync(characterResourcePath);
            await handle;

            if (!handle.IsValid())
                return;

            _view = handle.Result.GetComponent<SpriteCharacterView>();
            if (_statData.Spec != null)
            {
                IsAlive = true;
                _hpBarView = InGameHpBarViewPool.Instance.Get();
                _hpBarView.Initialize(statData, allianceType);
                _hpBarView.SetHpBarType(type);
                _view.SetHpBarView(_hpBarView, _statData.Spec.height);
                _view.SetFirstDirection(allianceType);
                if (_statData.Spec.prefab_id == 30101001 || _statData.Spec.prefab_id == 30101003 || _statData.Spec.prefab_id == 30101005/* ||
                    _statData.Spec.prefab_id == 20201*/)
                    _view.SetHologramShader();

                _view.OnAnimationEvent += OnAnimationEvent;
                _view.CachedTr.localPosition = position;
                _buffDebuffs = new();
                _buffDebuffRefCountDict = new Dictionary<BuffDebuffType, ObfuscatorInt>();
                _buffDebuffEffectViewDict = new Dictionary<BuffDebuffType, InGameVfx>();

                // add EffectCodes
                ecc = new EffectCodeContainer(this);
                needUpdateFlag = EffectCodeInheritFlagExtensions.AllFlags();
                ecc.OnChangedDirtyFlag += EffectCodeOnChangedDirtyFlagHandler;

                if (hasSkill)
                    AddSkillEffectCodes();

                _currHp = HP;
                AddPassive();
            }
        }

        /// <summary>
        /// 고스트 캐릭터 초기화 (드래그 프리뷰용 - 타일 점유 안 함, 시스템 미등록)
        /// </summary>
        public async UniTask InitializeAsGhost(CharacterStatData statData, InGameTile tile, AllianceType allianceType)
        {
            _characterUId = characUIdInc++;
            _statData = statData;
            position = tile.View.Position;
            // CurrentTile은 null로 유지 (고스트는 타일을 점유하지 않음)

            _allianceType = allianceType;

            if (_viewHandle.IsValid())
                Addressables.ReleaseInstance(_viewHandle);

            var characterResourcePath = statData.Spec.ToCharacterResourcePath();
            var handle = _viewHandle = Addressables.InstantiateAsync(characterResourcePath);
            await handle;

            if (!handle.IsValid())
                return;

            _view = handle.Result.GetComponent<SpriteCharacterView>();
            if (_statData.Spec != null)
            {
                // HP바 생성하지 않음 (고스트)
                _view.SetFirstDirection(AllianceType.Player);
                _view.CachedTr.localPosition = position;

                // EffectCode, 스킬, 패시브 등록 안 함 (고스트)
                IsAlive = true;
            }
        }

        private void AddPassive()
        {
            // job skill 추가
            if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStateLobbyCombat)
                return;
            var specDataManagerInstance = SpecDataManager.Instance;
            // TODO: 레벨 추가 시 grade를 변경하여 전달
            int testGrade = 0;

            var passiveList = specDataManagerInstance.GetJobPassiveList(SpecCharacter.character_position_type);// 오라클, 이런거
            if (passiveList == null || passiveList.Count == 0)
                return;

            Span<double> stats = stackalloc double[5];
            foreach (var passive in passiveList)
            {
                var passiveData = passive[testGrade];
                var effectCodeID = (long)passiveData.passive_skill_type;
                stats[0] = (double)passiveData.skill_value_type;
                stats[1] = passiveData.passive_rate;
                stats[2] = passiveData.lv;
                stats[3] = (double)passiveData.skill_value_type_2;
                stats[4] = passiveData.passive_rate_2;
                var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, stats);
                ecc.AddOrMergeEffectCode(effectCodeInfo, this);
            }

            // // skill passive 추가
            Span<double> statsSkillPasive = stackalloc double[8];

            var skillDataList = SpecDataManager.Instance.GetSkillPassiveDataList(_statData.Spec.passive_skill_id);
            if (skillDataList != null && skillDataList.Count > 0)
            {
                statsSkillPasive.Clear();
                for (int i = 0; i < skillDataList.Count; i++)
                {
                    statsSkillPasive[i] = skillDataList[i].base_rate;
                }

                var effectCodeInfo = new EffectCodeInfo(skillDataList[0].passive_group_id, 0, statsSkillPasive);
                ecc.AddOrMergeEffectCode(effectCodeInfo, this);
            }
        }


        public void AddViewScaleFactor(float viewScaleValue)
        {
            _view.AddViewScale(viewScaleValue);
        }
        public void RemoveViewScaleFactor(float viewScaleValue)
        {
            _view.RemoveViewScale(viewScaleValue);
        }

        public void SetStateType(Type baseStateType, Type concreteStateType)
        {
            if (baseStateType == null || concreteStateType == null)
                return;

            // concreteStateType이 baseStateType을 상속받는지 확인
            if (!concreteStateType.IsSubclassOf(baseStateType) && concreteStateType != baseStateType)
                return;

            _stateTypeMap[baseStateType] = concreteStateType;
        }
        public void RemoveStateType(Type baseStateType)
        {
            if (_stateTypeMap.TryGetValue(baseStateType, out Type concreteStateType))
            {
                _stateTypeMap.Remove(baseStateType);
            }
        }
        public Type FindStateType(Type baseStateType)
        {
            if (_stateTypeMap.TryGetValue(baseStateType, out Type concreteStateType))
            {
                return concreteStateType;
            }
            return baseStateType;
        }
        public CharacterController FindTarget()
        {
            var idleStateType = FindStateType(typeof(CharacterStateIdle));
            if (idleStateType == typeof(CharacterStateIdleHealer))
            {
                return CharacterStateIdleHealer.FindTarget(this);
            }
            else
            {
                return CharacterStateIdle.FindTarget(this);
            }
        }

        public void Clear()
        {
            if (_statData != null)
            {
                _hpBarView?.OnPreReturn();
                InGameHpBarViewPool.Instance.Return(_hpBarView);
                ecc.Clear();
                ecc.OnChangedDirtyFlag -= EffectCodeOnChangedDirtyFlagHandler;
                foreach (var pair in _buffDebuffEffectViewDict)
                {
                    InGameVfxManager.Instance.RemoveInGameVfx(pair.Value);
                }

                _buffDebuffEffectViewDict.Clear();
                _view.OnAnimationEvent -= OnAnimationEvent;
                _view.StopAllTweens();
            }
            if (_viewHandle.IsValid())
            {
                Addressables.ReleaseInstance(_viewHandle);
            }
            else
            {
                int test = 0;
            }
            ClearAllState();
            Target = null;
            _view = null;
            _hpBarView = null;
        }

        /// <summary>
        /// 고스트 캐릭터 제거 (ecc, HP바 등 시스템 미등록 상태)
        /// </summary>
        public void ClearGhost()
        {
            if (_viewHandle.IsValid())
            {
                Addressables.ReleaseInstance(_viewHandle);
            }
            ClearAllState();
            Target = null;
            _view = null;
        }

        private void AddSkillEffectCodes()
        {
            Span<double> stats = stackalloc double[CharacterActiveSkillStatCnt];
            foreach (var skillID in _statData.Spec.skill_ids)
            {
                var skillDataList = SpecDataManager.Instance.GetSkillDataList(skillID);
                if (skillDataList != null && skillDataList.Count > 0)
                {
                    stats.Clear();
                    stats[0] = skillDataList[0].base_rate; // cool time
                    for (int i = 1; i < skillDataList.Count; i++)
                    {
                        stats[i] = skillDataList[i].base_rate;
                    }

                    var effectCodeInfo = new EffectCodeInfo(skillDataList[0].skill_group_id, 0, stats);
                    ecc.AddOrMergeEffectCode(effectCodeInfo, this);
                }
            }
        }

        public void InjectSynergy(long effectCodeID, ISpecSynergyData synergyData)
        {
            Span<double> stats = stackalloc double[4];
            stats[0] = synergyData.effect_stat_value_1;
            stats[1] = synergyData.effect_stat_value_2;
            stats[2] = synergyData.grade;
            stats[3] = synergyData.effect_stat_value_3;
            var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, stats);
            ecc.AddOrMergeEffectCode(effectCodeInfo, null);
        }

        public void AddSynergyApplyEach(SynergyType targetSynergyType, long effectCodeID, ISpecSynergyData synergyData)
        {
            if (DistinguishSpecTypeHelper.IsAsterismSynergyType(targetSynergyType))
            {
                if (targetSynergyType != _statData.Spec.character_stella_type)
                    return;
            }
            else if (DistinguishSpecTypeHelper.IsElementSynergyType(targetSynergyType))
            {
                if (targetSynergyType != _statData.Spec.character_element_type)
                    return;
            }
            InjectSynergy(effectCodeID, synergyData);
        }

        public void RemoveSynergyEffectCodeALL()
        {
            for (int i = 1; i < Enum.GetValues(typeof(SynergyType)).Length; i++)
            {
                SynergyType targetSynergyType = (SynergyType)i;

                var inGameObjectManagerInstance = InGameObjectManager.Instance;

                var targetSynergyCharacterCount = inGameObjectManagerInstance.GetCharacterSynergyCount(_allianceType, targetSynergyType);

                if (targetSynergyCharacterCount < 1)
                    continue;

                if (!SpecDataManager.Instance.TryGetSynergyDataByCount(targetSynergyType, targetSynergyCharacterCount,
                out var outSynergyData, out var outSynergyList))
                    continue;

                ecc.RemoveEffectCode(outSynergyList[0].synergy_group_id);
            }
        }

        public void RemoveSynergyEffectCode(SynergyType targetSynergyType)
        {
            if (SpecCharacter.character_element_type != targetSynergyType && SpecCharacter.character_stella_type != targetSynergyType)
                return;

            var synergyList = SpecDataManager.Instance.GetSpecSynergyList(targetSynergyType);
            ecc.RemoveEffectCode(synergyList[0].synergy_group_id);
        }


        public void SetSelectedCharacter(bool isSetSelected, bool isDropFx = false)
        {
            if (isSetSelected)
                SelectedOffSet += Vector3.up * 0.3f;
            else
            {
                if (isDropFx)
                    InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_area_landing, CurrentTile.View.CachedTr.position);
                SelectedOffSet -= Vector3.up * 0.3f;
            }
            if (_view == null)
                return;
            _view.SetSelected(isSetSelected);
            Color color = isSetSelected ? Color.gray : Color.white;
            _view.SetColor(color);
        }

        public void ChangeOccupiedTile(InGameTile newTile)
        {

            if (CurrentTile != null)
            {
                if (CurrentTile.OccupiedCharacter != null && CurrentTile.OccupiedCharacter == this)
                    CurrentTile.SetUnoccupied();
            }

            // 새로운 타일을 현재 타일로 설정하고, 새로운 타일에 캐릭터를 설정
            if (_statData != null)
                Debug.LogColor($"[Set Tile] {_statData.CharacterId} : ({newTile.X}, {newTile.Y})");
            CurrentTile = newTile;
            if (CurrentTile.OccupiedCharacter != null)
            {
                Debug.LogColor("CurrentTile.OccupiedCharacter != null");
            }

            CurrentTile.SetOccupied(this);
        }

        public bool NeedToBeCrowdControlState()
        {
            return HasCrowdControl(CrowdControlType.Airborne) || HasCrowdControl(CrowdControlType.KnockBack) || HasCrowdControl(CrowdControlType.Stun);
        }

        public bool HasCrowdControl(CrowdControlType type)
        {
            return (_crowdControlType & type) == type;
        }

        public void AddCrowdControl(CrowdControlType type)
        {
            if (_statData.Spec != null)
            {
                // if (_statData.Spec.is_taken_cc)
                // {
                _crowdControlType = _crowdControlType | type;
                AddBuffDebuffType(type.ToBuffDebuffType());
                // }
            }
        }

        public void RemoveCrowdControl(CrowdControlType type)
        {
            if (_statData.Spec != null)
            {
                // if (_statData.Spec.is_taken_cc)
                // {
                _crowdControlType = _crowdControlType & ~type;
                RemoveBuffDebuffType(type.ToBuffDebuffType());
                // }
            }
        }

        public void LookAtTarget()
        {
            _view.LookAt(CurrentTile, Target.CurrentTile);
        }

        private void OnAnimationEvent(AnimationKey animName, AnimationEventKey eventKey)
        {
            _currState?.AnimationEventCallback(animName, eventKey);
        }


        static EffectCodeType[] buffDebuffTypes = { EffectCodeType.Buff, EffectCodeType.Debuff };
        // Update is called once per frame
        public void ManagedUpdate(float dt)
        {
            if (_currState == null)
            {
                if (_nextState != null)
                {
                    _currState = _nextState;
                    _nextState = null;
                    _currState.StateInit(this);
                    _currState.StateStart();
                    OnStateChanged?.Invoke(_currState);
                }

                return;
            }

            float modifiedSpeedRate = HasCrowdControl(CrowdControlType.Slowing) ? InGameCalculator.CrowdControlSlowRate : 1f;

            // 기본 공격 쿨타임을 컨틀롤러에서 가지고 있는다.
            // 공격 스테이트가 아닌 스테이트에서도 쿨타임을 감소 시켜야 하기 때문
            if (_atkCoolTime > 0f)
            {
                _atkCoolTime += -dt * AttackSpeed * modifiedSpeedRate;
            }

            var tempPosition = Position;
            CharacterStateRunningResult result = _currState.CharacterStateRunning(dt * modifiedSpeedRate);

            var isAirborne = HasCrowdControl(CrowdControlType.Airborne);
            var isFreezing = HasCrowdControl(CrowdControlType.Freezing);
            var isKnockBack = HasCrowdControl(CrowdControlType.KnockBack);
            var isStun = HasCrowdControl(CrowdControlType.Stun);
            var isEntangle = HasCrowdControl(CrowdControlType.Entangle);
            var isSilence = HasCrowdControl(CrowdControlType.Silence);
            var isMisaRestraint = HasCrowdControl(CrowdControlType.MisaRestraint);

            if (isAirborne || isFreezing || isKnockBack || isStun || isEntangle || isMisaRestraint)
            {
                result &= ~CharacterStateRunningResult.CanCallMove;
            }

            if (isStun || isAirborne || isFreezing || isKnockBack || isSilence || isMisaRestraint)
            {
                result &= ~CharacterStateRunningResult.CanCallEffectCodeActivate;
            }

            var isPrologueMode = InGameMainFlowManager.Instance.CurrentFlowState is CookApps.AutoBattler.Prologue.FlowStatePrologueCombat;
            // 프롤로그 모드에서는 쿨타임 동작하지 않음
            if (isPrologueMode)
            {
                result &= ~CharacterStateRunningResult.CanCallEffectCodeOnCooltime;
            }

            if ((result & CharacterStateRunningResult.CanCallMove) == CharacterStateRunningResult.CanCallMove)
            {
                _view.UpdatePosition(position, ViewPosition3D, SelectedOffSet);
            }

            if ((result & CharacterStateRunningResult.CanCallEffectCodeOnUpdate) == CharacterStateRunningResult.CanCallEffectCodeOnUpdate)
            {
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnUpdate);
                EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnUpdateLambda, dt);
            }

            if (isAirborne || isKnockBack)
            {
                _view.UpdatePosition(position, ViewPosition3D, SelectedOffSet);
            }

            {
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnCooltime);
                if ((result & CharacterStateRunningResult.CanCallEffectCodeOnCooltime) == CharacterStateRunningResult.CanCallEffectCodeOnCooltime)
                {
                    if (!isSilence && !isMisaRestraint)
                    {
                        float skillCooltimeRate = InGameCalculator.CalculateCooltimeRate(SkillCooltimeRate);
                        EffectCodeForLoopHelper.CallWithArgs(effectCodes,
                            EffectCodeCharacterLambda.CallOnCooltimeLambda, dt / skillCooltimeRate);
                    }
                }

                for (var i = 0; i < effectCodes.Count; i++)
                {
                    if (effectCodes[i] is EffectCodeCharacterBase characterEffectCode)
                    {
                        var coolTimeData = characterEffectCode.GetCoolTimeData();
                        if (coolTimeData.skillIndex < 0)
                            continue;
                        GetHpBarView().OnCoolTimeUpdated(coolTimeData.skillIndex, coolTimeData.elapsedTime, coolTimeData.coolTime);
                    }
                }
            }

            if ((result & CharacterStateRunningResult.CanCallEffectCodeActivate) == CharacterStateRunningResult.CanCallEffectCodeActivate)
            {
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseIsReadyToActivate);
                EffectCodeStatBase effectCode = EffectCodeForLoopHelper.ReturnFirst(effectCodes, EffectCodeCharacterLambda.CallIsReadyToActivateLambda);
                if (effectCode is EffectCodeCharacterBase runEffectCode)
                {
                    // SKILL_READY 튜토리얼 처리 시도 - 튜토리얼이 처리되면 스킬 발동은 튜토리얼 종료 시 수행
                    bool tutorialHandled = TutorialSkillReadyHandler.TryHandleTutorial(
                        CharacterId,
                        () => runEffectCode.Activate());

                    if (!tutorialHandled)
                    {
                        // 튜토리얼이 없으면 바로 스킬 발동
                        runEffectCode.Activate();
                    }
                }
            }

            // BuffDebuff 쿨타임
            {
                GetHpBarView().RefreshCoolTimeBuffIcon(out bool isExpired);

                if (isExpired)
                {
                    // BuffDebuff 아이콘은 BuffIconTracker(SO 기반)에서 관리
                }
            }

            // Regen HP
            RecoverHP(dt);

            if (_nextState != null)
            {
                if (_currState.IsBlockingChangeState)
                    return;

                _currState.StateEnd(false);
                StatePool.Instance.Return(_currState);
                _currState = _nextState;
                _nextState = null;
                _currState.StateInit(this);
                _currState.StateStart();
                OnStateChanged?.Invoke(_currState);
            }
        }

        public void LateUpdate(float dt)
        {
            FollowHpBar();
        }

        private ObfuscatorFloat _recoveryHPElapsedTime;
        private void RecoverHP(float dt)
        {
            if (_currHp <= 0)
            {
                return;
            }

            if (RecoveryHP <= 0)
            {
                return;
            }

            _recoveryHPElapsedTime += dt;
            if (_recoveryHPElapsedTime > InGameCalculator.RegenHPPendingTime)
            {
                _recoveryHPElapsedTime = 0;
                _currHp += RecoveryHP;
                if (_currHp > HP)
                {
                    _currHp = HP;
                }

                UpdateHpBar();
            }
        }

        /// <summary>
        /// modify 이나 쿨타임을 더해주는 함수.
        /// </summary>
        /// <param name="cooltime"></param>
        public void AddSkillCooltimeInECC(float cooltime)
        {
            var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseAddSkillCooltime);
            EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallAddSkillCooltimeLambda, cooltime);
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_cooldown);
        }

        public StateBase AddNextState(Type stateType, object stateData = null)
        {
#if UNITY_EDITOR
            Debug.Log($"AddNextState >> {Time.frameCount}, {stateType}");
#endif

            var state = StatePool.Instance.Get(stateType) as CharacterStateBase;
            if (state == null)
                return null;

            if (_nextState != null && state.StatePriority < _nextState.StatePriority)
            {
                StatePool.Instance.Return(state);
                return null;
            }

            state.SetStateData(stateData);
            _nextState = state;

            return state;
        }

        public T AddNextState<T>(object stateData = null) where T : CharacterStateBase, new()
        {
#if UNITY_EDITOR
            // Debug.Log($"AddNextState >> {Time.frameCount}, {CharacId}, {CharacUId}, {typeof(T).ToString()}");
#endif
            // StateTypeMap에 등록된 타입이 있으면 해당 타입 사용, 없으면 기본 타입 사용
            CharacterStateBase state;
            Type requestedType = typeof(T);
            if (_stateTypeMap.TryGetValue(requestedType, out Type concreteType))
            {
                state = StatePool.Instance.Get(concreteType) as CharacterStateBase
                    ?? StatePool.Instance.Get<T>();
            }
            else
            {
                state = StatePool.Instance.Get<T>();
            }

            if (state == null)
                return null;

            if (_nextState != null && state.StatePriority < _nextState.StatePriority)
            {
                StatePool.Instance.Return(state);
                return null;
            }

            state.SetStateData(stateData);
            _nextState = state;
            return state as T;
        }

        public T ForceSetNextState<T>(object stateData = null) where T : CharacterStateBase, new()
        {
            var state = StatePool.Instance.Get<T>();
            if (state == null)
                return null;

            _currState?.ClearBlockingChangeState();
            state.SetStateData(stateData);
            _nextState = state;
            return state;
        }

        public CharacterStateBase GetCurrentState()
        {
            return _currState;
        }

        public void ClearAllState()
        {
            if (_currState != null)
            {
                _currState.StateEnd(true);
                StatePool.Instance.Return(_currState);
                _currState = null;
            }
            if (_nextState != null)
            {
                StatePool.Instance.Return(_nextState);
                _nextState = null;
            }
        }

        #region Buff Debuff Effect View Control
        /// <summary>
        /// BuffDebuffType의 레퍼런스카운트를 관리
        /// 공격력 버프가 2개 걸려있을 경우 캐릭터에 이펙트가 1개만 보여야 하므로 레퍼런스 카운트로 관리한다.
        /// </summary>
        /// <param name="type">버프나 디버프 타입</param>
        public void AddBuffDebuffType(BuffDebuffType type)
        {
            if (IsAlive == false)
            {
                return;
            }

            if (type == BuffDebuffType.None || type == BuffDebuffType.MAX)
            {
                return;
            }

            var vfxName = type.GetOneShotVfxName();
            if (vfxName != InGameVfxNameType.NONE)
                InGameVfxManager.Instance.AddInGameVfx(vfxName, SkillRootTransformFollowable);

            var sfxName = type.GetSoundFx();
            if (sfxName != SoundFX.NONE)
                SoundManager.Instance.PlaySFX(sfxName);

            if (!_buffDebuffRefCountDict.TryAdd(type, 1))
            {
                _buffDebuffRefCountDict[type] += 1;
            }

            if (_buffDebuffEffectViewDict.ContainsKey(type) == false)
            {
                var loopVfxName = type.GetLoopVfxName();
                if (loopVfxName != InGameVfxNameType.NONE)
                {
                    bool isUseOffset = loopVfxName == InGameVfxNameType.fx_common_debuff_stun || loopVfxName == InGameVfxNameType.fx_common_debuff_silence;
                    bool isNotFollowable = loopVfxName == InGameVfxNameType.fx_common_commander_skill_03;
                    if (isUseOffset)
                    {
                        if (isNotFollowable)
                        {
                            var effectView = InGameVfxManager.Instance.AddInGameVfx(loopVfxName,
                                CurrentTile.View.Position);
                            _buffDebuffEffectViewDict.Add(type, effectView);
                        }
                        else
                        {
                            var effectView = InGameVfxManager.Instance.AddInGameVfx(loopVfxName,
                                SkillRootTransformFollowable, new Vector3(0, SpecCharacter.height, 0));
                            _buffDebuffEffectViewDict.Add(type, effectView);
                        }
                    }
                    else
                    {
                        if (isNotFollowable)
                        {
                            var effectView =
                                InGameVfxManager.Instance.AddInGameVfx(loopVfxName, CurrentTile.View.Position);
                            _buffDebuffEffectViewDict.Add(type, effectView);
                        }
                        else
                        {
                            var effectView =
                                InGameVfxManager.Instance.AddInGameVfx(loopVfxName, SkillRootTransformFollowable);
                            _buffDebuffEffectViewDict.Add(type, effectView);
                        }
                    }
                }
            }
        }

        public void RemoveBuffDebuffType(BuffDebuffType type)
        {
            if (type is BuffDebuffType.None or BuffDebuffType.MAX)
            {
                return;
            }

            if (_buffDebuffRefCountDict.ContainsKey(type))
            {
                _buffDebuffRefCountDict[type] -= 1;

                if (_buffDebuffRefCountDict[type] <= 0)
                {
                    _buffDebuffRefCountDict[type] = 0;
                    if (_buffDebuffEffectViewDict.ContainsKey(type))
                    {
                        var effectView = _buffDebuffEffectViewDict[type];
                        _buffDebuffEffectViewDict.Remove(type);
                        InGameVfxManager.Instance.RemoveInGameVfx(effectView);
                    }
                }
            }//
        }

        public bool HasBuffDebuffType(BuffDebuffType type)
        {
            if (_buffDebuffRefCountDict == null)
                return false;

            if (_buffDebuffRefCountDict.TryGetValue(type, out ObfuscatorInt count))
            {
                return count > 0;
            }

            return false;
        }

        public bool HasDebuffType()
        {
            if (_buffDebuffRefCountDict == null)
                return false;

            foreach (var pair in _buffDebuffRefCountDict)
            {
                // 디버프 유형을 나타내는 열거형의 값이 1000 이상인지 확인합니다.
                if ((int)pair.Key >= 1000 && pair.Value > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void AddBuffStackData(int codeID, BuffStackData buffStackData)
        {
            if (codeID == 0)
                return;

            _buffDebuffs.Add((codeID, buffStackData));
            // 아이콘 출력은 BuffIconTracker(SO 기반)에서 관리
        }

        public void RemoveBuffStackData(BuffStackData buffStackData)
        {
            _buffDebuffs.RemoveAll(x => x.buffStackData == buffStackData);
        }

        public void RemoveBuffStackData(long codeID)
        {
            _buffDebuffs.RemoveAll(x => x.codeID == codeID);
        }
        public void SetBuffStackDataValue(long codeID, double value)
        {
            _buffDebuffs.Find(x => x.codeID == codeID).buffStackData.value = value;
        }
        #endregion


        #region Hp


        public void SetMaxHealth()
        {
            _currHp = HP;
            UpdateHpBar();
        }

        private void FollowHpBar()
        {
            // hpBarView.CachedTr.position = GetCharacterView().CachedTr.position + new Vector3(0, GetCharacterView().Height);
        }

        public void UpdateHpBar()
        {
            if (_hpBarView != null)
            {
                _hpBarView.SetValue(_currHp, HP, _currShield, AllianceType == AllianceType.Player);

                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnHpChange);
                EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnHpChangeLambda);
            }
        }
        #endregion

        private void KillEffectCode(CharacterController deadCharacter)
        {
            if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat
            || InGameMainFlowManager.Instance.CurrentFlowState is FlowStateTrialDungeonCombat)
            {
                bool isPlayer = _allianceType == AllianceType.Player;
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnKill);
                EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnKillLambda, deadCharacter);
            }
            // else if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat)
            // {
            //     bool isPlayer = _allianceType == AllianceType.Player;
            //     if (isPlayer)
            //     {
            //         InGameMain.GetInGameMain().AddKillLog(this, deadCharacter, isPlayer);
            //         var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnKill);
            //         EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnKillLambda, deadCharacter);
            //     }
            // }
        }

        public void MoveToCharacter(bool isInRange, CharacterController target)
        {
            if (NeedToBeCrowdControlState())
            {//이건 cc기 당했다면 cc상태로 변환
                AddNextState<CharacterStateCC>();
                return;
            }

            if (isInRange)
            {//타겟과의 거리가 범위안에있다면 idle로 변환
                AddNextState<CharacterStateIdle>();
                return;
            }

            if (target == null)//타겟이 없다면 idle로 변환
            {
                AddNextState<CharacterStateIdle>();
                return;
            }

            InGameTile bestTile = InGameObjectManager.Instance.GetNextMovableTile(CurrentTile, target.CurrentTile);
            if (bestTile == CurrentTile)
            {
                if (_notFoundTargetFx == null)
                    _notFoundTargetFx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_ice_01,
                        SkillRootTransformFollowable);
                GetCharacterView().LookAt(CurrentTile, target.CurrentTile);
                AddNextState<CharacterStateIdle>();
                return;
            }
            else
            {
                if (_notFoundTargetFx != null)
                {
                    _notFoundTargetFx.Remove();
                    _notFoundTargetFx = null;
                }
            }

            var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseIsReadyToActivate);
            EffectCodeStatBase effectCode = EffectCodeForLoopHelper.ReturnFirst(effectCodes, EffectCodeCharacterLambda.CallIsReadyToActivateLambda);
            if (effectCode is EffectCodeCharacterBase runEffectCode)
            {
                GetCharacterView().LookAt(CurrentTile, Target.CurrentTile);
                AddNextState<CharacterStateIdle>();
                return;
            }

            MoveTile(bestTile);
        }

        public void MoveToTile(InGameTile targetTile)
        {
            if (NeedToBeCrowdControlState())
            {//이건 cc기 당했다면 cc상태로 변환
                AddNextState<CharacterStateCC>();
                return;
            }

            InGameTile bestTile = InGameObjectManager.Instance.GetNextMovableTile(CurrentTile, targetTile);
            if (bestTile == CurrentTile)
            {//이 경우는 다음 타일로 움직이고있는 중이라는 뜻 같다. 근데 얼리리턴함.
                if (_notFoundTargetFx == null)
                    _notFoundTargetFx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_ice_01,
                        SkillRootTransformFollowable);
                GetCharacterView().LookAt(CurrentTile, targetTile);
                AddNextState<CharacterStateIdle>();
                return;
            }
            else
            {
                if (_notFoundTargetFx != null)
                {
                    _notFoundTargetFx.Remove();
                    _notFoundTargetFx = null;
                }
            }

            var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseIsReadyToActivate);
            EffectCodeStatBase effectCode = EffectCodeForLoopHelper.ReturnFirst(effectCodes, EffectCodeCharacterLambda.CallIsReadyToActivateLambda);
            if (effectCode is EffectCodeCharacterBase runEffectCode)
            {
                GetCharacterView().LookAt(CurrentTile, targetTile);
                AddNextState<CharacterStateIdle>();
                return;
            }

            MoveTile(bestTile);
        }

        public void MoveTile(InGameTile tile)
        {
            if (SpecCharacter.move_speed > 0)
            {
                OnTileMoveEnd();
                GetCharacterView().LookAt(CurrentTile, tile);
                ChangeOccupiedTile(tile);
                AddNextState<CharacterStateMove>();
            }
        }

        public void ForceMoveTile(InGameTile tile, float? customMoveSpeed = null)
        {
            if (SpecCharacter.move_speed > 0)
            {
                OnTileMoveEnd();
                GetCharacterView().LookAt(CurrentTile, tile);
                Unity.Mathematics.int2 beforeTile = CurrentTile.Int2Index;
                ChangeOccupiedTile(tile);
                Unity.Mathematics.int2 afterTile = CurrentTile.Int2Index;
                AddNextState<CharacterStateForceMove>(customMoveSpeed);
            }
        }

    }
}
