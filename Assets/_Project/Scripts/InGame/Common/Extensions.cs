using System;
using System.Collections.Generic;

namespace CookApps.BattleSystem
{
    public static class AnimationEnumExtensions
    {
        private static Dictionary<int, string> cachedAnimKeyStringMap = new ();
        private static Dictionary<string, int> cachedAnimStringKeyMap = new ();

        public static string ToAnimationName(this AnimationKey key)
        {
            var index = (int) key;
            if (!cachedAnimKeyStringMap.ContainsKey(index))
            {
                cachedAnimKeyStringMap.Add(index, key.ToString());
            }

            return cachedAnimKeyStringMap[index];
        }

        public static AnimationKey ToAnimationKey(this string name)
        {
            if (!cachedAnimStringKeyMap.ContainsKey(name))
            {
                cachedAnimStringKeyMap.Add(name, (int) Enum.Parse(typeof(AnimationKey), name));
            }

            return (AnimationKey) cachedAnimStringKeyMap[name];
        }

        private static Dictionary<int, string> cachedInGameEffectAnimKeyStringMap = new ();

        public static string ToAnimationName(this InGameEffectAnimationKey key)
        {
            var index = (int) key;
            if (!cachedInGameEffectAnimKeyStringMap.ContainsKey(index))
            {
                cachedInGameEffectAnimKeyStringMap.Add(index, key.ToString());
            }

            return cachedInGameEffectAnimKeyStringMap[index];
        }
    }
}
