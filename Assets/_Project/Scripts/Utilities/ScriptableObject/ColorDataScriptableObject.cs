using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;


namespace CookApps.AutoBattler
{
    [CreateAssetMenu(fileName = "ColorData", menuName = "ScriptableObjects/ColorData")]
    public class ColorDataScriptableObject : ScriptableObject
    {
        [SerializedDictionaryAttribute("Color Name", "Color")]
        public SerializedDictionary<string, Color> ColorDataDic;
    }
}

