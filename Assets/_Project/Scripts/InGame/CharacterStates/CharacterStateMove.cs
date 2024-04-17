using CookApps.TeamBattle.BattleSystem;
using PrimeTween;
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
        Tween.(characCtrl.GetCharacterView().CachedTr, )
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        return CharacterStateRunningResult.CanCallAllWithoutActivate;
    }

    private void ChangeToAttackState()
    {
        characCtrl.AddNextState<CharacterStateAttack>();
    }
}
