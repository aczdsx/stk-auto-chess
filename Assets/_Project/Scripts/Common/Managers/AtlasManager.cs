using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;

namespace CookApps.AutoBattler
{
    public class AtlasManager : Singleton<AtlasManager>
    {
        private AtlasManagerScriptableObject so;
        private Dictionary<AssetReferenceT<SpriteAtlas>, string> assetRefToAtlasNameDict = new ();
        private Dictionary<string, SpriteAtlas> loadedAtlasDict = new ();

        public Sprite GetSprite(string atlasName, string spriteName)
        {
            if (loadedAtlasDict.TryGetValue(atlasName, out SpriteAtlas atlas))
            {
                return atlas.GetSprite(spriteName);
            }

            return null;
        }

        public async UniTask Initialize(string soAddressable)
        {
            so = await AddressableLoadHelper.LoadAssetAsync<AtlasManagerScriptableObject>(soAddressable);
        }

        public async UniTask OnStartChangeScene(string prevSceneName, string nextSceneName, object defaultUIData)
        {
            foreach (AtlasData data in so.atlasRefs)
            {
                AssetReferenceT<SpriteAtlas> assetRef = data.assetRef;
                List<string> sceneNames = data.sceneNames;
                if (sceneNames.Contains(prevSceneName))
                {
                    if (sceneNames.Contains(nextSceneName))
                    {
                        continue;
                    }

                    if (assetRefToAtlasNameDict.TryGetValue(assetRef, out string atlasName) &&
                        loadedAtlasDict.TryGetValue(atlasName, out SpriteAtlas atlas))
                    {
                        AddressableLoadHelper.ReleaseLoadedAsset(atlas);
                        loadedAtlasDict.Remove(atlasName);
                    }
                }

                if (!sceneNames.Contains(nextSceneName))
                {
                    continue;
                }

                {
                    var atlas = await AddressableLoadHelper.LoadAssetAsync<SpriteAtlas>(assetRef);
                    assetRefToAtlasNameDict.Add(assetRef, atlas.name);
                    loadedAtlasDict.Add(atlas.name, atlas);
                }
            }
        }
    }
}
