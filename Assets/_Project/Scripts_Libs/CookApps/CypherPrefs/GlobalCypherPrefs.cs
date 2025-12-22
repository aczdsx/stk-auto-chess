using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookApps.CypherPrefs
{
    public static class GlobalCypherPrefs
    {
        private static Dictionary<string, Preference> preferences = new();
        
        internal static void RegisterPreference(Preference preference)
        {
            if (preference == null)
                throw new System.ArgumentNullException(nameof(preference));

            var key = preference.PreferenceKey;
            if (!preferences.TryAdd(key, preference))
            {
                throw new System.ArgumentException($"Preference with key '{key}' is already registered.");
            }
        }
        
        public static T GetPreference<T>(string key) where T : Preference
        {
            if (preferences.TryGetValue(key, out var preference))
            {
                if (preference is not T res)
                {
                    throw new InvalidCastException($"Preference with key '{key}' is not of type {typeof(T).Name}.");
                }
                return res;
            }

            return null;
        }
        
        public static void SaveAll()
        {
            foreach (var preference in preferences.Values)
            {
                preference.Save();
            }
        }
        
        public static async Awaitable LoadAll()
        {
            foreach (var preference in preferences.Values)
            {
                preference.Load();
                await Awaitable.NextFrameAsync();
            }
        }
        
        public static void DeleteAll()
        {
            foreach (var preference in preferences.Values)
            {
                preference.Delete();
            }
            preferences.Clear();
        }
    }
}

