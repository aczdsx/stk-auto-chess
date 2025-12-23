using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;

namespace CookApps.TeamBattle
{
    [CreateAssetMenu(fileName = "AtlasManager", menuName = "ScriptableObjects/AtlasManager", order = 1)]
    public class SpriteManagerScriptableObject : ScriptableObject
    {
        [HideInInspector] public List<string> folderPathGuids = new ();
        [HideInInspector] public List<AssetReferenceT<SpriteAtlas>> atlasRefs = new ();
        [HideInInspector] public SerializableDictionary<ulong, AssetReferenceT<Sprite>> spriteRefs = new ();
        [HideInInspector] public SerializableDictionary<ulong, ulong> spriteNameToAtlasDict = new ();
    }
}
