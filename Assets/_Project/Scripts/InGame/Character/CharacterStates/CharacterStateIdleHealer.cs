using System.ComponentModel;
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
        if (characCtrl.Target == null)
        {
            characCtrl.Target = characCtrl.FindTarget();
        }


        if (characCtrl.Target is { IsAlive: false })
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
                characCtrl.MoveToCharacter(isInRange, characCtrl.Target);
            }
        }

        return CharacterStateRunningResult.CanCallAllWithoutMove;
    }

    /// <summary>
    /// 힐러 전용 타겟 찾기 (힐할 캐릭터 우선, 없으면 공격 타겟)
    /// </summary>
    public new static CookApps.BattleSystem.CharacterController FindTarget(CookApps.BattleSystem.CharacterController characCtrl)
    {
        CookApps.BattleSystem.CharacterController target = null;
        // 힐할 캐릭터 찾기
        var healTarget = InGameObjectManager.Instance.GetLowestHPOurTeam(characCtrl);
        //hp가 제일 적은 친구를 찾는다.
        if (healTarget != null && InGameObjectManager.Instance.IsInRange(characCtrl, healTarget))
        {
            target = healTarget;
        }
        else
        {//범위 안에 없다면 캐릭터 리스트에서 범위안에 힐 할 대상이 있는지 체크
            var attackRangeTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(characCtrl.CurrentTile, characCtrl.AttackRange);
            double minHP = double.MaxValue;
            CookApps.BattleSystem.CharacterController minHPCharacter = null;
            foreach (var tile in attackRangeTiles)
            {
                if (tile.OccupiedCharacter is null || tile.OccupiedCharacter.AllianceType != characCtrl.AllianceType)
                    continue;
                if(tile.OccupiedCharacter == characCtrl)
                    continue;


                if (tile.OccupiedCharacter.CurrentHp < minHP && tile.OccupiedCharacter.IsAlive)
                {
                    minHP = tile.OccupiedCharacter.CurrentHp;
                    minHPCharacter = tile.OccupiedCharacter;
                }
            }

            if (minHPCharacter != null)
            {
                target = minHPCharacter;
            }
            else
            {
                target = InGameObjectManager.Instance.GetOptimalAttackTarget(characCtrl);
            }
        }


        if (target?.AllianceType == characCtrl.AllianceType)
        {
            Debug.Log("Healer!! HealMode");
        }
        else
        {
            Debug.Log("Healer!! AttackMode");
        }

        return target;
    }
}
