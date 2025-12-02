using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Text;
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
        private Dictionary<string, Sprite> loadedSpriteDict = new ();

        public Sprite GetSprite(string atlasName, string spriteName)
        {
            if (loadedSpriteDict.TryGetValue(ZString.Format("{0}_{1}", atlasName, spriteName), out Sprite loadedSprite))
            {
                return loadedSprite;
            }
            
            if (loadedAtlasDict.TryGetValue(atlasName, out SpriteAtlas atlas))
            {
                Sprite sprite = atlas.GetSprite(spriteName);
                if (sprite != null)
                {
                    loadedSpriteDict.Add(ZString.Format("{0}_{1}", atlasName, spriteName), sprite);
                }
                return sprite;
            }

            return null;
        }

        public async UniTask Initialize(string soAddressable)
        {
            so = await Addressables.LoadAssetAsync<AtlasManagerScriptableObject>(soAddressable);
        }

        public async UniTask OnStartChangeScene(string prevSceneName, string nextSceneName, object defaultUIData)
        {
            foreach (var keyValuePair in loadedSpriteDict)
            {
                Object.Destroy(keyValuePair.Value);
            }

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
                        Addressables.Release(atlas);
                        loadedAtlasDict.Remove(atlasName);
                    }
                }

                if (!sceneNames.Contains(nextSceneName))
                {
                    continue;
                }

                {
                    var atlas = await Addressables.LoadAssetAsync<SpriteAtlas>(assetRef);
                    assetRefToAtlasNameDict.TryAdd(assetRef, atlas.name);
                    loadedAtlasDict.TryAdd(atlas.name, atlas);
                }
            }
        }
    }
}
