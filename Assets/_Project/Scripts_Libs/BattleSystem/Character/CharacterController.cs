using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CookApps.TeamBattle.BattleSystem
{
    public partial class CharacterController : IEffectCodeSource, IFollowable
    {
        private static int characUIdInc;
        private int characUId;

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

        private IHpBar hpBar;

        private AllianceType allianceType;
        public AllianceType AllianceType => allianceType;

        public async UniTask Initialize(ICharacterStatData statData, Vector3 position, AllianceType allianceType)
        {
            characUId = characUIdInc++;
            this.statData = statData;
            position.z = position.y;
            this.position = position;
            this.allianceType = allianceType;

            hpBar = await HpBarPool.Instance.GetHpBar();
            view = await CharacterViewPool.Instance.GetCharacterView(statData);
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
            needUpdateFlag = EffectCodeInheritFlag.All;
            ecc.dirtyFlagEvent += EffectCodeDirtyFlagHandler;

            currHp = HP;
            IsAlive = true;
            IsForceIdle = false;
        }

        public void Clear()
        {
            target = null;
            view.OnAnimationEvent -= OnAnimationEvent;
            ecc.Clear();
            CharacterViewPool.Instance.ReturnCharacterView(view);
            view = null;
            HpBarPool.Instance.ReturnHpBar(hpBar);
            hpBar = null;
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
                float skillCooltimeRate = 1f - (Const.Instance.MaxCooltime * (1f - (1f / (1f + SkillCooltimeRate))));
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
        private ObfuscatorFloat? recoveryHPPendingTime;

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

            recoveryHPPendingTime ??= Const.Instance.RegenHPPendingTime;
            recoveryHPElapsedTime += dt;
            if (recoveryHPElapsedTime > recoveryHPPendingTime)
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

        public DamageInfo PrecalculateDamageAmount(double ad, CharacterController attacker, int source, bool isSkill)
        {
            double damage = ad;

            var damageInfo = new DamageInfo();

            if (isSkill)
            {
                damage *= attacker.SkillDamageRate;
            }

            damageInfo.isCritical = attacker.CriticalTest();
            if (damageInfo.isCritical)
            {
                damage *= attacker.CriticalDamageRate;
                damageInfo.isDoubleCritical = attacker.DoubleCriticalTest();
                if (damageInfo.isDoubleCritical)
                {
                    damage *= attacker.DoubleCriticalDamageRate;
                }
            }

            damageInfo.damageAmount = damage;
            damageInfo.source = source;
            return damageInfo;
        }

        public double PostCalculateDamageAmount(double damageAmount, CharacterController enemy = null)
        {
            double minDamageAmount = Const.Instance.MinDamageRate * damageAmount;
            damageAmount = damageAmount * AttackDamageRate * (enemy?.TakenDamageRate ?? 1f);
            damageAmount = Math.Max(minDamageAmount, damageAmount);
            damageAmount = Math.Floor(damageAmount);
            return damageAmount;
        }

        public DamageReturnType GetDamaged(DamageInfo damageInfo, CharacterController attacker, int hitId, bool isPure = true)
        {
            // 같은 틱에 데미지를 줘서 여러번 죽이는 경우가 있어서 이미 죽었는지 체크
            if (currHp <= 0 || view == null)
            {
                return DamageReturnType.AlreadyDead;
            }

            GetCharacterView().OnHit();
            ShowDamageText(damageInfo.damageAmount, damageInfo.isCritical, damageInfo.isDoubleCritical).Forget();

            currHp -= damageInfo.damageAmount;

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
            hpBar.CachedTr.position = GetCharacterView().CachedTr.position + new Vector3(0, GetCharacterView().Height);
        }

        private void UpdateHp()
        {
            hpBar.SetHpValue(currHp, HP);
        }

        public void RefreshHp()
        {
            hpBar.SetHpValue(currHp, HP);
        }
        #endregion

        private void IncreaseKillCount(CharacterController deadCharacter)
        {
            List<EffectCodeStatBase> effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnKill);
            EffectCodeHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnKillLambda, deadCharacter);
        }

        public int PostCalculateHealAmount(int recoveryAmount, CharacterController friend)
        {
            // 속성, 크기, 종족, 힐량증감 stat 계산
            recoveryAmount = Mathf.RoundToInt(recoveryAmount * GivenHealRate * friend.TakenHealRate);
            return recoveryAmount;
        }

        public bool GetHealed(int amount, CharacterController friend, int source, bool isPure = true)
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
                EffectCodeHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnHealedLambda, amount, isPure);
                currHp += amount;
            }

            ShowHealText(amount).Forget();

            if (currHp > HP)
            {
                currHp = HP;
            }

            UpdateHp();
            return true;
        }

        private async UniTask ShowDamageText(double amount, bool isCritical, bool isDoubleCritical)
        {
            ITextView textView = await TextViewPool.Instance.GetDamageTextView();
            await textView.ShowDamageText(GetCharacterView().CachedTr.position, GetCharacterView().Height, amount, isCritical, isDoubleCritical);
            TextViewPool.Instance.ReturnDamageTextView(textView);
        }

        private async UniTask ShowHealText(double amount)
        {
            ITextView textView = await TextViewPool.Instance.GetDamageTextView();
            await textView.ShowHealText(GetCharacterView().CachedTr.position, GetCharacterView().Height, amount);
            TextViewPool.Instance.ReturnDamageTextView(textView);
        }
    }
}
