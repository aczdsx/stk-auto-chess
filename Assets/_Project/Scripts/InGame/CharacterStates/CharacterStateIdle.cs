using CookApps.TeamBattle.BattleSystem;
using UnityEngine;

public class CharacterStateIdle : CharacterStateBase
{
    private float scanTargetTime = 0f;

    public override void StateStart()
    {
        base.StateStart();
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.Idle);
        scanTargetTime = 1f;
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        if (characCtrl.NeedToBeIdle())
        {
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        scanTargetTime -= dt;
        if (scanTargetTime > 0f)
        {
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        characCtrl.target = InGameObjectManager.Instance.GetNearestEnemy(characCtrl);
        scanTargetTime = 1f;

        if (characCtrl.target == null)
        {
            return CharacterStateRunningResult.CanCallAllWithoutMove;
        }

        if (!characCtrl.target.IsAlive)
        {
            characCtrl.target = null;
            return CharacterStateRunningResult.CanCallAllWithoutMove;
        }

        float range = characCtrl.AttackRange;
        Vector2 diff = characCtrl.target.Position - characCtrl.Position;

        float resultRange = range * range;

        if (diff.sqrMagnitude < resultRange)
        {
            characCtrl.AddNextState<CharacterStateAttack>();
        }
        else
        {
            characCtrl.AddNextState<CharacterStateMove>();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        return CharacterStateRunningResult.CanCallAllWithoutMove;
    }
}
