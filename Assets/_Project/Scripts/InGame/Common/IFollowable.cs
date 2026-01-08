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

    public readonly struct SimpleSkillTopFXTransformFollowable : IFollowable
    {
        private readonly CharacterController character;
        public SimpleSkillTopFXTransformFollowable(CharacterController character)
        {
            this.character = character;
        }

        public bool IsAlive => character.GetCharacterView() != null && character.IsAlive;
        public Vector3 GetPosition() => character.GetCharacterView().SkillTopFXTransform.position;
    }
    public readonly struct SimpleSkillMiddleFXTransformFollowable : IFollowable
    {
        private readonly CharacterController character;
        public SimpleSkillMiddleFXTransformFollowable(CharacterController character)
        {
            this.character = character;
        }

        public bool IsAlive => character.GetCharacterView() != null && character.IsAlive;
        public Vector3 GetPosition() => character.GetCharacterView().SkillMiddleFXTransform.position;
    }
    public readonly struct SimpleSkillBottomFXTransformFollowable : IFollowable
    {
        private readonly CharacterController character;
        public SimpleSkillBottomFXTransformFollowable(CharacterController character)
        {
            this.character = character;
        }

        public bool IsAlive => character.GetCharacterView() != null && character.IsAlive;
        public Vector3 GetPosition() => character.GetCharacterView().SkillBottomFXTransform.position;
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
