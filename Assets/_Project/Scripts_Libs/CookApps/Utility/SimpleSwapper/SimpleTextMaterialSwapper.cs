using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(TMP_Text))]
    public class SimpleTextMaterialSwapper : SimpleSwapper
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private SerializableDictionary<SimpleSwapType, Material> materials;

        protected override IEnumerable<SimpleSwapType> GetSwapTypes()
        {
            return materials.Keys;
        }

        protected override void Awake()
        {
            base.Awake();
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
