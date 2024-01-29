using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleSpriteSwapper : SimpleSwapper
    {
        [SerializeField] private SpriteRenderer image;
        [SerializeField] private SerializableDictionary<SimpleSwapType, Sprite> sprites;

        protected override IEnumerable<SimpleSwapType> GetSwapTypes()
        {
            return sprites.Keys;
        }

        protected override void Awake()
        {
            base.Awake();
            if (image == null)
            {
                image = GetComponent<SpriteRenderer>();
            }

            image.sprite = sprites[currentType];
        }

        public override void Swap(SimpleSwapType swapType)
        {
            if (currentType == swapType)
            {
                return;
            }

            if (!sprites.ContainsKey(swapType))
            {
                return;
            }

            currentType = swapType;
            image.sprite = sprites[swapType];
        }
    }
}
