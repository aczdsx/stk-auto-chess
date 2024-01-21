using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.TeamBattle.BattleSystem
{
    public interface ICharacterView : ICachedGameObject, ICachedTransform
    {
        event Action<string, AnimationEventKey> OnAnimationEvent;

        float Height { get; }

        void UpdateTickAndPosition(float deltaTime, Vector3 position, Vector3 viewPosition);
        void SetAnimationSpeed(float speed);
        void LookAt(bool isFlipX);
        AnimationClip PlayAnimation(AnimationKey animationKey, bool isLoop = false);
        void OnHit();
    }

    public interface ICharacterViewPool
    {
        UniTask<ICharacterView> GetCharacterView(ICharacterStatData statData);
        void ReturnCharacterView(ICharacterView characterView);
    }

    public static class CharacterViewPool
    {
        private static ICharacterViewPool instance;

        public static ICharacterViewPool Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new NullReferenceException("CharacterView is not initialized yet.");
                }

                return instance;
            }
        }

        public static void Initialize(ICharacterViewPool instance)
        {
            CharacterViewPool.instance = instance;
        }
    }
}
