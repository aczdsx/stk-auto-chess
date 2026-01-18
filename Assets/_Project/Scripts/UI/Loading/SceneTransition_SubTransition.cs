using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    public class SceneTransition_SubTransition : SceneTransitionBase
    {
        private AsyncOperationHandle<GameObject> handle;
        private SubTransitionBase subTransition;
        
        public override void Initialize(object viewOption)
        {
            handle = Addressables.InstantiateAsync(viewOption as string, CachedTr);
        }

        public override async UniTask FadeInAsync()
        {
            await handle.WaitUntilDone();
            subTransition = handle.Result.GetComponent<SubTransitionBase>();
            await subTransition.FadeInAsync();
        }

        public override async UniTask FadeOutAsync()
        {
            await subTransition.FadeOutAsync();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            handle.Release();
        }
    }
}
