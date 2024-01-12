using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(Tilemap))]
    public class SimpleTilemapColorSwapper : SimpleSwapper
    {
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private SerializableDictionary<SimpleSwapType, Color> colors;
        [SerializeField] private SimpleSwapType currentType;

        private void Awake()
        {
            if (tilemap == null)
            {
                tilemap = GetComponent<Tilemap>();
            }

            tilemap.color = colors[currentType];
        }

        public override void Swap(SimpleSwapType swapType)
        {
            if (currentType == swapType)
            {
                return;
            }

            if (!colors.ContainsKey(swapType))
            {
                return;
            }

            currentType = swapType;
            tilemap.color = colors[swapType];
        }
    }
}
