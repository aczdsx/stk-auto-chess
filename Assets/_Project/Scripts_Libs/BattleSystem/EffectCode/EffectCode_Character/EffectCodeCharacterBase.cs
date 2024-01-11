using System;
using System.Collections.Generic;
using CookApps.Obfuscator;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Pool;

namespace CookApps.TeamBattle.BattleSystem
{
    public abstract class EffectCodeCharacterBase : EffectCodeStatBase
    {
        public override EffectCodeType Type => EffectCodeType.Character;
        protected CharacterController owner;

        public override int CalcOrder => 1;

        public virtual bool ForceReadyToActivate()
        {
            return false;
        }

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            owner = container.Owner as CharacterController;
            waitUpdateElapsedTime = 0f;
            waitCooltimeElapsedTime = 0f;
            updatePendingTime = ConstDataSource.Instance.EffectCodeUpdatePendingTime;
            cooltimePendingTime = ConstDataSource.Instance.EffectCodeCooltimePendingTime;
        }

        public override void OnPreRemoved()
        {
            base.OnPreRemoved();
            owner = null;
        }

        protected internal ObfuscatorFloat waitUpdateElapsedTime;
        protected internal ObfuscatorFloat updatePendingTime;

        internal bool WaitForUpdate(float dt)
        {
            waitUpdateElapsedTime += dt;
            return waitUpdateElapsedTime > updatePendingTime;
        }

        protected internal ObfuscatorFloat waitCooltimeElapsedTime;
        protected internal ObfuscatorFloat cooltimePendingTime;

        internal bool WaitForCooltime(float dt)
        {
            waitCooltimeElapsedTime += dt;
            return waitCooltimeElapsedTime > cooltimePendingTime;
        }

