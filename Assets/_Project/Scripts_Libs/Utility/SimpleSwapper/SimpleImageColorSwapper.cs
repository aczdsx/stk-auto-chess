using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(Image))]
    public class SimpleImageColorSwapper : SimpleSwapper
    {
        [SerializeField] private Image image;
        [SerializeField] private SerializableDictionary<SimpleSwapType, Color> colors;

        protected override IEnumerable<SimpleSwapType> GetSwapTypes()
        {
            return colors.Keys;
        }

        protected override void Awake()
        {
            base.Awake();
            if (image == null)
            {
                image = GetComponent<Image>();
            }

            image.color = colors[currentType];
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
            image.color = colors[swapType];
        }
    }
}
