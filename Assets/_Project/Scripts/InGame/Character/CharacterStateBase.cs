using System;
using CookApps.TeamBattle;

namespace CookApps.BattleSystem
{
    [Flags]
    public enum CharacterStateRunningResult
    {
        None = 0,                             //이후에 아무것도 호출하지 않을때
        CanCallEffectCodeOnUpdate = 1 << 0,   //쿨타임말고 업데이트를 돌릴때 cc기 맞아도 돌아야함
        CanCallEffectCodeOnCooltime = 1 << 1, //스킬 쿨타임
        CanCallEffectCodeActivate = 1 << 2,   //스킬을 사용하겠다
        CanCallMove = 1 << 3,                 //이동 하겠다
        CanCallEffectCodeOnUpdateAndOnCooltime = CanCallEffectCodeOnUpdate | CanCallEffectCodeOnCooltime,
        CanCallAllWithoutMove = CanCallEffectCodeOnUpdate | CanCallEffectCodeOnCooltime | CanCallEffectCodeActivate,
        CanCallAllWithoutActivate = CanCallEffectCodeOnUpdate | CanCallEffectCodeOnCooltime | CanCallMove,
        CanCallAll = CanCallEffectCodeOnUpdate | CanCallEffectCodeOnCooltime | CanCallEffectCodeActivate | CanCallMove,
    }

    public abstract class CharacterStateBase : StateBase
    {
        protected CharacterController characCtrl = null;

        public override void StateInit(object owner)
        {
            characCtrl = owner as CharacterController;
            CADebug.Assert(characCtrl != null, "CharacterState는 target으로 characterController를 줘야함.");
        }

        public override void StateStart()
        {
            characCtrl.GetCharacterView().SetAnimationSpeed(1f);
        }

        public virtual CharacterStateRunningResult CharacterStateRunning(float dt)
        {
            return CharacterStateRunningResult.CanCallAll;
        }

        public sealed override void StateRunning(float dt)
        {
            CharacterStateRunning(dt);
        }

        public override void StateEnd(bool isForced)
        {
            characCtrl = null;
        }

        public virtual void AnimationEventCallback(AnimationKey animName, AnimationEventKey eventKey)
        {
        }
    }
}
