using System;
using System.Collections.Generic;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.TeamBattle.BattleSystem
{
    public partial class CharacterController : IEffectCodeSource, IFollowable
    {
        public static Type defaultDeadState;

        private static int characUIdInc;
        private int characterUId;
        public int CharacterUId => characterUId;
        public int CharacterId => statData.CharacterId;

        private EffectCodeContainer ecc;

        public EffectCodeContainer GetEffectCodeContainer()
        {
            return ecc;
        }

        private ICharacterStatData statData;

        public ICharacterStatData GetCharacterStat()
        {
            return statData;
        }

        private ICharacterView view = null;

        public ICharacterView GetCharacterView()
        {
            return view;
        }

        public event Action<CharacterStateBase> OnStateChanged;

        public CharacterController target { get; set; }

        public bool IsAlive { get; set; }
        public bool IsForceIdle { get; set; }
        public bool IsBlockChangeState { get; set; }

        public bool IsBlackHole { get; set; }

        private Vector3 position;

        public bool isJoystickMove = false;

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

        private CharacterStateBase currState;
        private Queue<CharacterStateBase> nextStates = new ();

        private ObfuscatorFloat atkCoolTime;

        public float GetAttackCoolTime()
        {
            return atkCoolTime;
        }

        public void ResetAttackCoolTime()
        {
            atkCoolTime = 1f;
        }

        private Dictionary<BuffDebuffType, ObfuscatorInt> buffDebuffCountDict;
        private Dictionary<BuffDebuffType, InGameEffectBase> buffDebuffEffectViewDict;

        private double currHp;
        public double CurrentHp => currHp;

        private IHpBarView hpBarView;

        private AllianceType allianceType;
        public AllianceType AllianceType => allianceType;

        public void Initialize(ICharacterStatData statData, Vector3 position, AllianceType allianceType)
        {
            characterUId = characUIdInc++;
            this.statData = statData;
            position.z = position.y;
            this.position = position;
            this.allianceType = allianceType;

            view = CharacterViewPool.Instance.GetCharacterView(statData);
            hpBarView = HpBarViewPool.Instance.GetHpBar();
            hpBarView.Initialize(statData);
            FollowHpBar();

            view.OnAnimationEvent += OnAnimationEvent;
            view.CachedTr.localPosition = position;
            buffDebuffCountDict = new Dictionary<BuffDebuffType, ObfuscatorInt>();
            buffDebuffEffectViewDict = new Dictionary<BuffDebuffType, InGameEffectBase>();
            for (var i = BuffDebuffType.Meditation; i < BuffDebuffType.MAX; i++)
            {
                buffDebuffCountDict.Add(i, 0);
                buffDebuffEffectViewDict.Add(i, null);
            }

            // add EffectCodes
            ecc = new EffectCodeContainer(this);
            needUpdateFlag = EffectCodeInheritFlagExtensions.AllFlags();
            ecc.dirtyFlagEvent += EffectCodeDirtyFlagHandler;

            currHp = HP;
            IsAlive = true;
            IsForceIdle = false;
        }

        public void Clear()
        {
            target = null;
            ClearAllState();
            view.OnAnimationEvent -= OnAnimationEvent;
            ecc.Clear();
            CharacterViewPool.Instance.ReturnCharacterView(view);
            view = null;
            HpBarViewPool.Instance.ReturnHpBar(hpBarView);
            hpBarView = null;
        }

        private CrowdControlType crowdControlType = CrowdControlType.None;

        public bool NeedToBeIdle()
        {
            return HasCrowdControl(CrowdControlType.Airborne) || HasCrowdControl(CrowdControlType.KnockBack) || HasCrowdControl(CrowdControlType.Stun) || IsForceIdle;
        }

        public bool HasCrowdControl(CrowdControlType type)
        {
            return (crowdControlType & type) == type;
        }

        public void AddCrowdControl(CrowdControlType type)
        {
            crowdControlType = crowdControlType | type;
        }

        public void RemoveCrowdControl(CrowdControlType type)
        {
            crowdControlType = crowdControlType & ~type;
        }

        private void OnAnimationEvent(string animName, AnimationEventKey eventKey)
        {
            currState.AnimationEventCallback(animName, eventKey);
        }

        // Update is called once per frame
        public void ManagedUpdate(float dt)
        {
            if (currState == null)
            {
                if (nextStates.Count > 0)
                {
                    currState = nextStates.Dequeue();
                    currState.StateInit(this);
                    currState.StateStart();
                    OnStateChanged?.Invoke(currState);
                }

                return;
            }

            bool isSlowing = HasCrowdControl(CrowdControlType.Slowing);
            float modifiedSpeedRate = isSlowing ? Const.Instance.CrowdControlSlowRate : 1f;

            // 기본 공격 쿨타임을 컨틀롤러에서 가지고 있는다.
            // 스테이트에서 관리하면 적을 죽이고 난 뒤에 관리가 안된다.
            if (atkCoolTime > 0f)
            {
                atkCoolTime += -dt * AttackSpeed * modifiedSpeedRate;
            }

            CharacterStateRunningResult result = currState.CharacterStateRunning(dt * modifiedSpeedRate);

            if (result.HasFlag(CharacterStateRunningResult.CanCallMove))
            {
                view.UpdateTickAndPosition(dt * modifiedSpeedRate, position, viewPosition);
            }

            view.LookAt(FlipX);

            CalculateZ();
            if (result.HasFlag(CharacterStateRunningResult.CanCallEffectCodeOnUpdate))
            {
                List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnUpdate);
                EffectCodeHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnUpdateLambda, dt);
            }

            if (result.HasFlag(CharacterStateRunningResult.CanCallEffectCodeOnCooltime))
            {
                List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnCooltime);
                float skillCooltimeRate = InGameCalculator.Instance.CalculateCooltimeRate(SkillCooltimeRate);
                EffectCodeHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnCooltimeLambda, dt / skillCooltimeRate);
            }

            if (result.HasFlag(CharacterStateRunningResult.CanCallEffectCodeActivate))
            {
                List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseIsReadyToActivate);
                EffectCodeStatBase effectCode = EffectCodeHelper.ReturnFirst(effectCodes, EffectCodeCharacterLambda.CallIsReadyToActivateLambda);
                if (effectCode is EffectCodeCharacterBase runEffectCode)
                {
                    runEffectCode.Activate();
                }
            }

            // Regen HP
            RecoverHP(dt);

            if (nextStates.Count > 0)
            {
                if (IsBlockChangeState)
                {
                    return;
                }

                currState.StateEnd(false);
                StatePool.Instance.Push(currState);
                currState = nextStates.Dequeue();
                currState.StateInit(this);
                currState.StateStart();
                OnStateChanged?.Invoke(currState);
            }
        }

        public void LateUpdate(float dt)
        {
            FollowHpBar();
        }

        private void CalculateZ()
        {
            Vector3 pos = view.CachedTr.localPosition;
            pos.z = pos.y;
            view.CachedTr.localPosition = pos;
        }

        private ObfuscatorFloat recoveryHPElapsedTime;
        private void RecoverHP(float dt)
        {
            if (currHp <= 0)
            {
                return;
            }

            if (RecoveryHP <= 0)
            {
                return;
            }

            recoveryHPElapsedTime += dt;
            if (recoveryHPElapsedTime > Const.Instance.RegenHPPendingTime)
            {
                recoveryHPElapsedTime = 0;
                currHp += RecoveryHP;
                if (currHp > HP)
                {
                    currHp = HP;
                }

                UpdateHp();
            }
        }

        public StateBase AddNextState(Type stateType)
        {
#if UNITY_EDITOR
            // Debug.Log($"AddNextState >> {Time.frameCount}, {CharacId}, {CharacUId}, {stateType}");
#endif
            StateBase state = StatePool.Instance.GetState(stateType);
            if (state != null)
            {
                // Debug.Assert(nextStates.Count == 0, "Character Controller Already has Next State (count : " + nextStates.Count + ", first state name : " + nextStates.GetType().ToString() + "), Adding state : " + stateType.ToString());
                // Debug.Assert(state is CharacterStateBase, "Character Controller Adding state is not CharacterStateBase : " + stateType.ToString());
                nextStates.Enqueue(state as CharacterStateBase);
            }

            return state;
        }

        public T AddNextState<T>() where T : CharacterStateBase, new()
        {
#if UNITY_EDITOR
            // Debug.Log($"AddNextState >> {Time.frameCount}, {CharacId}, {CharacUId}, {typeof(T).ToString()}");
#endif
            var state = StatePool.Instance.GetState<T>();
            // Debug.Assert(nextStates.Count == 0, "Character Controller Already has Next State (count : " + nextStates.Count + ", first state name : " + nextStates.GetType().ToString() + "), Adding state : " + state.GetType().ToString());
            nextStates.Enqueue(state);
            return state;
        }

        public void ForceSetNextState<T>() where T : CharacterStateBase, new()
        {
            ClearAllState();
            var state = StatePool.Instance.GetState<T>();
            currState = state;
            currState.StateInit(this);
            currState.StateStart();
        }

        public CharacterStateBase GetCurrentState()
        {
            return currState;
        }

        public void ClearAllState()
        {
            if (currState != null)
            {
                currState.StateEnd(true);
                StatePool.Instance.Push(currState);
                while (nextStates.Count > 0)
                {
                    CharacterStateBase nextState = nextStates.Dequeue();
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
        public void AddBuffDebuffType(BuffDebuffType type, int otherEffect = 0)
        {
            if (IsAlive == false)
            {
                return;
            }

            if (otherEffect != 0)
            {
                var otherType = (BuffDebuffType) otherEffect;
                buffDebuffCountDict[otherType] += 1;

                if (buffDebuffEffectViewDict[otherType] == null)
                {
                    InGameEffectBase otherEffectView = InGameEffectFactory.Get((BuffDebuffType) otherEffect, GetCharacterView().CachedTr.transform);

                    otherEffectView.Initialize(Vector3.zero, false);

                    if (otherEffectView != null)
                    {
                        buffDebuffEffectViewDict[(BuffDebuffType) otherEffect] = otherEffectView;
                    }
                }
                else
                {
                    buffDebuffEffectViewDict[(BuffDebuffType) otherEffect].Restart();
                }
            }

            if (type == BuffDebuffType.None || type == BuffDebuffType.MAX)
            {
                return;
            }

            buffDebuffCountDict[type] += 1;

            if (buffDebuffEffectViewDict[type] == null)
            {
                InGameEffectBase effectView = InGameEffectFactory.Get(type, GetCharacterView().CachedTr.transform);

                if (effectView == null)
                {
                    return;
                }

                Vector2 vec = Vector2.zero;

                if (type == BuffDebuffType.Stun)
                {
                    vec = new Vector2(0, view.Height);
                }

                effectView.Initialize(vec, false);

                if (effectView != null)
                {
                    buffDebuffEffectViewDict[type] = effectView;
                }
            }
            else
            {
                buffDebuffEffectViewDict[type].Restart();
            }
        }

        public void RemoveBuffDebuffType(BuffDebuffType type)
        {
            if (type is BuffDebuffType.None or BuffDebuffType.MAX)
            {
                return;
            }

            buffDebuffCountDict[type] -= 1;
            if (buffDebuffCountDict[type] <= 0)
            {
                buffDebuffCountDict[type] = 0;
                if (buffDebuffEffectViewDict[type] != null)
                {
                    buffDebuffEffectViewDict[type].Remove();
                    buffDebuffEffectViewDict[type] = null;
                }
            }
        }

        public bool IsBuffDebuffType(BuffDebuffType type)
        {
            if (buffDebuffCountDict[type] > 0)
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
        /// <returns></returns>
        public DamageInfo PrecalculateDamageAmount(double ad, double ap, CharacterController target, int source, bool isSkill)
        {
            double damage = InGameCalculator.Instance.CalculateDefaultDamage(ad, ap, this, target);

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
            double minDamageAmount = Const.Instance.MinDamageRate * damageInfo.damageAmount;
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
            if (currHp <= 0 || view == null)
            {
                return DamageReturnType.AlreadyDead;
            }

            GetCharacterView().OnHit();
            ShowDamageText(damageInfo.damageAmount, damageInfo.isCritical, damageInfo.isDoubleCritical).Forget();

            currHp -= damageInfo.damageAmount;
            InGameStatistics.Instance.AddCombatDamage(attacker, this, damageInfo.damageAmount, currHp, damageInfo.source);

            UpdateHp();

            if (currHp <= 0)
            {
                currHp = 0;
                IsAlive = false;
                if (attacker != null)
                {
                    attacker.IncreaseKillCount(this);
                }

                var deathInfo = new DeathInfo {attacker = attacker};
                List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnDead);
                deathInfo = EffectCodeHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallOnDeadLambda, deathInfo);

                if (!deathInfo.isUseCustomState && defaultDeadState != null)
                {
                    AddNextState(defaultDeadState);
                }

                return DamageReturnType.Killed;
            }

            return DamageReturnType.Damaging;
        }

        #region Hp
        public void ForceSetHp(double hp)
        {
            currHp = Math.Min(hp, HP);
            UpdateHp();
        }

        private void FollowHpBar()
        {
            hpBarView.CachedTr.position = GetCharacterView().CachedTr.position + new Vector3(0, GetCharacterView().Height);
        }

        private void UpdateHp()
        {
            hpBarView.SetHpValue(currHp, HP);
        }

        public void RefreshHp()
        {
            hpBarView.SetHpValue(currHp, HP);
        }
        #endregion

        private void IncreaseKillCount(CharacterController deadCharacter)
        {
            List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnKill);
            EffectCodeHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnKillLambda, deadCharacter);
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
            if (currHp <= 0 || !IsAlive)
            {
                return false;
            }

            {
                List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseModifyHealAmount);
                amount = EffectCodeHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallModifyHealAmountLambda, amount);
            }
            {
                // effectCode에게 이벤트 전달
                List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnHealed);
                EffectCodeHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnHealedLambda, amount, isFirstHeal);
                currHp += amount;
            }


            ShowHealText(amount).Forget();

            if (currHp > HP)
            {
                currHp = HP;
            }

            InGameStatistics.Instance.AddCombatHeal(healer, this, amount, currHp, HP, source);

            UpdateHp();
            return true;
        }

        private async UniTask ShowDamageText(double amount, bool isCritical, bool isDoubleCritical)
        {
            ITextView textView = TextViewPool.Instance.GetDamageTextView();
            await textView.ShowDamageText(GetCharacterView().CachedTr.position, GetCharacterView().Height, amount, isCritical, isDoubleCritical);
            TextViewPool.Instance.ReturnDamageTextView(textView);
        }

        private async UniTask ShowHealText(double amount)
        {
            ITextView textView = TextViewPool.Instance.GetDamageTextView();
            await textView.ShowHealText(GetCharacterView().CachedTr.position, GetCharacterView().Height, amount);
            TextViewPool.Instance.ReturnDamageTextView(textView);
        }
    }
}
