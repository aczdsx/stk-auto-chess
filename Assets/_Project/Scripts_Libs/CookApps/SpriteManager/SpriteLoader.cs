using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle
{
    public class SpriteLoader : CachedMonoBehaviour
    {
        [SerializeField] private bool isSpriteRenderer;
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Image targetImage;

        private CancellationTokenSource cts;
        private string spriteName;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (!Application.isPlaying)
                return;
            cts?.Cancel();
            cts = null;
            if (string.IsNullOrEmpty(spriteName))
                return;
            SpriteManager.Instance.UnloadSprite(spriteName);
            this.spriteName = null;
        }

        public async UniTask SetSprite(string spriteName)
        {
            if (this.spriteName == spriteName)
                return;

            SetTargetEnable(false);
            if (!string.IsNullOrEmpty(this.spriteName))
            {
                SpriteManager.Instance.UnloadSprite(this.spriteName);
                this.spriteName = null;
            }
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            var task = SpriteManager.Instance.GetSprite(spriteName);
            var sprite = await task;
            if (token.IsCancellationRequested)
            {
                SpriteManager.Instance.UnloadSprite(spriteName);
                return;
            }

            this.spriteName = spriteName;
            SetTargetSprite(sprite);
            SetTargetEnable(true);
            enabled = true;
        }

        public void UnloadSprite()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            if (string.IsNullOrEmpty(spriteName))
                return;
            SpriteManager.Instance.UnloadSprite(spriteName);
            this.spriteName = null;
            SetTargetEnable(false);
        }

        private void SetTargetEnable(bool enable)
        {
            if (isSpriteRenderer)
            {
                targetRenderer.enabled = enable;
            }
            else
            {
                targetImage.enabled = enable;
            }
        }

        private void SetTargetSprite(Sprite sprite)
        {
            if (isSpriteRenderer)
            {
                targetRenderer.sprite = sprite;
            }
            else
            {
                targetImage.sprite = sprite;
            }
        }
    }
}
