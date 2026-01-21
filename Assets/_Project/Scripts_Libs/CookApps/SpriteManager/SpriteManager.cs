using System.Collections.Generic;
using CookApps.TeamBattle.Utility;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle
{
    public class SpriteManager : Singleton<SpriteManager>
    {
        private class AtlasCache
        {
            public ulong hash;
            public AssetReferenceT<SpriteAtlas> atlasRef;
            public SpriteAtlas atlas;
            public Dictionary<string, Sprite> sprites = new();
            public int refCount;
        }
        private class SpriteCache
        {
            public ulong hash;
            public AssetReferenceT<Sprite> spriteRef;
            public int refCount;
            public Sprite loadedSprite;
        }

        private SpriteManagerScriptableObject so;
        private Dictionary<ulong, AtlasCache> atlasCaches;
        private Dictionary<ulong, SpriteCache> spriteCaches;

        public async UniTask<Sprite> GetSprite(string spriteName)
        {
            // sprite in atlas
            if (so.spriteNameToAtlasDict.TryGetValue(spriteName.djb2Hash(), out var atlasNameHash))
            {
                if (!atlasCaches.TryGetValue(atlasNameHash, out var cache))
                    return null;

                cache.refCount++;
                if (cache.sprites.TryGetValue(spriteName, out var sprite))
                    return sprite;

                if (cache.atlas == null)
                {
                    var assetRef = cache.atlasRef;
                    var oper = assetRef.OperationHandle;
                    if (!oper.IsValid())
                    {
                        Debug.LogError($"SpriteManager: GetSprite: assetRef is not valid: {spriteName}");
                        oper = assetRef.LoadAssetAsync();
                    }
                    await oper.WaitUntilDone();
                    if (!oper.IsValid())
                        return null;
                    cache.atlas = oper.Result as SpriteAtlas;
                }

                sprite = cache.atlas?.GetSprite(spriteName);
                cache.sprites[spriteName] = sprite;
                return sprite;
            }

            // standalone sprite
            {
                if (spriteCaches.TryGetValue(spriteName.djb2Hash(), out var cache))
                {
                    cache.refCount++;
                    if (cache.loadedSprite != null)
                        return cache.loadedSprite;

                    var assetRef = cache.spriteRef;
                    var oper = assetRef.OperationHandle;
                    if (!oper.IsValid())
                        oper = assetRef.LoadAssetAsync();
                    await oper.WaitUntilDone();
                    if (!oper.IsValid())
                        return null;

                    cache.loadedSprite = oper.Result as Sprite;
                    return cache.loadedSprite;
                }
            }
            return null;
        }

        public void UnloadSprite(string spriteName)
        {
            // sprite in atlas
            if (so.spriteNameToAtlasDict.TryGetValue(spriteName.djb2Hash(), out var atlasNameHash))
            {
                if (!atlasCaches.TryGetValue(atlasNameHash, out var cache))
                    return;

                cache.refCount--;
                if (cache.refCount > 0)
                    return;

                foreach (var sprite in cache.sprites.Values)
                {
                    Object.Destroy(sprite);
                }

                cache.sprites.Clear();
                cache.atlas = null;
                cache.atlasRef.ReleaseAsset();
                return;
            }

            // standalone sprite
            {
                if (spriteCaches.TryGetValue(spriteName.djb2Hash(), out var cache))
                {
                    cache.refCount--;
                    if (cache.refCount > 0)
                        return;
                    cache.loadedSprite = null;
                    cache.spriteRef.ReleaseAsset();
                    return;
                }
            }
        }

        public async UniTask Initialize(string soAddress)
        {
            var handle = Addressables.LoadAssetAsync<SpriteManagerScriptableObject>(soAddress);
            so = await handle.WaitUntilDone();
            atlasCaches = new Dictionary<ulong, AtlasCache>();
            foreach (var atlasRef in so.atlasRefs)
            {
                var hash = atlasRef.AssetGUID.djb2Hash();
                atlasCaches.Add(hash, new AtlasCache
                {
                    hash = hash,
                    atlasRef = atlasRef,
                });
            }

            spriteCaches = new Dictionary<ulong, SpriteCache>();
            foreach (var (hash, spriteRef) in so.spriteRefs)
            {
                spriteCaches.Add(hash, new SpriteCache
                {
                    hash = hash,
                    spriteRef = spriteRef,
                });
            }
        }

        public void UnloadAllSprites()
        {
            foreach (var cache in atlasCaches.Values)
            {
                foreach (var sprite in cache.sprites.Values)
                {
                    Object.Destroy(sprite);
                }

                cache.sprites.Clear();
                cache.atlas = null;
                cache.refCount = 0;
                cache.atlasRef.ReleaseAsset();
            }

            foreach (var cache in spriteCaches.Values)
            {
                cache.loadedSprite = null;
                cache.refCount = 0;
                cache.spriteRef.ReleaseAsset();
            }
        }
    }
}
