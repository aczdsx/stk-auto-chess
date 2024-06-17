using CookApps.BattleSystem;
using UnityEngine;

public class CharacterStateIdle : CharacterStateBase
{
    private const float ScanTargetInterval = 0.1f;
    private float scanTargetTime = 0f;

    public override void StateStart()
    {
        base.StateStart();
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.IDLE);
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

        // 2. 적을 찾아서 타겟으로 설정 (찾을 필요 없다면 스킵)
        if (characCtrl.GetCharacterStat().ScanType == ScanType.Nearest)
        {
            characCtrl.Target = InGameObjectManager.Instance.GetNearestTarget(characCtrl);
        }
        else
        {
            characCtrl.Target = InGameObjectManager.Instance.GetNearestTarget(characCtrl);
        }

        if (characCtrl.Target is {IsAlive: false})
        {
            characCtrl.Target = null;
            return CharacterStateRunningResult.CanCallAllWithoutMove;
        }

        // 3. 적이 공격 범위 안에 들어왔는지 체크
        if (characCtrl.Target != null)
        {
            var isInRange = InGameObjectManager.Instance.IsInRange(characCtrl, characCtrl.Target);

            if (isInRange)
            {
                // 4-1. 공격 범위 안에 들어왔다면 공격 상태로 전환
                characCtrl.AddNextState<CharacterStateAttack>();
            }
            else
            {
                characCtrl.MoveCharacter(isInRange);

                return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
            }
        }

        return CharacterStateRunningResult.CanCallAllWithoutMove;
    }
}
