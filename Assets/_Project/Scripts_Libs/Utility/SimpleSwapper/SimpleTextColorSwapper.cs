using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(TMP_Text))]
    public class SimpleTextColorSwapper : SimpleSwapper
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private SerializableDictionary<SimpleSwapType, Color> colors;

        protected override IEnumerable<SimpleSwapType> GetSwapTypes()
        {
            return colors.Keys;
        }

        protected override void Awake()
        {
            base.Awake();
            if (text == null)
            {
                text = GetComponent<TMP_Text>();
            }

            text.color = colors[currentType];
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
            text.color = colors[swapType];
        }
    }
}
