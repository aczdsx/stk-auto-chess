using System;
using CookApps.Obfuscator;

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
            updatePendingTime = InGameCalculator.Instance.EffectCodeUpdatePendingTime;
            cooltimePendingTime = InGameCalculator.Instance.EffectCodeCooltimePendingTime;
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

        // 스킬 사용 시마다 호출 된다.
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

        // 대미지를 받을 때마다 호출 된다. 특수하게 대미지를 감소시키는 경우에만 여기서 감소된 대미지를 리턴한다.(예. 3067)
        // 보통은 대미지 감소 버프로 계산하자.
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
        /// <summary>
        /// 액티브 스킬의 경우 스킬 발동 가능할 때 True를 리턴하면 된다.
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseIsReadyToActivate)]
        public virtual bool IsReadyToActivate()
        {
            return false;
        }

        /// <summary>
        /// 캐릭터가 이 스킬을 발동 가능한 경우에 호출된다.
        /// 실제로 스킬을 발동안시켜도 되지만, 발동시키려면 여기서 발동시키면 된다.
        /// 캐릭터 애니메이션을 실행시킬 경우 owner.AddNextState<CharacterStateSkill>(skillEffectCode)를 호출하면 된다.
        /// </summary>
        public virtual void Activate()
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
        /// <summary>
        /// 스킬의 애니메이션 이벤트에 따라 호출됨.
        /// 애니메이션키가 Execute1Per1 이면 인자로 (0, 1)로 1회 호출되고,
        /// 애니메이션키가 Execute1Per2 이면 인자로 (0, 2)로 1회 (1, 2)로 1회 총 2회 호출될 것으로 의도하였음.
        /// 예로 특정 캐릭터의 애니메이션이 칼을 3번 휘둘러서 300 만큼의 대미지를 주는 스킬이라면
        /// 애니메이션에 Execute1Per3 이벤트 키를 3곳에 찍어두고 인덱스에 맞게 300의 대미지를 나눠서 주는 것으로 구현하면 됨.
        /// </summary>
        /// <param name="executeIndex"></param>
        /// <param name="totalLength"></param>
        public virtual void OnSkillExecute(int executeIndex, int totalLength)
        {
        }

        /// <summary>
        /// 스킬 애니메이션 중 특정 이펙트를 발동시켜야할 때
        /// 애니메이션에 이벤트키를 VFX1, VFX2, VFX3, VFX4, VFX5로 찍어두고
        /// 이 함수를 오버라이드하여 특정 이펙트를 발동시키면 됨.
        /// </summary>
        /// <param name="effectIndex"></param>
        public virtual void OnSkillVFX(int effectIndex)
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
        public static Action<EffectCodeStatBase, float> CallOnUpdateLambda = (x, dt) =>
        {
            if (x is EffectCodeCharacterBase code && code.WaitForUpdate(dt))
            {
                code.OnUpdate(code.waitUpdateElapsedTime);
                code.waitUpdateElapsedTime = 0;
            }
        };

        public static Action<EffectCodeStatBase, float> CallOnCooltimeLambda = (x, dt) =>
        {
            if (x is EffectCodeCharacterBase code && code.WaitForCooltime(dt))
            {
                code.OnCooltime(code.waitCooltimeElapsedTime);
                code.waitCooltimeElapsedTime = 0;
            }
        };

        public static Action<EffectCodeStatBase> CallOnCombatStartLambda = x =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnCombatStart();
            }
        };

        public static Action<EffectCodeStatBase> CallOnAttackLambda = x =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnAttack();
            }
        };

        public static Action<EffectCodeStatBase, EffectCodeBase> CallOnSkillLambda = (x, skillEffectCode) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnSkill(skillEffectCode);
            }
        };

        public static Action<EffectCodeStatBase, CharacterController> CallOnKillLambda = (x, deadCharacter) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnKill(deadCharacter);
            }
        };

        public static Func<EffectCodeStatBase, int, int> CallModifyDamageAmountLambda = (x, damageAmount) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.ModifyDamageAmount(damageAmount);
            }

            return damageAmount;
        };

        public static Func<EffectCodeStatBase, int, int> CallModifyHealAmountLambda = (x, healAmount) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.ModifyHealAmount(healAmount);
            }

            return healAmount;
        };

        public static Func<EffectCodeStatBase, int, int> CallModifyShieldAmountLambda = (x, shieldAmount) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.ModifyShieldAmount(shieldAmount);
            }

            return shieldAmount;
        };

        public static Action<EffectCodeStatBase, int, bool> CallOnHealedLambda = (x, healAmount, isPure) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnHealed(healAmount, isPure);
            }
        };

        public static Func<EffectCodeStatBase, int, CharacterController, bool, int> CallOnDamagedLambda = (x, damageAmount, attacker, isPure) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.OnDamaged(damageAmount, attacker, isPure);
            }

            return damageAmount;
        };

        public static Func<EffectCodeStatBase, DeathInfo, DeathInfo> CallOnDeadLambda = (x, deathInfo) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.OnDead(deathInfo);
            }

            return deathInfo;
        };

        public static Action<EffectCodeStatBase> CallOnCriticalLambda = x =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnCritical();
            }
        };

        public static Func<EffectCodeStatBase, bool> CallIsReadyToActivateLambda = x =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.IsReadyToActivate();
            }

            return false;
        };

        public static Func<EffectCodeStatBase, bool> CallIsUseNormalAttackLambda = x =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.IsUseNormalAttack();
            }

            return false;
        };
    }
}
