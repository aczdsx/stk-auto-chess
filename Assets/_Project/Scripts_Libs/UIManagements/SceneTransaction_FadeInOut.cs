using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle.UIManagements
{
    public class SceneTransaction_FadeInOut : MonoBehaviour, ISceneTransition
    {
        [SerializeField] private Image dim;
        private float fadeInDuration = 0.25f;
        private float fadeOutDuration = 0.5f;

        public static SceneTransaction_FadeInOut Create()
        {
            var prefab = Resources.Load<GameObject>("UI/FakeLoading");
            var go = Instantiate(prefab);
            DontDestroyOnLoad(go);
            return go.GetComponent<SceneTransaction_FadeInOut>();
        }

        public async UniTask FadeInAsync()
        {
            var color = dim.color;
            var diff = 1f - color.a;
            while (color.a < 1f)
            {
                color.a += diff * Time.deltaTime / fadeInDuration;
                dim.color = color;
                await UniTask.Yield();
            }
        }

        public async UniTask FadeOutAsync(bool withDelete)
        {
            var color = dim.color;
            var diff = 0f - color.a;
            while (color.a > 0f)
            {
                color.a += diff * Time.deltaTime / fadeOutDuration;
                dim.color = color;
                await UniTask.Yield();
            }

            if (withDelete)
                Destroy(gameObject);
        }
    }
}
