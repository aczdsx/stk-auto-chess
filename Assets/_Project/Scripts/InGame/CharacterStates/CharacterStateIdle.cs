using CookApps.TeamBattle.BattleSystem;
using UnityEngine;

public class CharacterStateIdle : CharacterStateBase
{
    private const float ScanTargetInterval = 0.1f;
    private float scanTargetTime = 0f;

    public override void StateStart()
    {
        base.StateStart();
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.Idle);
        scanTargetTime = ScanTargetInterval;
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        // 1. 캐릭터가 Idle 상태로 있어야 하는지 체크
        if (characCtrl.NeedToBeIdle())
        {
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        scanTargetTime -= dt;
        if (scanTargetTime > 0f)
        {
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }
        scanTargetTime = ScanTargetInterval;

        // 2. 적을 찾아서 타겟으로 설정
        if (characCtrl.GetCharacterStat().ScanType == ScanType.Nearest)
        {
            characCtrl.target = InGameObjectManager.Instance.GetNearestEnemy(characCtrl);
        }
        else
        {
            characCtrl.target = InGameObjectManager.Instance.GetNearestEnemy(characCtrl);
        }

        if (characCtrl.target == null)
        {
            return CharacterStateRunningResult.CanCallAllWithoutMove;
        }

        if (!characCtrl.target.IsAlive)
        {
            characCtrl.target = null;
            return CharacterStateRunningResult.CanCallAllWithoutMove;
        }

        // 3. 적이 공격 범위 안에 들어왔는지 체크
        float range = characCtrl.AttackRange;
        Vector2 diff = characCtrl.target.Position - characCtrl.Position;
        float resultRange = range * range;

        if (diff.sqrMagnitude < resultRange)
        {
            // 4-1. 공격 범위 안에 들어왔다면 공격 상태로 전환
            characCtrl.AddNextState<CharacterStateAttack>();
        }
        else
        {
            // 4-2. 공격 범위 밖에 있다면 이동 상태로 전환
            characCtrl.AddNextState<CharacterStateMove>();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        return CharacterStateRunningResult.CanCallAllWithoutMove;
    }
}
