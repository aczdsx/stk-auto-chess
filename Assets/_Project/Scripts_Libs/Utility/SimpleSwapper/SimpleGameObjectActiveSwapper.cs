using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    public class SimpleGameObjectActiveSwapper : SimpleSwapper
    {
        [SerializeField] private SerializableDictionary<GameObject, Item> objects;
        [SerializeField] private SimpleSwapType currentType;

        [Serializable]
        public class Item
        {
            public List<SimpleSwapType> list;
        }

        private void Awake()
        {
            Refresh();
        }

        private void Refresh()
        {
            foreach (KeyValuePair<GameObject, Item> pair in objects)
            {
                pair.Key.SetActive(pair.Value.list.Contains(currentType));
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
