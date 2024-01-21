using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle.UIManagements
{
    public class SceneTransition_Instant : ISceneTransition
    {
        public async UniTask FadeInAsync()
        {
            await UniTask.Yield();
        }

        public async UniTask FadeOutAsync(bool withDelete)
        {
            await UniTask.Yield();
        }
    }
}
