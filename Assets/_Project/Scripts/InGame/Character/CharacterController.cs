using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using CookApps.Obfuscator;
using CookApps.AutoBattler;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using Unity.Mathematics;
using UnityEngine.Tilemaps;
using Unity.Profiling;

namespace CookApps.BattleSystem
{
    public partial class CharacterController : IEffectCodeSource
    {
        public int CharacterUId => _characterUId;
        public int CharacterId => _statData?.CharacterId ?? _characterID;
        public SpecCharacter SpecCharacter => _statData.Spec;

        private Dictionary<Type, Type> _stateTypeMap = new Dictionary<Type, Type>();

        private EffectCodeContainer ecc;
        /// <summary>
        /// 타일 이동 종료 시 호출되는 함수입니다.
        /// 현재 CharacterStateMove의 StateEnd
        /// CharacterController의 MoveTile의 새로운 타일로 CurrentTile을 변경하기 전에 호출합니다.
        /// </summary>
        public void OnTileMoveEnd()
        {
            if (CurrentTile.EffectCodeContainer.GetEffectCode((int)EffectCodeNameType.CHAPTER_TRAP)
                                                is EffectCodeGameBase effectCodeGameBase)
            {
                effectCodeGameBase.OnTileMoveEnd(CurrentTile, this);
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


        public IFollowable SkillRootTransformFollowable => new SimpleSkillTransformFollowable(this);

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
            GameObject viewGo = await Addressables.InstantiateAsync(
                $"Obstacle/{id}/GenerateResources/CharacterView_{id}.prefab");
            _view = viewGo.GetComponent<SpriteCharacterView>();
            _view.CachedTr.SetParent(Playground, false);
            _view.CachedTr.localPosition = position;
        }

        public async UniTask Initialize(CharacterStatData statData, InGameTile tile, AllianceType allianceType, bool hasSkill, HpBarType type = HpBarType.None)
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

            GameObject viewGo = null;

            if (statData.Spec.character_type == CharacterType.CHARACTER)
            {
                viewGo = await Addressables.InstantiateAsync(
                    $"Characters/{_statData.Spec.prefab_id}/GenerateResources/CharacterView_{_statData.Spec.prefab_id}.prefab");
            }
            else if (statData.Spec.character_type == CharacterType.OBSTACLE)
            {
                viewGo = await Addressables.InstantiateAsync(
                    $"Obstacle/Stage/{_statData.Spec.prefab_id}/GenerateResources/CharacterView_{_statData.Spec.prefab_id}.prefab");
            }
            else
            {
                viewGo = await Addressables.InstantiateAsync(
                    $"Mob/{_statData.Spec.prefab_id}/GenerateResources/CharacterView_{_statData.Spec.prefab_id}.prefab");
            }


            _view = viewGo.GetComponent<SpriteCharacterView>();
            if (_statData.Spec != null)
            {
                _hpBarView = InGameHpBarViewPool.Instance.Get();
                _hpBarView.Initialize(statData, allianceType);
                _hpBarView.SetHpBarType(type);
                _view.SetHpBarView(_hpBarView, _statData.Spec.height);
                _view.SetFirstDirection(allianceType);
                if (_statData.Spec.prefab_id == 10101 || _statData.Spec.prefab_id == 10201 || _statData.Spec.prefab_id == 10401/* ||
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
                IsAlive = true;
            }

        }


        public void AddViewScaleFactor(float viewScaleValue)
        {
            _view.AddViewScale(viewScaleValue);
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

        public void Clear()
        {
            if (_statData != null)
            {
                InGameHpBarViewPool.Instance.Return(_hpBarView);
                ecc.Clear();
                ecc.OnChangedDirtyFlag -= EffectCodeOnChangedDirtyFlagHandler;
                foreach (var pair in _buffDebuffEffectViewDict)
                {
                    InGameVfxManager.Instance.RemoveInGameVfx(pair.Value);
                }

                _buffDebuffEffectViewDict.Clear();
                _view.OnAnimationEvent -= OnAnimationEvent;
            }
            Addressables.ReleaseInstance(_view.gameObject);
            ClearAllState();
            Target = null;
            _view = null;
            _hpBarView = null;
        }

        private void AddSkillEffectCodes()
        {
            Span<double> stats = stackalloc double[8];
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

                    var effectCodeInfo = new EffectCodeInfo(skillDataList[0].skill_id, 0, stats);
                    ecc.AddOrMergeEffectCode(effectCodeInfo, this);
                }
            }
        }
        public void InjectSynergy(long effectCodeID, SpecSynergy synergyData)
        {
            Span<double> stats = stackalloc double[3];
            stats[0] = synergyData.stat_value;
            stats[1] = synergyData.stat_value_2;
            stats[2] = synergyData.grade;
            var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, stats);
            ecc.AddOrMergeEffectCode(effectCodeInfo, this);
        }

        public void AddSynergyApplyEach(SynergyType targetSynergyType, long effectCodeID, SpecSynergy synergyData)
        {
            //이건좀 고민
            if (targetSynergyType != _statData.Spec.element_type
            && targetSynergyType != _statData.Spec.asterism_type)
                return;

            InjectSynergy(effectCodeID, synergyData);
        }

        public void RemoveSynergyEffectCode()
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

                ecc.RemoveEffectCode(outSynergyList[0].id);
            }
        }

        public void InjectPassive(long effectCodeID, SpecPassive passiveData)
        {
            Span<double> stats = stackalloc double[5];
            stats[0] = (double)passiveData.skill_value_type;
            stats[1] = passiveData.passive_rate;
            stats[2] = passiveData.grade;
            stats[3] = (double)passiveData.skill_value_type_2;
            stats[4] = passiveData.passive_rate_2;
            var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, stats);
            ecc.AddOrMergeEffectCode(effectCodeInfo, this);
        }

        public void SetSelectedCharacter(bool isSetSelected)
        {
            if (isSetSelected)
                SelectedOffSet += Vector3.up * 0.3f;
            else
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_area_landing, CurrentTile.View.CachedTr.position);
                SelectedOffSet -= Vector3.up * 0.3f;
            }
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
                if (_statData.Spec.is_taken_cc)
                {
                    _crowdControlType = _crowdControlType | type;
                    AddBuffDebuffType(type.ToBuffDebuffType());
                }
            }
        }

        public void RemoveCrowdControl(CrowdControlType type)
        {
            if (_statData.Spec != null)
            {
                if (_statData.Spec.is_taken_cc)
                {
                    _crowdControlType = _crowdControlType & ~type;
                    RemoveBuffDebuffType(type.ToBuffDebuffType());
                }
            }
        }

        public void LookAtTarget()
        {
            _view.LookAt(CurrentTile, Target.CurrentTile);
        }

        private void OnAnimationEvent(AnimationKey animName, AnimationEventKey eventKey)
        {
            _currState.AnimationEventCallback(animName, eventKey);
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

            var isPrologueMode = InGameMainFlowManager.Instance.CurrentFlowState is FlowStatePrologueCombat;
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
                    runEffectCode.Activate();
                }
            }

            // BuffDebuff 쿨타임
            {
                GetHpBarView().RefreshCoolTimeBuffIcon(out bool isExpired);

                if (isExpired)
                {
                    // BuffDebuff 아이콘 재구축
                    GetHpBarView().RestructBuffIcon(_buffDebuffs);
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

            var affectText = type.GetAffectToken();
            if (!string.IsNullOrEmpty(affectText))
            {
                if (type == BuffDebuffType.Shield)
                {
                    string hexColor = "#00ABFF";
                    ShowNormalText(affectText, hexColor: hexColor).Forget();
                }
                else
                {
                    string hexColor = (int)type >= 1000 ? "#5DC9FFFF" : "#FF5149";
                    ShowNormalText(affectText, hexColor: hexColor).Forget();
                }
            }

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
            }
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
            _hpBarView.RestructBuffIcon(_buffDebuffs);
        }

        public void RemoveBuffStackData(BuffStackData buffStackData)
        {
            _buffDebuffs.RemoveAll(x => x.buffStackData == buffStackData);
            _hpBarView.RestructBuffIcon(_buffDebuffs);
        }

        public void RemoveBuffStackData(long codeID)
        {
            _buffDebuffs.RemoveAll(x => x.codeID == codeID);
            _hpBarView.RestructBuffIcon(_buffDebuffs);
        }
        public void SetBuffStackDataValue(long codeID, double value)
        {
            _buffDebuffs.Find(x => x.codeID == codeID).buffStackData.value = value;
            _hpBarView.RestructBuffIcon(_buffDebuffs);
        }
        #endregion

        private bool CriticalTest()
        {
            return InGameRandomManager.GetUniversalRandomValue(0f, 100f) < (_fixedCriticalProb + CriticalProb) * 100; // OK
        }

        public void SetFixedCriticalProb(float fixedCriticalProb)
        {
            _fixedCriticalProb = fixedCriticalProb;
        }
        public void ResetFixedCriticalProb()
        {
            _fixedCriticalProb = 0f;
        }

        public bool PureDamageTest()
        {
            return InGameRandomManager.GetUniversalRandomValue(0f, 100f) < PureDamageProb * 100; // OK
        }

        private bool DoubleCriticalTest()
        {
            return InGameRandomManager.GetUniversalRandomValue(0f, 100f) < DoubleCriticalProb; // OK
        }

        public struct DamageInfo
        {
            public AttackerType attackerType;
            public ObfuscatorDouble damageAmount;
            public bool isAD;
            public bool isCritical;
            public bool isDoubleCritical;
            public ElementAdvantageHelper.ElementAdvantageResult elementAdvantageResult;
            public long source;

            // 외부에서 DamageInfo를 생성할 때 사용하는 함수
            public static DamageInfo Create(double damageAmount, long source, AttackerType attackerType, bool isAD = true, bool isCritical = false, bool isDoubleCritical = false)
            {
                return new DamageInfo
                {
                    damageAmount = damageAmount,
                    source = source,
                    attackerType = attackerType,
                    isAD = isAD,
                    isCritical = isCritical,
                    isDoubleCritical = isDoubleCritical
                };
            }
        }

        /// <summary>
        /// 기본 스텟으로 대미지 계산
        /// 퓨어 대미지의 경우 이 함수를 거치지 않고 바로 PostCalculateDamageAmount를 호출
        /// </summary>
        /// <param name="ad">공격자가 순수하게 입히려고 하는 물리 대미지</param>
        /// <param name="ap">공격자가 순수하게 입히려고 하는 마법 대미지</param>
        /// <param name="target">대상</param>
        /// <param name="source">기본 공격일 경우 0, 스킬일 경우 effectCodeId</param>
        /// <param name="isSkill">스킬로 입히는 대미지인지</param>
        /// <returns></returns>
        public DamageInfo PrecalculateDamageAmount(double ad, double ap, CharacterController target, long source, bool isSkill)
        {
            double damage = InGameCalculator.CalculateDefaultDamage(ad, ap, this, target);

            var damageInfo = new DamageInfo();
            damageInfo.attackerType = AttackerType.CHARCTER;
            damageInfo.isAD = true;

            if (isSkill)
            {
                damage *= SkillDamageRate;
                if (ap > 0)
                    damageInfo.isAD = false;
            }


            damageInfo.isCritical = CriticalTest();
            if (damageInfo.isCritical)
            {
                damage *= CriticalDamageRate;
                damageInfo.isDoubleCritical = DoubleCriticalTest();
                if (damageInfo.isDoubleCritical)
                {
                    damage *= DoubleCriticalDamageRate;
                }
            }

            damageInfo.damageAmount = damage;
            damageInfo.source = source;
            return damageInfo;
        }

        /// <summary>
        /// 주는/받는 피해량 계수로 최종 대미지 계산
        /// </summary>
        /// <param name="damageInfo">계산 전 대미지 정보</param>
        /// <param name="target">피해 받는 대상</param>
        public void PostCalculateDamageAmount(ref DamageInfo damageInfo, CharacterController target = null)
        {
            // 최소 대미지량
            double minDamageAmount = InGameCalculator.MinDamageRate * damageInfo.damageAmount;
            // 대미지 증감에 따른 최종 대미지 계산
            damageInfo.damageAmount = damageInfo.damageAmount * AttackDamageRate * (target?.TakenDamageRate ?? 1f);
            // 추가로 종족, 크기, 속성에 따른 대미지 계산이 필요하다면 여기서 할 것
            CalculateElementAdvantageDamage(ref damageInfo, target);
            // 최소 대미지량 적용
            damageInfo.damageAmount = Math.Floor(Math.Max(minDamageAmount, damageInfo.damageAmount));

            if (ecc != null)
            {
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseModifyDamageAmount);
                damageInfo.damageAmount = EffectCodeForLoopHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallModifyDamageAmountLambda, damageInfo.damageAmount.Value);
            }
        }

        /// <summary>
        /// 실제로 대미지를 입히는 함수
        /// </summary>
        /// <param name="damageInfo">계산된 대미지 정보</param>
        /// <param name="attacker">공격자</param>
        /// <param name="isFirstDamage">
        /// 스킬중에 피해를 받으면 받는 피해의 일부를 주변 동료에게 넘기는 스킬을 가진 캐릭터가 둘 있다면,
        /// 그 둘중 하나가 피헤를 받을 경우, 둘이서 피해를 무한으로 넘기고 받을 수 있을 텐데,
        /// 이를 막기위해 첫번째 피해인 것을 명시하여 이후의 피해는 스킬 효과를 적용하지 않도록 한다.
        /// </param>
        /// <returns>
        /// 대미지를 입힌 후 상태
        /// "스킬로 상대를 죽인 경우 쿨타임 초기화" 이런 것을 처리하기 위해 반환값을 사용
        /// </returns>
        public DamageReturnType GetDamaged(in DamageInfo damageInfo, CharacterController attacker,
        bool isFirstDamage = true, string hexColor = null)
        {
            if (!InGameManager.Instance.IsInGameCombat)
                return DamageReturnType.Damaging;

            // 로비 전투 상황일 때
            if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStateLobbyCombat)
            {
                if (attacker.AllianceType == AllianceType.Enemy)
                    return DamageReturnType.Damaging;
            }
            // 같은 틱에 대미지를 줘서 여러번 죽이는 경우가 있어서 이미 죽었는지 체크
            if (_currHp <= 0 || _view == null)
            {
                return DamageReturnType.AlreadyDead;
            }

            var originDamageAmount = damageInfo.damageAmount.Value;
            var damageAmount = damageInfo;
            // effectCode에게 이벤트 전달
            {
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnDamaged);
                damageAmount = EffectCodeForLoopHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallOnDamagedLambda, damageInfo, this, isFirstDamage);
            }

            // effectCode에게 이벤트 전달
            if (damageInfo.isCritical)
            {
                var effectCodes = attacker.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnCritical);
                EffectCodeForLoopHelper.Call(effectCodes, EffectCodeCharacterLambda.CallOnCriticalLambda);
            }

            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_hit_01, SkillRootTransformFollowable);
            GetCharacterView().OnHit();

            ShowDamageText(damageAmount.damageAmount.Value, damageInfo.isCritical, damageInfo.elementAdvantageResult, hexColor).Forget();

            _currHp -= damageAmount.damageAmount.Value;

            InGameStatistics.Instance.AddCombatDamage(attacker, this, damageInfo.damageAmount, _currHp, damageInfo.source);
            UpdateHpBar();

            if (_currHp <= 0)
            {
                _currHp = 0;
                IsAlive = false;

                if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStatePvpCombat
                    || InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat
                    || InGameMainFlowManager.Instance.CurrentFlowState is FlowStateTrialDungeonCombat)
                {
                    switch (damageInfo.attackerType)
                    {
                        case AttackerType.CHARCTER:
                            InGameMain.GetInGameMain().AddKillLog(attacker, this, attacker.AllianceType == AllianceType.Player);
                            break;
                        case AttackerType.COMMANDER_SKILL:
                            var commanderSkill = SpecDataManager.Instance.GetCommanderSkillDataList((int)damageInfo.source)[0];
                            InGameMain.GetInGameMain().AddKillLog(commanderSkill, this, AllianceType != AllianceType.Player);
                            break;
                        case AttackerType.CHAPTER_RULE:
                            var chapterRule = SpecDataManager.Instance.GetChapterRuleData((int)damageInfo.source);
                            InGameMain.GetInGameMain().AddKillLog(chapterRule, this, AllianceType != AllianceType.Player);
                            break;
                    }
                }

                var deathInfo = new DeathInfo { attacker = attacker };
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnDead);
                deathInfo = EffectCodeForLoopHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallOnDeadLambda, deathInfo);

                if (!deathInfo.isUseCustomState)
                {
                    ForceSetNextState<CharacterStateDead>();

                    if (LobbyMain.GetLobbyMain() != null)
                    {
                        SpawnDropFx((targetLine) =>
                        {
                            if (targetLine != null)
                                targetLine.Remove();
                        });

                    }
                }

                return DamageReturnType.Killed;
            }

            return DamageReturnType.Damaging;
        }

        #region Hp
        public void ForceSetHp(double hp)
        {
            _currHp = Math.Min(hp, HP);
            UpdateHpBar();
        }

        private void FollowHpBar()
        {
            // hpBarView.CachedTr.position = GetCharacterView().CachedTr.position + new Vector3(0, GetCharacterView().Height);
        }

        public void UpdateHpBar()
        {
            _hpBarView.SetValue(_currHp, HP, _currShield);
        }

        public void RefreshHp()
        {
            _hpBarView.SetValue(_currHp, HP, _currShield);
        }
        #endregion

        private void KillEffectCode(CharacterController deadCharacter)
        {
            if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStatePvpCombat
            || InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat
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

        private void CalculateElementAdvantageDamage(ref DamageInfo damageInfo, CharacterController target = null)
        {
            damageInfo.elementAdvantageResult = ElementAdvantageHelper.ElementAdvantageResult.NONE;
            if (target == null)
                return;

            damageInfo.elementAdvantageResult = ElementAdvantageHelper.GetElementAdvantageResult(this.SpecCharacter.element_type,
                                                                                        target.SpecCharacter.element_type);
            switch (damageInfo.elementAdvantageResult)
            {
                case ElementAdvantageHelper.ElementAdvantageResult.ADVANTAGE:
                    damageInfo.damageAmount *= ElementAdvantageHelper.ADVANTAGE_MULTIPLIER;
                    return;
                case ElementAdvantageHelper.ElementAdvantageResult.RESIST:
                    damageInfo.damageAmount *= ElementAdvantageHelper.RESIST_MULTIPLIER;
                    return;
            }
        }

        /// <summary>
        /// 회복량 계산
        /// </summary>
        /// <param name="recoveryAmount"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public double PostCalculateHealAmount(double recoveryAmount, CharacterController target)
        {
            // 주는/받는 회복량 계수로 회복량 계산
            recoveryAmount = Math.Round(recoveryAmount * GivenHealRate * target.TakenHealRate);
            // 속성, 크기, 종족에 따른 회복량 계산이 필요하다면 여기서 할 것

            {
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseModifyHealAmount);
                recoveryAmount = EffectCodeForLoopHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallModifyHealAmountLambda, recoveryAmount);
            }

            return recoveryAmount;
        }

        /// <summary>
        /// 회복
        /// </summary>
        /// <param name="amount">힐량</param>
        /// <param name="healer">힐을 준 캐릭터</param>
        /// <param name="source">힐이 사용된 effectCodeId</param>
        /// <param name="isFirstHeal">
        /// 스킬중에 힐을 받으면 주변 동료에게 받은 힐량의 절반을 회복하는 스킬을 가진 캐릭터가 둘 있다면,
        /// 그 둘중 하나가 힐을 받을 경우, 둘이서 회복을 계속 하면서 무한으로 주고 받을 수 있을텐데,
        /// 이를 막기위해 첫번째 힐인 것을 명시하여 이후의 힐은 스킬 효과를 적용하지 않도록 한다.
        /// </param>
        /// <returns>회복 됬는지 유무</returns>
        public bool GetHealed(double amount, CharacterController healer, long source, bool isFirstHeal = true)
        {
            // 죽어있으면 안준다.
            if (_currHp <= 0 || !IsAlive)
            {
                return false;
            }

            {
                // effectCode에게 이벤트 전달
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnHealed);
                EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnHealedLambda, amount, isFirstHeal);
                _currHp += amount;
            }

            ShowHealText(amount).Forget();

            if (_currHp > HP)
            {
                _currHp = HP;
            }

            InGameStatistics.Instance.AddCombatHeal(healer, this, amount, _currHp, HP, source);

            UpdateHpBar();
            return true;
        }

        private async UniTask ShowDamageText(double amount, bool isCritical,
                                            ElementAdvantageHelper.ElementAdvantageResult elementAdvantageResult,
                                            string hexColor = null)
        {
            if (amount == 0)
            {
                return;
            }
            InGameTextView textView = InGameTextViewPool.Instance.Get();
            await textView.ShowDamageText(GetCharacterView().CachedTr.position, _statData.Spec.height, amount, isCritical, hexColor);

            if (elementAdvantageResult == ElementAdvantageHelper.ElementAdvantageResult.NONE)
            {
                return;
            }
            textView.AttachElementAdvanageText(elementAdvantageResult, isCritical);
        }

        private async UniTask ShowHealText(double amount)
        {
            if (amount == 0)
            {
                return;
            }
            InGameTextView textView = InGameTextViewPool.Instance.Get();
            await textView.ShowHealText(GetCharacterView().CachedTr.position, _statData.Spec.height, amount);
        }

        public async UniTask ShowShieldText(double amount)
        {
            if (amount == 0)
            {
                return;
            }
            InGameTextView textView = InGameTextViewPool.Instance.Get();
            await textView.ShowShieldText(GetCharacterView().CachedTr.position, _statData.Spec.height, amount);
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
            {//이 경우는 다음 타일로 움직이고있는 중이라는 뜻 같다. 근데 얼리리턴함.
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
                ChangeOccupiedTile(tile);
                AddNextState<CharacterStateForceMove>(customMoveSpeed);
            }
        }

        public InGameVfxTargetLine SetLine(CharacterController character, bool isOwn, Action<InGameVfxTargetLine> onComplete = null)
        {
            var obj = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.TargetLineRenderer, Position3D);
            if (obj != null)
            {
                InGameVfxTargetLine targetLine = obj.GetComponent<InGameVfxTargetLine>();
                targetLine.SetActiveObject(true);
                targetLine.TargetLine.DrawLine(this, character, isOwn, () =>
                {
                    if (onComplete != null)
                        onComplete.Invoke(targetLine);
                });

                return targetLine;
            }

            return null;
        }
        public void ReUseLine(InGameVfxTargetLine targetLine, CharacterController character, bool isOwn, Action<InGameVfxTargetLine> onComplete = null)
        {
            if (targetLine == null) return;
            targetLine.SetActiveObject(true);
            targetLine.TargetLine.DrawLine(this, character, isOwn, () =>
                {
                    if (onComplete != null)
                        onComplete.Invoke(targetLine);
                });

        }

        public InGameVfxTargetLine SpawnDropFx(Action<InGameVfxTargetLine> onComplete = null)
        {
            var obj = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.DropFx, Position3D);
            if (obj != null)
            {
                InGameVfxTargetLine targetLine = obj.GetComponent<InGameVfxTargetLine>();

                Transform targetPos = LobbyMain.GetLobbyMain().GetIdleRewardTransform;
                Camera mainCamera = Camera.main;
                Vector3 screenPos = new Vector3(targetPos.position.x, targetPos.position.y, mainCamera.nearClipPlane);
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
                targetLine.TargetLine.DrawLine(Position3D, worldPos, () =>
                {
                    if (onComplete != null)
                    {
                        onComplete.Invoke(targetLine);
                        LobbyMain.GetLobbyMain().PlayDropFx();
                    }
                });

                return targetLine;
            }

            return null;
        }

        public async UniTask ShowNormalText(string token, string hexColor = null)
        {
            InGameTextView textView = InGameTextViewPool.Instance.Get();

            string convertText = LanguageManager.Instance.GetLanguageText(token);
            await textView.ShowNormalText(GetCharacterView().CachedTr.position, _statData.Spec.height, convertText, hexColor);
        }
    }
}
