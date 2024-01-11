using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.BattleSystem
{
    public class CharacterStateDead : CharacterStateBase
    {
        private float elapsedTime = 0f;

        public override void StateStart()
        {
            base.StateStart();
            elapsedTime = 0;
            characCtrl.GetCharacterView().PlayAnimation(AnimationKey.Death);
        }

        public override CharacterStateRunningResult CharacterStateRunning(float dt)
        {
            if (characCtrl == null)
            {
                return CharacterStateRunningResult.None;
            }

            return CharacterStateRunningResult.None;
        }

        public override void AnimationEventCallback(string animName, AnimationEventKey eventKey)
        {
            base.AnimationEventCallback(animName, eventKey);
            // Debug.Log($"dead AnimationEventCallback {animName}, {eventKey}");

            if (animName != AnimationKey.Death.ToAnimationName())
            {
                return;
            }

            if (AnimationEventKey.End == eventKey)
            {
                // Debug.Log($"dead AnimationEventCallback {animName}, {eventKey}");
                // Debug.Log("deadEffect disappear");
                RemoveDataField();
                characCtrl.ClearAllState();
            }
        }

        private void RemoveDataField()
        {
            InGameObjectManager.Instance.RemoveCharacterFromField(characCtrl);
        }
    }
}
