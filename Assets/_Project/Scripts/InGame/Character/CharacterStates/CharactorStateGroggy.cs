using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CharacterStateGroggy : CharacterStateBase
{
    public override StatePriority StatePriority => StatePriority.Groggy;
    
    public override void StateStart()
    {
        base.StateStart();
        AnimationClip clip = characCtrl.GetCharacterView().PlayAnimation(AnimationKey.GROGGY);
        Debug.Log($"{characCtrl.CharacterId} : AnimationClip clip = characCtrl.GetCharacterView().PlayAnimation(AnimationKey.GROGGY);");
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        return CharacterStateRunningResult.None;
    }
}
