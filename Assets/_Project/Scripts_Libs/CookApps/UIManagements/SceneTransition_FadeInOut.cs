using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.UIManagements
{
    public class SceneTransition_FadeInOut : SceneTransitionBase
    {
        [SerializeField] private Image dim;
        private float fadeInDuration = 0.25f;
        private float fadeOutDuration = 0.5f;

        public override void Initialize(object viewOption)
        {
            var dimLayer = CachedGo.AddComponent<Image>();
            dimLayer.color = new Color(0f, 0f, 0f, 0f);
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.black);
            tex.Apply();
            dimLayer.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            var dimLayerTr = CachedGo.GetComponent<RectTransform>();

            dimLayerTr.anchorMax = Vector2.one;
            dimLayerTr.anchorMin = Vector2.zero;
            dimLayerTr.sizeDelta = Vector2.zero;

            dim = dimLayer;
        }

        public override async UniTask FadeInAsync()
        {
            Color color = dim.color;
            float diff = 1f - color.a;
            while (color.a < 1f)
            {
                color.a += diff * Time.deltaTime / fadeInDuration;
                dim.color = color;
                await Awaitable.NextFrameAsync();
            }
        }

        public override async UniTask FadeOutAsync()
        {
            Color color = dim.color;
            float diff = 0f - color.a;
            while (color.a > 0f)
            {
                color.a += diff * Time.deltaTime / fadeOutDuration;
                dim.color = color;
                await Awaitable.NextFrameAsync();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (dim != null && dim.sprite != null)
            {
                var tex = dim.sprite.texture;
                Destroy(dim.sprite);
                if (tex != null)
                    Destroy(tex);
                dim.sprite = null;
            }
        }
    }
}
