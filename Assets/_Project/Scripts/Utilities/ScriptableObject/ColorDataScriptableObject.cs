using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;


namespace CookApps.AutoBattler
{
    [CreateAssetMenu(fileName = "ColorData", menuName = "ScriptableObjects/ColorData")]
    public class ColorDataScriptableObject : ScriptableObject
    {
        [SerializedDictionary("Color Name", "Color")]
        public SerializedDictionary<string, Color> StandardColorDataDic;

        [SerializedDictionaryAttribute("Color Name", "Color")]
        public SerializedDictionary<string, Color> GaugeColorDataDic;

        [SerializedDictionary("Color Name", "Gradient")]
        public SerializedDictionary<string, Gradient> GaugeColorGradientDataDic;
    }
}

