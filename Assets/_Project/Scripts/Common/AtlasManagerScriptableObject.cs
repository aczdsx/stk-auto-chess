using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;

namespace CookApps.SampleTeamBattle
{
    [Serializable]
    public class AtlasData
    {
        public AssetReferenceT<SpriteAtlas> assetRef;
        public List<string> sceneNames;
    }

    [CreateAssetMenu(fileName = "AtlasManager", menuName = "ScriptableObjects/AtlasManager", order = 1)]
    public class AtlasManagerScriptableObject : ScriptableObject
    {
        // [SerializeField] public AssetReferenceT<SpriteAtlas>[] ingameAtlasRefs;
        // [SerializeField] public AssetReferenceT<SpriteAtlas>[] outgameAtlasRefs;
        [SerializeField] [Header("아틀라스가 쓰이는 씬들의 이름을 적어주세요.")]
        public List<AtlasData> atlasRefs;
    }
}
