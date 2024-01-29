using System;
using System.Linq;
using CookApps.Obfuscator;
using UnityEngine;

namespace CookApps.SampleTeamBattle
{
    public static class SpecOptionCache
    {
        private static ObfuscatorInt? deckMaxSize;
        public static int DeckMaxSize => deckMaxSize ??= GetOptionFromSpec_Int("DECK_MAX_SIZE", 5);

        private static ObfuscatorInt? deckLineMaxSize;
        public static int DeckLineMaxSize => deckLineMaxSize ??= GetOptionFromSpec_Int("DECK_LINE_MAX_SIZE", 3);

        #region Getters
        private static int GetOptionFromSpec_Int(string optionName, int defaultData)
        {
            SpecOption data = SpecDataManager.Instance.SpecOption.Get(optionName);

            if (int.TryParse(data?.value, out int result))
            {
                return result;
            }

            return defaultData;
        }

        private static ObfuscatorInt[] GetOptionFromSpec_IntArray(string optionName, ObfuscatorInt[] defaultData, char separator = '|')
        {
            SpecOption data = SpecDataManager.Instance.SpecOption.Get(optionName);

            if (data == null)
            {
                return defaultData;
            }

            try
            {
                ObfuscatorInt[] arr = data.value.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => (ObfuscatorInt) int.Parse(x)).ToArray();
                return arr;
            }
            catch (Exception e)
            {
                Debug.LogError($"SpecOption {optionName} cannot parse to int array! value = {data}");
            }

            return defaultData;
        }

        private static float GetOptionFromSpec_Float(string optionName, float defaultData)
        {
            SpecOption data = SpecDataManager.Instance.SpecOption.Get(optionName);

            if (float.TryParse(data?.value, out float result))
            {
                return result;
            }

            return defaultData;
        }
        #endregion
    }
}
