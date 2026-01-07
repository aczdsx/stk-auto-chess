using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
namespace CookApps.BattleSystem
{    /// <summary>
     /// 평타 공격시 아군중 체력이 가장 낮은 캐릭터 대상 공격력 비례 회복 적용.
     /// 일단 자힐도 함께해야함.
     /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeJobPassiveRecovery : EffectCodeCharacterBase
    {
        public const int CodeId = (int)EffectCodeNameType.JOBS_RECOVERY;
        private float _recoveryPercentage = 0f;//회복비율
        private const InGameVfxNameType _recoveryVfxEnum = InGameVfxNameType.fx_common_buff_heal;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);

            _recoveryPercentage = codeInfo.GetCodeStatToFloat(1);

            owner.SetStateType(typeof(CharacterStateAttack), typeof(CharacterStateAttackHealer));
            owner.SetStateType(typeof(CharacterStateIdle), typeof(CharacterStateIdleHealer));
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _recoveryPercentage = codeInfo.GetCodeStatToFloat(1);
        }

        public override void OnAttackEnd(CharacterController target)
        {
            base.OnAttackEnd(target);
            if (target.AllianceType == owner.AllianceType)
            {
                //target에게 공격력 비례 회복적용

                double attackPower = owner.SpecCharacter.atk_type == AtkType.AD ? owner.AD : owner.AP;

                double recoveryAmount = CalculateOracleNormalAttackRecoveryAmount(target);

                var effectCodes = owner.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseModifyHealAmount);
                recoveryAmount = EffectCodeForLoopHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallModifyHealAmountLambda, recoveryAmount);

                 target.GetHealed(recoveryAmount, owner, codeId, true);

                InGameVfxManager.Instance.AddInGameVfx(_recoveryVfxEnum, target.SkillRootTransformFollowable);
            }
            else
            {
                var stateAttack = owner.GetCurrentState() as CharacterStateAttack;
                var damageInfo = stateAttack.CalculateNormalAttackDamage();
                target.GetDamaged(damageInfo, owner);
            }
        }
        public override void OnPreRemoved()
        {
            owner.RemoveStateType(typeof(CharacterStateAttack));
            owner.RemoveStateType(typeof(CharacterStateIdle));

            base.OnPreRemoved();
        }

        private double CalculateOracleNormalAttackRecoveryAmount(CharacterController target)
        {
            var attackPower = owner.SpecCharacter.atk_type == AtkType.AD ? owner.AD : owner.AP;
            var BaseHealOnHit = attackPower * _recoveryPercentage;

            var HealPowerCaster = owner.SpecCharacter.heal_power;
            var HealPowerTarget = target.SpecCharacter.heal_power;

            float HealTakenMul = 1f;

            var FinalHealOnHit = BaseHealOnHit * (1 + HealPowerCaster) * (1 + HealPowerTarget) * HealTakenMul;

            return Math.Round(FinalHealOnHit);
        }

        public static double CalculateOracleDefaultSkillRecoveryAmount(CharacterController owner, float healRate)
        {
            double attackpower = owner.SpecCharacter.atk_type == AtkType.AD ? owner.AD : owner.AP;

            double flatHeal = 0d;

                // 즉시 힐량 계산 (PostCalculateHealAmount에서 오라클 처리)
            return attackpower * healRate + flatHeal;
        }

        public static double CalculateOracleSkillRecoveryAmount(CharacterController owner, CharacterController target,
        double baseHealSkill)
        {
            var HealPowerCaster = owner.SpecCharacter.heal_power;
            var HealPowerTarget = target.SpecCharacter.heal_power;

            var AntiHealMul = 1f;

            var FinalHealSkill = baseHealSkill * (1 + HealPowerCaster) * (1 + HealPowerTarget) * AntiHealMul;

            return Math.Round(FinalHealSkill);
        }


    }
}
