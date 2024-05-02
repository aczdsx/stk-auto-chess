using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    public class SimpleGameObjectActiveSwapper : SimpleSwapper
    {
        [SerializeField] private SerializableDictionary<GameObject, Item> objects;

        [Serializable]
        public class Item
        {
            public List<SimpleSwapType> list;
        }

        protected override IEnumerable<SimpleSwapType> GetSwapTypes()
        {
            foreach (KeyValuePair<GameObject, Item> pair in objects)
            {
                foreach (SimpleSwapType e in pair.Value.list)
                {
                    yield return e;
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
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
