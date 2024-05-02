using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CookApps.TeamBattle.Utility
{
    public class SimpleObjectActiveSwapper : SimpleSwapper
    {
        [SerializeField] private SerializableDictionary<SimpleSwapType, Item> objects;

        [Serializable]
        public class Item
        {
            public List<GameObject> list;
        }

        protected override IEnumerable<SimpleSwapType> GetSwapTypes()
        {
            return objects.Keys;
        }

        protected override void Awake()
        {
            base.Awake();
            Refresh();
        }

        private void Refresh()
        {
            foreach (KeyValuePair<SimpleSwapType, Item> pair in objects)
            {
                bool isActive = pair.Key == currentType;
                foreach (GameObject e in pair.Value.list)
                {
                    e.SetActive(isActive);
                }
            }
        }

        public override void Swap(SimpleSwapType swapType)
        {
            if (currentType == swapType)
            {
                return;
            }

            currentType = swapType;

            Refresh();
        }
    }
}
