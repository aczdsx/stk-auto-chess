using System.Collections.Generic;
using CookApps.TeamBattle.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.TeamBattle.BattleSystem.CharacterController;

public class CharacterStateAttack : CharacterStateBase
{
    protected bool isAttackAnimRunning;

    public override void StateStart()
    {
        base.StateStart();
        isAttackAnimRunning = false;
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.Idle);
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        if (isAttackAnimRunning)
        {
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        if (characCtrl.NeedToBeIdle())
        {
            ReturnToIdle();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        // 1. 잡는 적이 아직 살아있는지 체크
        CharacterController atkTarget = characCtrl.target;
        if (atkTarget == null || !atkTarget.IsAlive)
        {
            isAttackAnimRunning = false;
            ReturnToIdle();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        float range = characCtrl.AttackRange;
        Vector2 diff = characCtrl.target.Position - characCtrl.Position;
        float resultRange = range * range;

        characCtrl.FlipX = diff.x > 0;

        if (characCtrl.GetAttackCoolTime() <= 0f)
        {
            characCtrl.ResetAttackCoolTime();

            // 이펙트 코드에게 공격 횟수 전달
            List<EffectCodeStatBase> characEffectCodes = characCtrl.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnAttack);
            EffectCodeHelper.Call(characEffectCodes, EffectCodeCharacterLambda.CallOnAttackLambda);

            RunAttackAnimation();
            isAttackAnimRunning = true;
        }

        return isAttackAnimRunning ? CharacterStateRunningResult.CanCallAllWithoutMove : CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
    }

    protected virtual void RunAttackAnimation()
    {
        AnimationClip clip = characCtrl.GetCharacterView().PlayAnimation(AnimationKey.Attack);
        // 공격 애니메이션 타임 스케일 계산 방법 : 기본 공격 시간 (atkTime : 1/atkSpeed)이 공격 애니메이션 시간의 1.5배보다 느리면
        // 공격 애니메이션 타임에 공속을 곱함. 아님 1f
        float animTime = clip.length * 1.5f;
        float atkTime = 1f / characCtrl.AttackSpeed;
        if (animTime > atkTime)
        {
            characCtrl.GetCharacterView().SetAnimationSpeed(animTime * characCtrl.AttackSpeed);
        }
        else
        {
            characCtrl.GetCharacterView().SetAnimationSpeed(1f);
        }
    }

    public override void AnimationEventCallback(string animName, AnimationEventKey eventKey)
    {
        if (animName != AnimationKey.Attack.ToAnimationName())
        {
            return;
        }

        if (eventKey == AnimationEventKey.End)
        {
            characCtrl.GetCharacterView().SetAnimationSpeed(1f);
            isAttackAnimRunning = false;
            return;
        }

        if (characCtrl.target == null)
        {
            return;
        }

        if (AnimationEventKey.ActivateStart < eventKey && eventKey < AnimationEventKey.ActivateEnd)
        {
            int hitCount = eventKey - AnimationEventKey.ActivateStart;

            // damage 계산
            CharacterController.DamageInfo damageInfo = characCtrl.target.PrecalculateDamageAmountWithoutAP(characCtrl.AD, characCtrl, 0, false);
            damageInfo.damageAmount = characCtrl.PostCalculateDamageAmount(damageInfo.damageAmount, characCtrl.target) / hitCount;
            var hitId = 0;
            if (characCtrl.GetCharacterStat().AttackType == AttackType.Projectile)
            {
                if (characCtrl == null || characCtrl.IsAlive == false)
                {
                    return;
                }

                // TODO: throw projectile Effect
                characCtrl.target.GetDamaged(damageInfo, characCtrl);
                return;
            }
            else
            {
                // TODO: Effect
                characCtrl.target.GetDamaged(damageInfo, characCtrl);
            }
        }
    }

    protected virtual void ReturnToIdle()
    {
        characCtrl.target = null;
        characCtrl.AddNextState<CharacterStateIdle>();
    }
}
