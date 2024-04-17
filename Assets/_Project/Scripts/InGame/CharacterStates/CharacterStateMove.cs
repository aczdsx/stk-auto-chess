using CookApps.TeamBattle.BattleSystem;
using UnityEngine;

/// <summary>
/// 1칸 이동하는 상태
/// 이동 후 idle로 전환
/// </summary>
public class CharacterStateMove : CharacterStateBase
{
    private bool isMoving;

    public override void StateStart()
    {
        base.StateStart();
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.Walk);
        isMoving = true;
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        if (characCtrl.NeedToBeIdle())
        {
            characCtrl.AddNextState<CharacterStateIdle>();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
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
