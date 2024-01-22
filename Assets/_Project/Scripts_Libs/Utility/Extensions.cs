using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace CookApps.TeamBattle.Utility
{
    public static class Vector2Extension
    {
        public static Vector2 Rotate(this Vector2 v, float degree)
        {
            return Quaternion.Euler(0, 0, degree) * v;
        }
    }

    public static class Extensions
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

    public static class RandomExtensions
    {
        public static float Next(this Random random, float min, float max)
        {
            return (float) (random.NextDouble() * (max - min)) + min;
        }

        public static float Next(this Random random, float max)
        {
            return (float) (random.NextDouble() * max);
        }

        public static long Next(this Random random, long min, long max)
        {
            if (max <= min)
            {
                throw new ArgumentOutOfRangeException(nameof(max), "max must be > min!");
            }

            var uRange = (ulong) (max - min);
            ulong back = (uint) random.Next();
            ulong front = (uint) random.Next();
            ulong ulongRand = (front << 32) | back;
            return (long) (ulongRand % uRange) + min;
        }

        public static void Shuffle<T>(this Random random, IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        public static Vector2 InsideCircle(this Random random, float maxRad)
        {
            float degree = random.Next(0f, 360f);
            float distance = random.Next(0f, maxRad);
            Vector2 unitVector = Vector2.left.Rotate(degree);
            return unitVector * distance;
        }
    }

    public static class StringExtensions
    {
        public static Color HexColor(this string hexCode)
        {
            if (ColorUtility.TryParseHtmlString(hexCode, out Color color))
            {
                return color;
            }

            return Color.white;
        }

        public static Color SetAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }

    public static class InheritHelper
    {
        public static Type[] GetAllImplementations<T>()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(T).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract)
                .ToArray();
        }
    }
}
