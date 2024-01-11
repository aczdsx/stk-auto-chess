using TMPro;
using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(TMP_Text))]
    public class SimpleTextColorSwapper : SimpleSwapper
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private SerializableDictionary<SimpleSwapType, Color> colors;
        [SerializeField] private SimpleSwapType currentType;

        private void Awake()
        {
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
