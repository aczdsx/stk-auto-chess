using CookApps.BattleSystem;
using UnityEngine;

public class CharactorStatePrologueDie : CharacterStateBase
{
    public override StatePriority StatePriority => StatePriority.Knockback;
    
    public override void StateStart()
    {
        base.StateStart();
        AnimationClip clip = characCtrl.GetCharacterView().PlayAnimation(AnimationKey.PARRY, false);
        Debug.Log($"{characCtrl.CharacterId} : AnimationClip clip = characCtrl.GetCharacterView().PlayAnimation(AnimationKey.GROGGY);");
        isBlockingChangeState = true;
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        return CharacterStateRunningResult.None;
    }
}
