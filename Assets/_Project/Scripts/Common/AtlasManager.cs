using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;

namespace CookApps.SampleTeamBattle
{
    [CreateAssetMenu(fileName = "AtlasManager", menuName = "ScriptableObjects/AtlasManager", order = 1)]
    public class AtlasManagerScriptableObject : ScriptableObject
    {
        // [SerializeField] public AssetReferenceT<SpriteAtlas>[] ingameAtlasRefs;
        // [SerializeField] public AssetReferenceT<SpriteAtlas>[] outgameAtlasRefs;
        [SerializeField] [Header("아틀라스가 쓰이는 씬들의 이름을 적어주세요.")]
        public SerializableDictionary<AssetReferenceT<SpriteAtlas>, List<string>> atlasRefs;
    }

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
            SceneUIManager.OnStartChangeScene += OnStartChangeScene;
        }

        private async UniTask OnStartChangeScene(string prevSceneName, string nextSceneName, object defaultUIData)
        {
            foreach ((AssetReferenceT<SpriteAtlas> assetRef, List<string> sceneNames) in so.atlasRefs)
            {
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
