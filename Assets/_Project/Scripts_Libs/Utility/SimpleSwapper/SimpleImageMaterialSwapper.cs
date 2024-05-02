using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(Image))]
    public class SimpleImageMaterialSwapper : SimpleSwapper
    {
        [SerializeField] private Image image;
        [SerializeField] private SerializableDictionary<SimpleSwapType, Material> materials;

        protected override IEnumerable<SimpleSwapType> GetSwapTypes()
        {
            return materials.Keys;
        }

        protected override void Awake()
        {
            base.Awake();
            if (image == null)
            {
                image = GetComponent<Image>();
            }

            materials.TryGetValue(currentType, out Material material);
            image.material = material;
        }

        public override void Swap(SimpleSwapType swapType)
        {
            if (currentType == swapType)
            {
                return;
            }

            currentType = swapType;
            materials.TryGetValue(currentType, out Material material);
            image.material = material;
        }
    }
}
