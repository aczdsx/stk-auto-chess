using UnityEngine;

namespace CookApps.BattleSystem
{
    public interface IFollowable
    {
        bool IsAlive { get; }
        Vector3 GetPosition();
    }

    public readonly struct SimpleSkillTransformFollowable : IFollowable
    {
        private readonly CharacterController character;
        public SimpleSkillTransformFollowable(CharacterController character)
        {
            this.character = character;
        }

        public bool IsAlive => character.GetCharacterView() != null && character.IsAlive;
        public Vector3 GetPosition() => character.GetCharacterView().SkillRootTransform.position;
    }

    public readonly struct SimpleTransformFollowable : IFollowable
    {
        private readonly Transform transform;
        public SimpleTransformFollowable(Transform transform)
        {
            this.transform = transform;
        }

        public bool IsAlive => transform != null;
        public Vector3 GetPosition() => transform.position;
    }
}
