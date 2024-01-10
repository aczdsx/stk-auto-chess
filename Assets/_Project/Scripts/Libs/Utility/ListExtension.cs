using System.Collections.Generic;

namespace CookApps.TeamBattle.Utility
{
    public static class ListExtension
    {
        public static int RemoveAllNull<T>(this List<T> list)
        {
            return list.RemoveAll(NullCheck);
        }

        private static bool NullCheck<T>(T x)
        {
            return x == null;
        }
    }
}
