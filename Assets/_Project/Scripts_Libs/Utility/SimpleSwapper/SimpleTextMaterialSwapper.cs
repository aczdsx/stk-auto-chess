using TMPro;
using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(TMP_Text))]
    public class SimpleTextMaterialSwapper : SimpleSwapper
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private SerializableDictionary<SimpleSwapType, Material> materials;
        [SerializeField] private SimpleSwapType currentType;

        private void Awake()
        {
            if (text == null)
            {
                text = GetComponent<TMP_Text>();
            }

            text.fontMaterial = materials[currentType];
        }

        public override void Swap(SimpleSwapType swapType)
        {
            if (currentType == swapType)
            {
                return;
            }

            if (!materials.ContainsKey(swapType))
            {
                return;
            }

            currentType = swapType;
            text.fontMaterial = materials[swapType];
        }
    }
}
