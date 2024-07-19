using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [CreateAssetMenu(fileName = "VignetteMaterials", menuName = "ScriptableObjects/VignetteData")]
    public class VignetteSO : ScriptableObject
    {
        [SerializedDictionary("InGameType", "Stage Colors")]
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
