using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CookApps.Obfuscator;
using Cysharp.Text;
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

    public static class NullChecker<T> where T : class
    {
        public static Predicate<T> NullCheck = x => x == null;
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

        public static ulong djb2Hash(this string s)
        {
            ulong hash = 5381;
            for (var i = 0; i < s.Length; i++)
            {
                hash = (hash << 5) + hash + s[i];
            }

            return hash;
        }

        public static ulong djb2HashCaseInsensitive(this string s)
        {
            ulong hash = 5381;
            for (var i = 0; i < s.Length; i++)
            {
                hash = ((hash << 5) + hash) ^ (s[i] & ~0x20UL);
            }

            return hash;
        }

        public static string ToInvariantString<T>(this T obj) where T : unmanaged
        {
            switch (obj)
            {
                case sbyte value: return value.ToString(CultureInfo.InvariantCulture);
                case byte value: return value.ToString(CultureInfo.InvariantCulture);
                case short value: return value.ToString(CultureInfo.InvariantCulture);
                case ushort value: return value.ToString(CultureInfo.InvariantCulture);
                case int value: return value.ToString(CultureInfo.InvariantCulture);
                case uint value: return value.ToString(CultureInfo.InvariantCulture);
                case long value: return value.ToString(CultureInfo.InvariantCulture);
                case ulong value: return value.ToString(CultureInfo.InvariantCulture);
                case char value: return value.ToString(CultureInfo.InvariantCulture);
                case float value: return value.ToString(CultureInfo.InvariantCulture);
                case double value: return value.ToString(CultureInfo.InvariantCulture);
                case decimal value: return value.ToString(CultureInfo.InvariantCulture);
                case bool value: return value.ToString(CultureInfo.InvariantCulture);
            }

            return obj.ToString();
        }

        public static string ToCommaString(this long value)
        {
            return ZString.Format("{0:#,##0}", value);
        }

        public static string ToCommaString(this ObfuscatorLong value)
        {
            return ZString.Format("{0:#,##0}", value);
        }

        public static string ToCommaString(this int value)
        {
            return ZString.Format("{0:#,##0}", value);
        }

        public static string ToCommaString(this ObfuscatorInt value)
        {
            return ZString.Format("{0:#,##0}", value);
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
