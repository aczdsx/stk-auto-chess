using System.Collections.Generic;

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
    }

    public abstract class SimpleSwapper : CachedMonoBehaviour
    {
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
