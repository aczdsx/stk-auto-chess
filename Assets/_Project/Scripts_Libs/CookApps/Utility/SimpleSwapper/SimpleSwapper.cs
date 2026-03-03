using System.Collections.Generic;
using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    public enum SimpleSwapType
    {
        Normal = 0,
        Disabled = 1,


        Grade_0 = 10,
        Grade_1 = 11,
        Grade_2 = 12,
        Grade_3 = 13,
        Grade_4 = 14,
        Grade_5 = 15,
        Grade_6 = 16,
        Grade_7 = 17,
        Grade_8 = 18,
        Grade_9 = 19,

        Custom_0 = 100,
        Custom_1 = 101,
        Custom_2 = 102,
        Custom_3 = 103,
        Custom_4 = 104,
        Custom_5 = 105,
        Custom_6 = 106,
        Custom_7 = 107,
        Custom_8 = 108,
        Custom_9 = 109,
        Custom_10 = 110,
        Custom_11 = 111,
        Custom_12 = 112,
        Custom_13 = 113,
        
        Possible = 200,
        Impossible = 201,
        
        Selected = 300,
        AD =401,
        AP = 402,
        
        Fire = 501,
        Wind = 502,
        Lightning = 503,
        Earth = 504,
        Water = 505,
        Noblesse = 506,
        Troubleshooter = 507,
        Supernova = 508,
        Uroboros = 509,
        Arcana = 510,
        Eclipse = 511,
        
        Elemental = 550,
        Constellation = 551,
    }

    public abstract class SimpleSwapper : CachedMonoBehaviour
    {
        [SerializeField] protected SimpleSwapType currentType;
        protected abstract IEnumerable<SimpleSwapType> GetSwapTypes();

        protected virtual void Awake()
        {
            IEnumerable<SimpleSwapType> getSwapTypes = GetSwapTypes();
            var currentTypeContained = false;
            var defaultType = SimpleSwapType.Normal;
            var firstCheck = true;
            foreach (SimpleSwapType swapType in getSwapTypes)
            {
                if (firstCheck)
                {
                    defaultType = swapType;
                    firstCheck = false;
                }

                if (swapType == currentType)
                {
                    currentTypeContained = true;
                    break;
                }
            }

            if (!currentTypeContained)
            {
                currentType = defaultType;
            }
        }

        public abstract void Swap(SimpleSwapType swapType);
    }

    public static class SimpleSwapperExtensions
    {
        public static void Swap(this IEnumerable<SimpleSwapper> swappers, SimpleSwapType swapType)
        {
            foreach (SimpleSwapper swapper in swappers)
            {
                swapper.Swap(swapType);
            }
        }
    }
}
