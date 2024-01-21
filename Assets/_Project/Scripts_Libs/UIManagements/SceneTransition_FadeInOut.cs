using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle.UIManagements
{
    public class SceneTransition_FadeInOut : MonoBehaviour, ISceneTransition
    {
        [SerializeField] private Image dim;
        private float fadeInDuration = 0.25f;
        private float fadeOutDuration = 0.5f;

        public static SceneTransition_FadeInOut Create()
        {
            var prefab = Resources.Load<GameObject>("UI/FakeLoading");
            GameObject go = Instantiate(prefab);
            DontDestroyOnLoad(go);
            return go.GetComponent<SceneTransition_FadeInOut>();
        }

        public async UniTask FadeInAsync()
        {
            Color color = dim.color;
            float diff = 1f - color.a;
            while (color.a < 1f)
            {
                color.a += diff * Time.deltaTime / fadeInDuration;
                dim.color = color;
                await UniTask.Yield();
            }
        }

        public async UniTask FadeOutAsync(bool withDelete)
        {
            Color color = dim.color;
            float diff = 0f - color.a;
            while (color.a > 0f)
            {
                color.a += diff * Time.deltaTime / fadeOutDuration;
                dim.color = color;
                await UniTask.Yield();
            }

            if (withDelete)
            {
                Destroy(gameObject);
            }
        }
    }
}
