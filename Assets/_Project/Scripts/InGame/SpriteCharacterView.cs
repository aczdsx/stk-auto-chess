using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.SampleTeamBattle
{
    public class SpriteCharacterView : CachedMonoBehaviour, ICharacterView
    {
        public event Action<string, AnimationEventKey> OnAnimationEvent;

        public float Height => throw new NotImplementedException();

        public void UpdateTickAndPosition(float deltaTime, Vector3 position, Vector3 viewPosition)
        {
            throw new NotImplementedException();
        }

        public void SetAnimationSpeed(float speed)
        {
            throw new NotImplementedException();
        }

        public void LookAt(bool isFlipX)
        {
            throw new NotImplementedException();
        }

        public AnimationClip PlayAnimation(AnimationKey animationKey, bool isLoop = false)
        {
            throw new NotImplementedException();
        }

        public void OnHit()
        {
            throw new NotImplementedException();
        }
    }

    public class SpriteCharacterViewPool : ICharacterViewPool
    {
        public UniTask<ICharacterView> GetCharacterView(ICharacterStatData statData)
        {
            throw new NotImplementedException();
        }

        public void ReturnCharacterView(ICharacterView characterView)
        {
            throw new NotImplementedException();
        }
    }
}
