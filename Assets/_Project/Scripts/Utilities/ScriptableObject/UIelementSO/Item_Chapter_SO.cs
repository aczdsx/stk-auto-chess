using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [CreateAssetMenu(fileName = "Item_Chapter_SO", menuName = "ScriptableObjects/UIelementData/Item_Chapter")]
    public class Item_Chapter_SO : ScriptableObject
    {
        [SerializedDictionary("Name", "float")]
        public SerializedDictionary<string, float> BgHeight;

        [SerializedDictionary("Color Name", "Color")]
        public SerializedDictionary<string, Color> TextColors;

        [SerializedDictionary("Color Name", "Color")]
        public SerializedDictionary<string, Color> FloorColors;

        [SerializedDictionary("Color Name", "Color")]
        public SerializedDictionary<string, Gradient> PillarColors;




    }
}
