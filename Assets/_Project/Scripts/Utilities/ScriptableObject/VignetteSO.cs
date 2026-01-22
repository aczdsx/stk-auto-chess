using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoBattler
{
    [CreateAssetMenu(fileName = "VignetteMaterials", menuName = "ScriptableObjects/VignetteData")]
    public class VignetteSO : ScriptableObject
    {
        public List<MaterialColorPair> stageColors;
        
        public async UniTask LoadAssetsAsync()
        {
            foreach (var color in stageColors)
            {
                if (color.Material.RuntimeKeyIsValid())
                    await color.Material.LoadAssetAsync();
            }
        }
    }

    [System.Serializable]
    public class MaterialColorPair
    {
        public InGameType InGameType;
        public int ID;
        public AssetReferenceT<Material> Material;
        public Color Color;
    }
}
