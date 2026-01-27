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
        private class AsyncAssetHandle<T> where T : Object
        {
            public readonly ulong hash;
            public readonly AssetReferenceT<T> assetRef;
            public T asset;
            public int refCount;
            public UniTaskCompletionSource<T> loadingTask;
            public bool isLoading;

            public AsyncAssetHandle(ulong hash, AssetReferenceT<T> assetRef)
            {
                this.hash = hash;
                this.assetRef = assetRef;
            }

            public async UniTask<T> LoadAsync(string debugContext)
            {
                if (asset != null)
                    return asset;

                if (isLoading && loadingTask != null)
                    return await loadingTask.Task;

                isLoading = true;
                loadingTask = new UniTaskCompletionSource<T>();
                try
                {
                    var oper = assetRef.OperationHandle;
                    if (!oper.IsValid())
                        oper = assetRef.LoadAssetAsync();

                    await oper.WaitUntilDone();
                    if (!oper.IsValid())
                    {
                        Debug.LogError($"SpriteManager: LoadAsync: assetRef is not valid: {debugContext}");
                        loadingTask.TrySetResult(null);
                        return null;
                    }

                    asset = oper.Result as T;
                    loadingTask.TrySetResult(asset);
                    return asset;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"SpriteManager: LoadAsync: exception: {ex}");
                    loadingTask.TrySetResult(null);
                    return null;
                }
                finally
                {
                    isLoading = false;
                }
            }

            /// <summary>
            /// refCount를 감소시키고, 해제가 필요하면 true를 반환합니다.
            /// </summary>
            public bool DecrementRef(string debugContext)
            {
                if (refCount <= 0)
                {
                    Debug.LogWarning($"[SpriteManager] UnloadSprite: refCount is already 0. context: {debugContext}");
                    return false;
                }
                refCount--;

                if (isLoading)
                {
                    Debug.LogWarning($"[SpriteManager] UnloadSprite called while loading. context: {debugContext}");
                    return false;
                }

                return refCount <= 0;
            }

            public void Release()
            {
                asset = null;
                loadingTask = null;
                assetRef.ReleaseAsset();
            }

            public void ForceRelease(string debugContext)
            {
                if (isLoading && loadingTask != null)
                {
                    Debug.LogWarning($"[SpriteManager] UnloadAllSprites: still loading. hash: {hash}, context: {debugContext}");
                    loadingTask.TrySetResult(null);
                }

                asset = null;
                refCount = 0;
                loadingTask = null;
                isLoading = false;
                assetRef.ReleaseAsset();
            }
        }

        private class AtlasEntry
        {
            public readonly AsyncAssetHandle<SpriteAtlas> handle;
            public readonly Dictionary<string, Sprite> sprites = new();

            public AtlasEntry(ulong hash, AssetReferenceT<SpriteAtlas> atlasRef)
            {
                handle = new AsyncAssetHandle<SpriteAtlas>(hash, atlasRef);
            }

            public void ClearSprites()
            {
                foreach (var sprite in sprites.Values)
                {
                    Object.Destroy(sprite);
                }
                sprites.Clear();
            }
        }

        private SpriteManagerScriptableObject so;
        private Dictionary<ulong, AtlasEntry> atlasEntries;
        private Dictionary<ulong, AsyncAssetHandle<Sprite>> spriteHandles;
        private bool isInitialized;

        public async UniTask Initialize(string soAddress)
        {
            var handle = Addressables.LoadAssetAsync<SpriteManagerScriptableObject>(soAddress);
            so = await handle.WaitUntilDone();
            atlasEntries = new Dictionary<ulong, AtlasEntry>();
            foreach (var atlasRef in so.atlasRefs)
            {
                var hash = atlasRef.AssetGUID.djb2Hash();
                atlasEntries.Add(hash, new AtlasEntry(hash, atlasRef));
            }

            spriteHandles = new Dictionary<ulong, AsyncAssetHandle<Sprite>>();
            foreach (var (hash, spriteRef) in so.spriteRefs)
            {
                spriteHandles.Add(hash, new AsyncAssetHandle<Sprite>(hash, spriteRef));
            }

            isInitialized = true;
        }

        public async UniTask<Sprite> GetSprite(string spriteName)
        {
            if (!isInitialized)
            {
                Debug.LogError("[SpriteManager] GetSprite called before Initialize.");
                return null;
            }

            // sprite in atlas
            if (so.spriteNameToAtlasDict.TryGetValue(spriteName.djb2Hash(), out var atlasNameHash))
            {
                if (!atlasEntries.TryGetValue(atlasNameHash, out var entry))
                    return null;

                entry.handle.refCount++;
                bool success = false;
                try
                {
                    if (entry.sprites.TryGetValue(spriteName, out var sprite))
                    {
                        success = true;
                        return sprite;
                    }

                    var atlas = await entry.handle.LoadAsync(spriteName);
                    if (atlas == null)
                        return null;

                    sprite = atlas.GetSprite(spriteName);
                    if (sprite == null)
                    {
                        Debug.LogError($"[SpriteManager] Failed to get sprite from atlas. spriteName: {spriteName}, atlas: {atlas.name}");
                        return null;
                    }
                    entry.sprites[spriteName] = sprite;
                    success = true;
                    return sprite;
                }
                finally
                {
                    if (!success)
                        entry.handle.refCount--;
                }
            }

            // standalone sprite
            {
                if (spriteHandles.TryGetValue(spriteName.djb2Hash(), out var handle))
                {
                    handle.refCount++;
                    bool success = false;
                    try
                    {
                        if (handle.asset != null)
                        {
                            success = true;
                            return handle.asset;
                        }

                        var sprite = await handle.LoadAsync(spriteName);
                        if (sprite != null)
                            success = true;
                        return sprite;
                    }
                    finally
                    {
                        if (!success)
                            handle.refCount--;
                    }
                }
            }
            return null;
        }

        public void UnloadSprite(string spriteName)
        {
            if (!isInitialized)
            {
                Debug.LogError("[SpriteManager] UnloadSprite called before Initialize.");
                return;
            }

            // sprite in atlas
            if (so.spriteNameToAtlasDict.TryGetValue(spriteName.djb2Hash(), out var atlasNameHash))
            {
                if (!atlasEntries.TryGetValue(atlasNameHash, out var entry))
                    return;

                if (!entry.handle.DecrementRef(spriteName))
                    return;

                entry.ClearSprites();
                entry.handle.Release();
                return;
            }

            // standalone sprite
            {
                if (spriteHandles.TryGetValue(spriteName.djb2Hash(), out var handle))
                {
                    if (!handle.DecrementRef(spriteName))
                        return;

                    handle.Release();
                }
            }
        }

        public void UnloadAllSprites()
        {
            if (!isInitialized)
            {
                Debug.LogError("[SpriteManager] UnloadAllSprites called before Initialize.");
                return;
            }

            foreach (var entry in atlasEntries.Values)
            {
                entry.handle.ForceRelease("atlas");
                entry.ClearSprites();
            }

            foreach (var handle in spriteHandles.Values)
            {
                handle.ForceRelease("sprite");
            }
        }
    }
}
