using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle
{
    public class ImageFromSpriteManager : Image
    {
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

        public async Awaitable SetSprite(string spriteName)
        {
            if (this.spriteName == spriteName)
                return;

            enabled = false;
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
            this.sprite = sprite;
            enabled = true;
        }
    }
}
