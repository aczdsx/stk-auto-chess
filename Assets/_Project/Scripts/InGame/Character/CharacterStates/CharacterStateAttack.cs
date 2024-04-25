using System.Collections.Generic;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

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
        CharacterController atkTarget = characCtrl.Target;
        if (atkTarget == null || !atkTarget.IsAlive || !InGameObjectManager.Instance.IsInRange(characCtrl, characCtrl.Target))
        {
            isAttackAnimRunning = false;
            ReturnToIdle();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        Vector2 diff = characCtrl.Target.Position - characCtrl.Position;
        characCtrl.FlipX = diff.x > 0;

        if (characCtrl.GetAttackCoolTime() <= 0f)
        {
            characCtrl.ResetAttackCoolTime();

            // 이펙트 코드에게 공격 횟수 전달
            List<EffectCodeStatBase> characEffectCodes = characCtrl.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnAttack);
            EffectCodeForLoopHelper.Call(characEffectCodes, EffectCodeCharacterLambda.CallOnAttackLambda);

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

        if (characCtrl.Target == null)
        {
            return;
        }

        if (AnimationEventKey.ExecuteStart < eventKey && eventKey < AnimationEventKey.ExecuteEnd)
        {
            int hitCount = eventKey - AnimationEventKey.ExecuteStart;

            // damage 계산
            CharacterController.DamageInfo damageInfo = characCtrl.Target.PrecalculateDamageAmount(characCtrl.AD, 0, characCtrl, 0, false);
            characCtrl.PostCalculateDamageAmount(ref damageInfo, characCtrl.Target);
            if (hitCount > 1)
            {
                damageInfo.damageAmount /= hitCount;
            }

            // var hitId = 0;
            if (characCtrl.GetCharacterStat().AttackType == AttackType.Projectile)
            {
                if (characCtrl == null || characCtrl.IsAlive == false)
                {
                    return;
                }

                // TODO: throw projectile Effect
                characCtrl.Target.GetDamaged(damageInfo, characCtrl);
            }
            else
            {
                // TODO: Effect
                characCtrl.Target.GetDamaged(damageInfo, characCtrl);
            }
        }
    }

    protected virtual void ReturnToIdle()
    {
        characCtrl.Target = null;
        characCtrl.AddNextState<CharacterStateIdle>();
    }

    public override void StateEnd(bool isForced)
    {
        base.StateEnd(isForced);
        characCtrl.GetCharacterView().SetAnimationSpeed(1f);
    }
}
