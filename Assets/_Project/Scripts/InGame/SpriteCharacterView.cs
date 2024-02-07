using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CookApps.SampleTeamBattle
{
    public class SpriteCharacterView : CachedMonoBehaviour, ICharacterView
    {
        public event Action<string, AnimationEventKey> OnAnimationEvent;

        public float Height => throw new NotImplementedException();

        public async UniTask Initialize(ICharacterStatData statData)
        {
            throw new NotImplementedException();
        }

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
        public ICharacterView GetCharacterView(ICharacterStatData statData)
        {
            if (!InGameResourceHolder.PlayerCharacterPrefabs.TryGetValue(statData.CharacterId, out GameObject prefab))
            {
                return default;
            }

            GameObject go = Object.Instantiate(prefab);
            var view = go.GetComponent<SpriteCharacterView>();
            return view;
        }

        public void ReturnCharacterView(ICharacterView characterView)
        {
            if (characterView is SpriteCharacterView view)
            {
                Object.Destroy(view.gameObject);
            }
        }
    }
}