        #region 발동 조건 이벤트들
        /// 매 틱마다 호출 된다.
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnUpdate)]
        public virtual void OnUpdate(float dt)
        {
        }

        /// 쿨타임을 업데이트 해준다.
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnCooltime)]
        public virtual void OnCooltime(float dt)
        {
        }

        public virtual float OnCoolRemainTime()
        {
            return 0;
        }

        // 전투 시작 시 호출 된다.
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnCombatStart)]
        public virtual void OnCombatStart()
        {
        }

        // 공격 시마다 호출 된다.
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnAttack)]
        public virtual void OnAttack()
        {
        }

        // 공격 시마다 호출 된다.
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnSkill)]
        public virtual void OnSkill(EffectCodeBase skillEffectCode)
        {
        }

        // 킬수가 오를 때마다 호출 된다.
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnKill)]
        public virtual void OnKill(CharacterController deadCharacter)
        {
        }

        // 힐을 받을 때마다 호출 된다.
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnHealed)]
        public virtual void OnHealed(int healAmount, bool isPure)
        {
        }

        // 데미지를 받을 때마다 호출 된다. 특수하게 데미지를 감소시키는 경우에만 여기서 감소된 데미지를 리턴한다.(예. 3067)
        // 보통은 데미지 감소 버프로 계산하자.
        // attacker는 nullable
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnDamaged)]
        public virtual int OnDamaged(int damageAmount, CharacterController attacker, bool isPure)
        {
            return damageAmount;
        }

        // 죽을 때 호출 된다.
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnDead)]
        public virtual DeathInfo OnDead(DeathInfo deathInfo)
        {
            return deathInfo;
        }

        // 크리티컬 터질때 호출 된다.
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnCritical)]
        public virtual void OnCritical()
        {
        }
        #endregion

        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseModifyDamageAmount)]
        public virtual int ModifyDamageAmount(int damageAmount)
        {
            return damageAmount;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseModifyHealAmount)]
        public virtual int ModifyHealAmount(int healAmount)
        {
            return healAmount;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseModifyShieldAmount)]
        public virtual int ModifyShieldAmount(int shieldAmount)
        {
            return shieldAmount;
        }

        #region Active 기능들
        /// 발동할 수 있는지
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseIsReadyToActivate)]
        public virtual bool IsReadyToActivate()
        {
            return false;
        }

        /// 발동시키자
        public virtual void Activate(bool isClick = false)
        {
        }
        #endregion

        #region Passive 기능들
        // 일반 공격을 변경할 때 사용
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseIsUseNormalAttack)]
        public virtual bool IsUseNormalAttack()
        {
            return false;
        }
        #endregion

        #region Skill Animation 또는 State 관련
        public virtual void OnSkillExecute(int executeIndex, int totalLength)
        {
        }

        public virtual void OnSkillAnimationEnd()
        {
        }

        public virtual void OnSkillCanceled()
        {
        }
        #endregion
    }

    public static class EffectCodeCharacterLambda
    {
        public static Action<EffectCodeStatBase, float> CallOnUpdateLambda = new (CallOnUpdate);

        private static void CallOnUpdate(EffectCodeStatBase x, float dt)
        {
            if (x is EffectCodeCharacterBase code && code.WaitForUpdate(dt))
            {
                code.OnUpdate(code.waitUpdateElapsedTime);
                code.waitUpdateElapsedTime = 0;
            }
        }

        public static Action<EffectCodeStatBase, float> CallOnCooltimeLambda = new (CallOnCooltime);

        private static void CallOnCooltime(EffectCodeStatBase x, float dt)
        {
            if (x is EffectCodeCharacterBase code && code.WaitForCooltime(dt))
            {
                code.OnCooltime(code.waitCooltimeElapsedTime);
                code.waitCooltimeElapsedTime = 0;
            }
        }

        public static Action<EffectCodeStatBase> CallOnCombatStartLambda = new (CallOnCombatStart);

        private static void CallOnCombatStart(EffectCodeStatBase x)
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnCombatStart();
            }
        }

        public static Action<EffectCodeStatBase> CallOnAttackLambda = new (CallOnAttack);

        private static void CallOnAttack(EffectCodeStatBase x)
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnAttack();
            }
        }

        public static Action<EffectCodeStatBase, EffectCodeBase> CallOnSkillLambda = new (CallOnSkill);

        private static void CallOnSkill(EffectCodeStatBase x, EffectCodeBase skillEffectCode)
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnSkill(skillEffectCode);
            }
        }

        public static Action<EffectCodeStatBase, CharacterController> CallOnKillLambda = new (CallOnKill);

        private static void CallOnKill(EffectCodeStatBase x, CharacterController deadCharacter)
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnKill(deadCharacter);
            }
        }

        public static Func<EffectCodeStatBase, int, int> CallModifyDamageAmountLambda = new (CallModifyDamageAmount);

        private static int CallModifyDamageAmount(EffectCodeStatBase x, int damageAmount)
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.ModifyDamageAmount(damageAmount);
            }

            return damageAmount;
        }

        public static Func<EffectCodeStatBase, int, int> CallModifyHealAmountLambda = new (CallModifyHealAmount);

        private static int CallModifyHealAmount(EffectCodeStatBase x, int healAmount)
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.ModifyHealAmount(healAmount);
            }

            return healAmount;
        }

        public static Func<EffectCodeStatBase, int, int> CallModifyShieldAmountLambda = new (CallModifyShieldAmount);

        private static int CallModifyShieldAmount(EffectCodeStatBase x, int shieldAmount)
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.ModifyShieldAmount(shieldAmount);
            }

            return shieldAmount;
        }

        public static Action<EffectCodeStatBase, int, bool> CallOnHealedLambda = new (CallOnHealed);

        private static void CallOnHealed(EffectCodeStatBase x, int healAmount, bool isPure)
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnHealed(healAmount, isPure);
            }
        }

        public static Func<EffectCodeStatBase, int, CharacterController, bool, int> CallOnDamagedLambda = new (CallOnDamaged);

        private static int CallOnDamaged(EffectCodeStatBase x, int damageAmount, CharacterController attacker, bool isPure)
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.OnDamaged(damageAmount, attacker, isPure);
            }

            return damageAmount;
        }

        public static Func<EffectCodeStatBase, DeathInfo, DeathInfo> CallOnDeadLambda = new (CallOnDead);

        private static DeathInfo CallOnDead(EffectCodeStatBase x, DeathInfo deathInfo)
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.OnDead(deathInfo);
            }

            return deathInfo;
        }

        public static Action<EffectCodeStatBase> CallOnCriticalLambda = new (CallOnCritical);

        private static void CallOnCritical(EffectCodeStatBase x)
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnCritical();
            }
        }

        public static Func<EffectCodeStatBase, bool> CallIsReadyToActivateLambda = new (CallIsReadyToActivate);

        private static bool CallIsReadyToActivate(EffectCodeStatBase x)
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.IsReadyToActivate();
            }

            return false;
        }

        public static Func<EffectCodeStatBase, bool> CallIsUseNormalAttackLambda = new (CallIsUseNormalAttack);

        private static bool CallIsUseNormalAttack(EffectCodeStatBase x)
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.IsUseNormalAttack();
            }

            return false;
        }
    }
}
