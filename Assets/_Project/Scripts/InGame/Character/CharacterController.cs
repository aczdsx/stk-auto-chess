using System;
using System.Collections.Generic;
using CookApps.Obfuscator;
using CookApps.AutoBattler;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public partial class CharacterController : IEffectCodeSource, IFollowable
    {
        public static Type DefaultDeadState;
        public int CharacterUId => _characterUId;
        public int CharacterId => _statData.CharacterId;

        private EffectCodeContainer ecc;

        public EffectCodeContainer GetEffectCodeContainer()
        {
            return ecc;
        }

        private CharacterStatData _statData;

        public CharacterStatData GetCharacterStat()
        {
            return _statData;
        }

        private SpriteCharacterView view = null;

        public SpriteCharacterView GetCharacterView()
        {
            return view;
        }

        public event Action<CharacterStateBase> OnStateChanged;

        public CharacterController Target { get; set; }
        public InGameTile CurrentTile { get; set; }

        public bool IsAlive { get; set; }
        public bool IsForceIdle { get; set; }
        public bool IsBlockChangeState { get; set; }

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

        private CharacterStateBase _currState;
        private Queue<CharacterStateBase> _nextStates = new ();

        private ObfuscatorFloat _atkCoolTime;

        private Dictionary<BuffDebuffType, ObfuscatorInt> _buffDebuffRefCountDict;
        private Dictionary<BuffDebuffType, InGameEffectView> _buffDebuffEffectViewDict;

        private static int characUIdInc;
        private int _characterUId;

        public void Initialize(CharacterStatData statData, InGameTile tile, AllianceType allianceType)
        {
            //[TODO] 빈 오브젝트 생성하고 안에 넣으라고 하셨던거 같은데... 지금은 내부에 하나 더 생성
            Debug.LogColor("CharacterController Initialize : " + statData.CharacterId);
            _characterUId = characUIdInc++;
            _statData = statData;
            CurrentTile = tile;
            position = tile.View.Position;
            tile.SetOccupied(this);
            _allianceType = allianceType;

            view = SpriteCharacterViewPool.Instance.GetCharacterView(statData, _allianceType);
            if (_allianceType == AllianceType.Enemy)
            {
                FlipX = true;
            }
            _hpBarView = InGameHpBarViewPool.Instance.GetHpBar();
            _hpBarView.Initialize(statData);
            FollowHpBar();

            view.OnAnimationEvent += OnAnimationEvent;
            view.CachedTr.localPosition = position;
            _buffDebuffRefCountDict = new Dictionary<BuffDebuffType, ObfuscatorInt>();
            _buffDebuffEffectViewDict = new Dictionary<BuffDebuffType, InGameEffectView>();

            // add EffectCodes
            ecc = new EffectCodeContainer(this);
            needUpdateFlag = EffectCodeInheritFlagExtensions.AllFlags();
            ecc.OnChangedDirtyFlag += EffectCodeOnChangedDirtyFlagHandler;

            _currHp = HP;
            IsAlive = true;
            IsForceIdle = false;
        }

        public void Clear()
        {
            Target = null;
            ClearAllState();
            view.OnAnimationEvent -= OnAnimationEvent;
            ecc.Clear();
            SpriteCharacterViewPool.Instance.ReturnCharacterView(view);
            view = null;
            InGameHpBarViewPool.Instance.ReturnHpBar(_hpBarView);
            _hpBarView = null;
        }

        public void SetSelectedCharacter(bool isSetSelected)
        {
            view.SetSelected(isSetSelected);
        }

        public void ChangeTile(InGameTile newTile)
        {
            if (CurrentTile != null)
            {
                if (CurrentTile.OccupiedCharacter != null && CurrentTile.OccupiedCharacter == this)
                    CurrentTile.SetOccupied(null);
            }

            // 새로운 타일을 현재 타일로 설정하고, 새로운 타일에 캐릭터를 설정
            CurrentTile = newTile;
            position = CurrentTile.View.Position;
            newTile.SetOccupied(this);

        }

        public bool NeedToBeIdle()
        {
            return HasCrowdControl(CrowdControlType.Airborne) || HasCrowdControl(CrowdControlType.KnockBack) || HasCrowdControl(CrowdControlType.Stun) || IsForceIdle;
        }

        public bool HasCrowdControl(CrowdControlType type)
        {
            return (_crowdControlType & type) == type;
        }

        public void AddCrowdControl(CrowdControlType type)
        {
            _crowdControlType = _crowdControlType | type;
            AddBuffDebuffType(type.ToBuffDebuffType());
        }

        public void RemoveCrowdControl(CrowdControlType type)
        {
            _crowdControlType = _crowdControlType & ~type;
            RemoveBuffDebuffType(type.ToBuffDebuffType());
        }

        private void OnAnimationEvent(string animName, AnimationEventKey eventKey)
        {
            _currState.AnimationEventCallback(animName, eventKey);
        }

        // Update is called once per frame
        public void ManagedUpdate(float dt)
        {
            if (_currState == null)
            {
                if (_nextStates.Count > 0)
                {
                    _currState = _nextStates.Dequeue();
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
            if (isAirborne || isFreezing || isKnockBack || isStun || isEntangle)
            {
                result &= ~CharacterStateRunningResult.CanCallMove;
                if (isStun || isAirborne || isFreezing || isKnockBack)
                {
                    result &= ~CharacterStateRunningResult.CanCallEffectCodeActivate;
                }
            }

            if (result.HasFlag(CharacterStateRunningResult.CanCallMove))
            {
                view.UpdatePosition(position, ViewPosition3D);
            }

            view.LookAt(FlipX);

            if (result.HasFlag(CharacterStateRunningResult.CanCallEffectCodeOnUpdate))
            {
                List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnUpdate);
                EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnUpdateLambda, dt);
            }

            if (isAirborne || isKnockBack)
            {
                view.UpdatePosition(position, ViewPosition3D);
            }

            if (result.HasFlag(CharacterStateRunningResult.CanCallEffectCodeOnCooltime))
            {
                List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnCooltime);
                float skillCooltimeRate = InGameCalculator.CalculateCooltimeRate(SkillCooltimeRate);
                EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnCooltimeLambda, dt / skillCooltimeRate);
            }

            if (result.HasFlag(CharacterStateRunningResult.CanCallEffectCodeActivate))
            {
                List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseIsReadyToActivate);
                EffectCodeStatBase effectCode = EffectCodeForLoopHelper.ReturnFirst(effectCodes, EffectCodeCharacterLambda.CallIsReadyToActivateLambda);
                if (effectCode is EffectCodeCharacterBase runEffectCode)
                {
                    runEffectCode.Activate();
                }
            }

            // Regen HP
            RecoverHP(dt);

            if (_nextStates.Count > 0)
            {
                if (IsBlockChangeState)
                {
                    return;
                }

                _currState.StateEnd(false);
                StatePool.Instance.Push(_currState);
                _currState = _nextStates.Dequeue();
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

                UpdateHp();
            }
        }

        public StateBase AddNextState(Type stateType, object stateData = null)
        {
#if UNITY_EDITOR
            // Debug.Log($"AddNextState >> {Time.frameCount}, {CharacId}, {CharacUId}, {stateType}");
#endif
            StateBase state = StatePool.Instance.GetState(stateType);
            if (state != null)
            {
                state.SetStateData(stateData);
                _nextStates.Enqueue(state as CharacterStateBase);
            }

            return state;
        }

        public T AddNextState<T>(object stateData = null) where T : CharacterStateBase, new()
        {
#if UNITY_EDITOR
            // Debug.Log($"AddNextState >> {Time.frameCount}, {CharacId}, {CharacUId}, {typeof(T).ToString()}");
#endif
            var state = StatePool.Instance.GetState<T>();
            state.SetStateData(stateData);
            _nextStates.Enqueue(state);
            return state;
        }

        public void ForceSetNextState<T>(object stateData = null) where T : CharacterStateBase, new()
        {
            ClearAllState();
            var state = StatePool.Instance.GetState<T>();
            _currState = state;
            _currState.SetStateData(stateData);
            _currState.StateInit(this);
            _currState.StateStart();
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
                StatePool.Instance.Push(_currState);
                while (_nextStates.Count > 0)
                {
                    CharacterStateBase nextState = _nextStates.Dequeue();
                    StatePool.Instance.Push(nextState);
                }
            }
        }

        #region IFollowable
        public Vector3 GetPosition()
        {
            return Vector3.zero;
        }

        public int GetSortingLayerOrder()
        {
            return 0;
        }
        #endregion

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

            _buffDebuffRefCountDict[type] += 1;
            if (_buffDebuffEffectViewDict.ContainsKey(type) == false)
            {
                var effectView = InGameEffectManager.Instance.Get(type, view.CachedTr);
                _buffDebuffEffectViewDict.Add(type, effectView);
            }
        }

        public void RemoveBuffDebuffType(BuffDebuffType type)
        {
            if (type is BuffDebuffType.None or BuffDebuffType.MAX)
            {
                return;
            }

            _buffDebuffRefCountDict[type] -= 1;
            if (_buffDebuffRefCountDict[type] <= 0)
            {
                _buffDebuffRefCountDict[type] = 0;
                if (_buffDebuffEffectViewDict.ContainsKey(type))
                {
                    var effectView = _buffDebuffEffectViewDict[type];
                    _buffDebuffEffectViewDict.Remove(type);
                    InGameEffectManager.Instance.RemoveInGameEffect(effectView);
                }
            }
        }

        public bool HasBuffDebuffType(BuffDebuffType type)
        {
            if (_buffDebuffRefCountDict[type] > 0)
            {
                return true;
            }

            return false;
        }
        #endregion

        private bool CriticalTest()
        {
            return InGameRandomManager.GetUniversalRandomValue(0f, 100f) < CriticalProb; // OK
        }

        private bool DoubleCriticalTest()
        {
            return InGameRandomManager.GetUniversalRandomValue(0f, 100f) < DoubleCriticalProb; // OK
        }

        public struct DamageInfo
        {
            public ObfuscatorDouble damageAmount;
            public bool isCritical;
            public bool isDoubleCritical;
            public ObfuscatorInt source;
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
        public DamageInfo PrecalculateDamageAmount(double ad, double ap, CharacterController target, int source, bool isSkill)
        {
            double damage = InGameCalculator.CalculateDefaultDamage(ad, ap, this, target);

            var damageInfo = new DamageInfo();

            if (isSkill)
            {
                damage *= SkillDamageRate;
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

            // 최소 대미지량 적용
            damageInfo.damageAmount = Math.Floor(Math.Max(minDamageAmount, damageInfo.damageAmount));
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
        public DamageReturnType GetDamaged(in DamageInfo damageInfo, CharacterController attacker, bool isFirstDamage = true)
        {
            // 같은 틱에 대미지를 줘서 여러번 죽이는 경우가 있어서 이미 죽었는지 체크
            if (_currHp <= 0 || view == null)
            {
                return DamageReturnType.AlreadyDead;
            }

            GetCharacterView().OnHit();
            ShowDamageText(damageInfo.damageAmount, damageInfo.isCritical, damageInfo.isDoubleCritical).Forget();

            _currHp -= damageInfo.damageAmount;
            InGameStatistics.Instance.AddCombatDamage(attacker, this, damageInfo.damageAmount, _currHp, damageInfo.source);

            UpdateHp();

            if (_currHp <= 0)
            {
                _currHp = 0;
                IsAlive = false;
                if (attacker != null)
                {
                    attacker.IncreaseKillCount(this);
                }

                var deathInfo = new DeathInfo {attacker = attacker};
                List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnDead);
                deathInfo = EffectCodeForLoopHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallOnDeadLambda, deathInfo);

                if (!deathInfo.isUseCustomState && DefaultDeadState != null)
                {
                    AddNextState(DefaultDeadState);
                }

                return DamageReturnType.Killed;
            }

            return DamageReturnType.Damaging;
        }

        #region Hp
        public void ForceSetHp(double hp)
        {
            _currHp = Math.Min(hp, HP);
            UpdateHp();
        }

        private void FollowHpBar()
        {
            // hpBarView.CachedTr.position = GetCharacterView().CachedTr.position + new Vector3(0, GetCharacterView().Height);
        }

        private void UpdateHp()
        {
            _hpBarView.SetHpValue(_currHp, HP);
        }

        public void RefreshHp()
        {
            _hpBarView.SetHpValue(_currHp, HP);
        }
        #endregion

        private void IncreaseKillCount(CharacterController deadCharacter)
        {
            List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnKill);
            EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnKillLambda, deadCharacter);
        }

        /// <summary>
        /// 회복량 계산
        /// </summary>
        /// <param name="recoveryAmount"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public int PostCalculateHealAmount(int recoveryAmount, CharacterController target)
        {
            // 주는/받는 회복량 계수로 회복량 계산
            recoveryAmount = Mathf.RoundToInt(recoveryAmount * GivenHealRate * target.TakenHealRate);
            // 속성, 크기, 종족에 따른 회복량 계산이 필요하다면 여기서 할 것

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
        public bool GetHealed(int amount, CharacterController healer, int source, bool isFirstHeal = true)
        {
            // 죽어있으면 안준다.
            if (_currHp <= 0 || !IsAlive)
            {
                return false;
            }

            {
                List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseModifyHealAmount);
                amount = EffectCodeForLoopHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallModifyHealAmountLambda, amount);
            }
            {
                // effectCode에게 이벤트 전달
                List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnHealed);
                EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnHealedLambda, amount, isFirstHeal);
                _currHp += amount;
            }


            ShowHealText(amount).Forget();

            if (_currHp > HP)
            {
                _currHp = HP;
            }

            InGameStatistics.Instance.AddCombatHeal(healer, this, amount, _currHp, HP, source);

            UpdateHp();
            return true;
        }

        private async UniTask ShowDamageText(double amount, bool isCritical, bool isDoubleCritical)
        {
            InGameTextView textView = InGameTextViewPool.Instance.GetDamageTextView();
            await textView.ShowDamageText(GetCharacterView().CachedTr.position, GetCharacterView().Height, amount, isCritical, isDoubleCritical);
            InGameTextViewPool.Instance.ReturnDamageTextView(textView);
        }

        private async UniTask ShowHealText(double amount)
        {
            InGameTextView textView = InGameTextViewPool.Instance.GetDamageTextView();
            await textView.ShowHealText(GetCharacterView().CachedTr.position, GetCharacterView().Height, amount);
            InGameTextViewPool.Instance.ReturnDamageTextView(textView);
        }
    }
}
