using System;
using CookApps.BattleSystem;
using CookApps.TeamBattle;

public class AnimationEventListener : CachedMonoBehaviour
{
    public event Action<AnimationEventKey> OnAnimationEvent = delegate { };

    public void InvokeAnimationEvent(AnimationEventKey animationEventName)
    {
        OnAnimationEvent.Invoke(animationEventName);
    }
}
