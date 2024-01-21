using CookApps.TeamBattle.BattleSystem;
using UnityEngine;

public class CharacterStateMove : CharacterStateBase
{
    private float scanTargetTime = 0f;

    public override void StateStart()
    {
        base.StateStart();
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.Walk);
        scanTargetTime = 1f;
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        if (characCtrl.NeedToBeIdle())
        {
            characCtrl.AddNextState<CharacterStateIdle>();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        //조이스틱 떗을때 한번더 타겟 검사해야됨
        if (characCtrl.target == null)
        {
            characCtrl.target = InGameObjectManager.Instance.GetTarget(characCtrl);
        }
        else
        {
            scanTargetTime -= dt;
            if (scanTargetTime > 0f)
            {
                return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
            }

            characCtrl.target = InGameObjectManager.Instance.GetTarget(characCtrl);
            scanTargetTime = 1f;
        }

        if (characCtrl.target == null)
        {
            characCtrl.AddNextState<CharacterStateIdle>();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        if (!characCtrl.target.IsAlive)
        {
            characCtrl.AddNextState<CharacterStateIdle>();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        Vector2 targetPos = characCtrl.target.Position;

        if (!characCtrl.HasCrowdControl(CrowdControlType.Entangle))
        {
            Vector2 moveVec = targetPos - characCtrl.Position;
            moveVec = moveVec.normalized * (characCtrl.MoveSpeed * dt);
            characCtrl.Position += moveVec;
            characCtrl.FlipX = moveVec.x > 0;
            characCtrl.GetCharacterView().LookAt(characCtrl.FlipX);
        }

        float range = characCtrl.AttackRange;
        Vector2 diff = targetPos - characCtrl.Position;
        float resultRange = range * range;

        if (diff.sqrMagnitude < resultRange)
        {
            ChangeToAttackState();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        return CharacterStateRunningResult.CanCallAll;
    }

    private void ChangeToAttackState()
    {
        characCtrl.AddNextState<CharacterStateAttack>();
    }
}
