using System;
using CookApps.Obfuscator;
using CookApps.AutoBattler;
using JetBrains.Annotations;

namespace CookApps.BattleSystem
{
    public abstract class EffectCodeCharacterBase : EffectCodeStatBase
    {
        public override EffectCodeType Type => EffectCodeType.Character;
        protected CharacterController owner;

        protected ObfuscatorInt SkillIndex = -1;
        protected ObfuscatorFloat CoolTimeDurationTime;
        protected ObfuscatorFloat CoolTimeElapsedTime;
        public void SetElapsedTime(float elapsedTime) => CoolTimeElapsedTime = elapsedTime;
        public float GetElapsedTime() => CoolTimeElapsedTime;
        public float GetDurationTime() => CoolTimeDurationTime;
        public bool IsSkillActivated;
        public bool IsExecute;

        public override int CalcOrder => 1;

        public virtual bool ForceReadyToActivate()
        {
            return false;
        }

        public string GetSoundFxName()
        {
            string codeIdStr = codeInfo.CodeId.ToString();
            string modifiedCodeIdStr = codeIdStr.Length > 1 ? codeIdStr.Substring(1) : "0";
            return "snd_sfx_skill_" + modifiedCodeIdStr;
        }

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            owner = container.Owner as CharacterController;
            waitUpdateElapsedTime = 0f;
            waitCooltimeElapsedTime = 0f;
            updatePendingTime = InGameCalculator.EffectCodeUpdatePendingTime;
            cooltimePendingTime = InGameCalculator.EffectCodeCooltimePendingTime;
        }

