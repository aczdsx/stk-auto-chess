using CookApps.BattleSystem;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharacterStateIdleHealer : CharacterStateIdle
{
    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        // 1. 캐릭터가 Idle 상태로 있어야 하는지 체크
        if (characCtrl.NeedToBeCrowdControlState())
        {
            characCtrl.AddNextState<CharacterStateCC>();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        scanTargetTime -= dt;
        if (scanTargetTime > 0f)
        {
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }
        scanTargetTime = ScanTargetInterval;

        // 2. 타겟 찾기
        if (characCtrl.Target == null ||
         (characCtrl.Target.AllianceType != characCtrl.AllianceType))
        {
            characCtrl.Target = InGameObjectManager.Instance.GetLowestHPOurTeam(characCtrl);
        }

        // 3. 적이 공격 범위 안에 들어왔는지 체크
        if (characCtrl.Target != null)
        {
            //힐 할 캐릭터가
            var isInRange = InGameObjectManager.Instance.IsInRange(characCtrl, characCtrl.Target);

            if (isInRange)
            {
                // 4-1. 공격 범위 안에 들어왔다면 공격 상태로 전환
                characCtrl.AddNextState<CharacterStateAttack>();
            }
            else
            {
                characCtrl.MoveToCharacter(isInRange, characCtrl.Target);
            }
        }

        return CharacterStateRunningResult.CanCallAllWithoutMove;
    }
}
