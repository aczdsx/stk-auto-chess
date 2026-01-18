using System.Collections.Generic;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [CreateAssetMenu(fileName = "VignetteMaterials", menuName = "ScriptableObjects/VignetteData")]
    public class VignetteSO : ScriptableObject
    {
        public List<MaterialColorPair> stageColors;
    }

    [System.Serializable]
    public class MaterialColorPair
    {
        public InGameType InGameType;
        public int ID;
        public Material Material;
        public Color Color;
    }
}