        public override void OnPreRemoved()
        {
            base.OnPreRemoved();
            SkillIndex = -1;
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
        /// <summary>
        /// updatePendingTime에 의해 지연된 시간이 dt로 들어온다.
        /// updatePendingTime은 InGameCalculator.Instance.EffectCodeUpdatePendingTime로 설정된다.
        /// updatePendingTime이 0이면 매 틱마다 호출 된다.
        /// </summary>
        /// <param name="dt"></param>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnUpdate)]
        public virtual void OnUpdate(float dt)
        {
        }

        /// <summary>
        /// cooltimePendingTime에 의해 지연된 시간이 dt로 들어온다.
        /// cooltimePendingTime은 InGameCalculator.Instance.EffectCodeCooltimePendingTime로 설정된다.
        /// cooltimePendingTime이 0이면 매 틱마다 호출 된다.
        /// </summary>
        /// <param name="dt"></param>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnCooltime)]
        public virtual void OnCooltime(float dt)
        {
        }

        public virtual (int skillIndex, float elapsedTime, float coolTime) GetCoolTimeData()
        {
            return (SkillIndex, CoolTimeElapsedTime, CoolTimeDurationTime);
        }

        /// <summary>
        /// 전투 시작 시 호출 된다.
        /// </summary>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnCombatStart)]
        public virtual void OnCombatStart()
        {
        }

        /// <summary>
        /// 공격 시마다 호출 된다.
        /// </summary>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnAttack)]
        public virtual void OnAttack()
        {
        }

        /// <summary>
        /// 스킬 사용 시마다 호출 된다.
        /// </summary>
        /// <param name="skillEffectCode">사용된 스킬의 이펙트코드</param>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnSkill)]
        public virtual void OnSkill(EffectCodeBase skillEffectCode)
        {
        }

        /// <summary>
        /// 킬수가 오를 때마다 호출 된다.
        /// </summary>
        /// <param name="deadCharacter"></param>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnKill)]
        public virtual void OnKill(CharacterController deadCharacter)
        {
        }

        /// <summary>
        /// 힐을 받을 때마다 호출 된다.
        /// </summary>
        /// <param name="healAmount"></param>
        /// <param name="isPure"></param>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnHealed)]
        public virtual void OnHealed(double healAmount, bool isPure)
        {
        }

        /// <summary>
        /// 대미지를 받을 때마다 호출 된다.
        /// 특수하게 대미지를 감소시키는 경우에만 여기서 감소된 대미지를 리턴한다.
        /// 일반적으로 피해량 감소 기능을 사용해야한다.
        /// 특수케이스 예시.
        /// 주변 아군이 받는 피해의 {0}%를 대신 받는 스킬의 경우
        /// 주변 아군에게 방어력이나 피해량 감소 기능을 넣어주는 것 보다 이 함수를 통해 피해량을 감소시키고 감소된 피해량을 본인에게 주자.
        /// </summary>
        /// <param name="damageInfo"></param>
        /// <param name="attacker">nullable</param>
        /// <param name="isPure"></param>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnDamaged)]
        public virtual CharacterController.DamageInfo OnDamaged(CharacterController.DamageInfo damageInfo, [CanBeNull] CharacterController attacker, bool isPure)
        {
            return damageInfo;
        }

        /// <summary>
        /// 죽을 때 호출 된다.
        /// </summary>
        /// <param name="deathInfo"></param>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnDead)]
        public virtual DeathInfo OnDead(DeathInfo deathInfo)
        {
            return deathInfo;
        }

        /// <summary>
        /// 크리티컬 터질때 호출 된다.
        /// </summary>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnCritical)]
        public virtual void OnCritical(CharacterController target)
        {
        }
        #endregion

        /// <summary>
        /// 대미지량을 수정할 때 호출 된다.
        /// 특수한 케이스에서만 사용할 것.
        /// 특수케이스 예시.
        /// 캐릭터들의 공격력과 체력이 몬스터와 전투하는 벨런스로 개발되었는데,
        /// pvp로 전환할 경우 체력이 너무 낮아 전투가 너무 빨리 끝나거나
        /// 공격력이 너무 낮아 전투가 안끝나는 경우가 있는데
        /// 이 때 이 기능을 사용한 이펙트코드를 pvp전투시 모든 캐릭터한테 넣어주어 벨런스를 조정할 수 있다.
        /// </summary>
        /// <param name="damageAmount"></param>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseModifyDamageAmount)]
        public virtual double ModifyDamageAmount(double damageAmount)
        {
            return damageAmount;
        }

        /// <summary>
        /// 힐량을 수정할 때 호출 된다.
        /// <see cref="ModifyDamageAmount"/> 와 마찬가지로 특수한 케이스에서만 사용할 것.
        /// </summary>
        /// <param name="healAmount"></param>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseModifyHealAmount)]
        public virtual double ModifyHealAmount(double healAmount)
        {
            return healAmount;
        }

        /// <summary>
        /// 쉴드량을 수정할 때 호출 된다.
        /// <see cref="ModifyDamageAmount"/> 와 마찬가지로 특수한 케이스에서만 사용할 것.
        /// </summary>
        /// <param name="shieldAmount"></param>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseModifyShieldAmount)]
        public virtual double ModifyShieldAmount(double shieldAmount)
        {
            return shieldAmount;
        }

        /// <summary>
        /// 쿨타임 추가 함수
        /// 컨트롤러에서 한꺼번에 모두 쿨타임을 업데이트 하므로 조심히 오버라이딩 해주세요.
        /// </summary>
        /// <param name="cooltime"></param>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseAddSkillCooltime)]
        public virtual float AddSkillCooltime(float cooltime)
        {
            return cooltime;
        }

        /// <summary>
        /// 데미지 계산 시 스킵할 테스트 플래그를 반환
        /// 각 이펙트 코드에서 오버라이드하여 자신의 플래그를 반환
        /// </summary>
        /// <returns>스킵할 테스트 플래그</returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseModifyDamageTestFlags)]
        public virtual CharacterController.DamageTestFlags GetDamageTestFlags()
        {
            return CharacterController.DamageTestFlags.None;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnModifyAvoidRateAmount)]
        public virtual float ModifyAvoidRateAmount(float avoidRateAmount)
        {
            return avoidRateAmount;
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
        /// 캐릭터 애니메이션을 실행시킬 경우 <code>owner.AddNextState<CharacterStateSkill>(skillEffectCode)</code>를 호출하면 된다.
        /// </summary>
        public virtual void Activate()
        {
            if (SoundManager.Instance.IsPlayingGacha) return;   // 가챠 실행중에는 사운드 off

            SoundManager.Instance.PlaySFX(GetSoundFxName());
        }
        #endregion

        #region Passive 기능들
        /// <summary>
        /// 일반 공격을 변경할 때 사용한다.
        /// 아직 개발이 다 되지 않음. 사용하려할 경우 개발이 필요함.
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseIsUseNormalAttack)]
        public virtual bool IsUseNormalAttack()
        {
            return false;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnAttackEnd)]
        public virtual void OnAttackEnd(CharacterController target)
        {
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
            IsExecute = true;
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

        /// <summary>
        /// 스킬 애니메이션이 끝나고 호출됨.
        /// </summary>
        public virtual void OnSkillAnimationEnd()
        {
            // 이펙트 코드에게 스킬 사용 전달
            var characEffectCodes = owner.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnSkill);
            EffectCodeForLoopHelper.CallWithArgs(characEffectCodes, EffectCodeCharacterLambda.CallOnSkillLambda, this);
        }

        public virtual void OnSkillCanceled()
        {
            Debug.LogColor("[TEST] SkillCanceled", "red");
            IsSkillActivated = false;
            if (!IsExecute)
            {
                SetElapsedTime(GetDurationTime() - 0.5f);
            }
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
        public static Action<EffectCodeStatBase, CharacterController> CallOnAttackEndLambda = (x, target) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnAttackEnd(target);
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

        public static Func<EffectCodeStatBase, double, double> CallModifyDamageAmountLambda = (x, damageAmount) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.ModifyDamageAmount(damageAmount);
            }

            return damageAmount;
        };

        public static Func<EffectCodeStatBase, double, double> CallModifyHealAmountLambda = (x, healAmount) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.ModifyHealAmount(healAmount);
            }

            return healAmount;
        };

        public static Func<EffectCodeStatBase, double, double> CallModifyShieldAmountLambda = (x, shieldAmount) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.ModifyShieldAmount(shieldAmount);
            }

            return shieldAmount;
        };

        public static Func<EffectCodeStatBase, float, float> CallOnModifyAvoidRateAmountLambda = (x, avoidRateAmount) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.ModifyAvoidRateAmount(avoidRateAmount);
            }

            return avoidRateAmount;
        };


        public static Action<EffectCodeStatBase, double, bool> CallOnHealedLambda = (x, healAmount, isPure) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnHealed(healAmount, isPure);
            }
        };

        public static Func<EffectCodeStatBase, CharacterController.DamageInfo, CharacterController, bool, CharacterController.DamageInfo> CallOnDamagedLambda = (x, damageInfo, attacker, isFirstDamage) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.OnDamaged(damageInfo, attacker, isFirstDamage);
            }

            return damageInfo;
        };

        public static Func<EffectCodeStatBase, DeathInfo, DeathInfo> CallOnDeadLambda = (x, deathInfo) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                return code.OnDead(deathInfo);
            }

            return deathInfo;
        };

        public static Action<EffectCodeStatBase, CharacterController> CallOnCriticalLambda = (x, target) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.OnCritical(target);
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

        public static Action<EffectCodeStatBase, float> CallAddSkillCooltimeLambda = (x, cooltime) =>
        {
            if (x is EffectCodeCharacterBase code)
            {
                code.AddSkillCooltime(cooltime);
            }

        };
    }
}
