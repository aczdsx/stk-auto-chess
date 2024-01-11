using System;
using System.Collections.Generic;
using CookApps.Obfuscator;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Random = System.Random;

namespace CookApps.TeamBattle.BattleSystem
{
    public enum GlobalRandomType
    {
        Universal,
        MAX,
    }

    public class InGameRandomManager : Singleton<InGameRandomManager>
    {
        public InGameRandomManager()
        {
            TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            seedGenerator = new Random((int) timeSpan.TotalSeconds);
            for (var i = 0; i < globalRandomTypes.Length; i++)
            {
                globalRandoms.Add(globalRandomTypes[i], new Random(seedGenerator.Next()));
            }
        }

        #region for Sequential Random
        private Random seedGenerator;

        public void ResetRandomSeedGenerator(int seed)
        {
            seedGenerator = new Random(seed);
        }

        public Random GetRandom()
        {
            return new Random(seedGenerator.Next());
        }
        #endregion

        #region Globally managed random
        private GlobalRandomType[] globalRandomTypes =
        {
            GlobalRandomType.Universal,
        };

        private Dictionary<GlobalRandomType, Random> globalRandoms = new ();

        public void ResetGlobalRandomSeed(int[] seedList)
        {
            for (var i = 0; i < seedList.Length; i++)
            {
                globalRandoms[globalRandomTypes[i]] = new Random(seedList[i]);
            }
        }

        public static Random GetGlobalRandom(GlobalRandomType type)
        {
            return Instance.globalRandoms[type];
        }

        public static int GetGlobalRandomValue(GlobalRandomType type)
        {
            return Instance.globalRandoms[type].Next();
        }

        public static int GetGlobalRandomValue(GlobalRandomType type, int max)
        {
            return Instance.globalRandoms[type].Next(max);
        }

        public static int GetGlobalRandomValue(GlobalRandomType type, int min, int max)
        {
            return Instance.globalRandoms[type].Next(min, max);
        }

        public static float GetGlobalRandomValue(GlobalRandomType type, float max)
        {
            return Instance.globalRandoms[type].Next(max);
        }

        public static float GetGlobalRandomValue(GlobalRandomType type, float min, float max)
        {
            return Instance.globalRandoms[type].Next(min, max);
        }

        public static void GetGlobalShuffle<T>(GlobalRandomType type, IList<T> targetList)
        {
            Instance.globalRandoms[type].Shuffle(targetList);
        }

        public static Vector2 GetGlobalRandomVector2InsideCircle(GlobalRandomType type, float minRad, float maxRad)
        {
            float degree = GetGlobalRandomValue(type, 0f, 360f);
            float distance = GetGlobalRandomValue(type, minRad, maxRad);
            Vector2 unitVector = Vector2.left.Rotate(degree);
            return unitVector * distance;
        }

        public static Vector2 GetGlobalRandomVector2InsideCircle(GlobalRandomType type, float maxRad)
        {
            return GetGlobalRandomVector2InsideCircle(type, 0f, maxRad);
        }

        public static Vector2 GetGlobalRandomVector2InsideRect(GlobalRandomType type, Rect rect)
        {
            float x = GetGlobalRandomValue(type, rect.xMin, rect.xMax);
            float y = GetGlobalRandomValue(type, rect.yMin, rect.yMax);
            return new Vector2(x, y);
        }

        public static int GetUniversalRandomValue()
        {
            return GetGlobalRandomValue(GlobalRandomType.Universal);
        }

        public static int GetUniversalRandomValue(int max)
        {
            return GetGlobalRandomValue(GlobalRandomType.Universal, max);
        }

        public static int GetUniversalRandomValue(int min, int max)
        {
            return GetGlobalRandomValue(GlobalRandomType.Universal, min, max);
        }

        public static float GetUniversalRandomValue(float max)
        {
            return GetGlobalRandomValue(GlobalRandomType.Universal, max);
        }

        public static float GetUniversalRandomValue(float min, float max)
        {
            return GetGlobalRandomValue(GlobalRandomType.Universal, min, max);
        }

        public static void UniversalShuffle<T>(IList<T> targetList)
        {
            GetGlobalShuffle(GlobalRandomType.Universal, targetList);
        }

        public static Vector2 GetUniversalRandomVector2InsideCircle(float minRad, float maxRad)
        {
            return GetGlobalRandomVector2InsideCircle(GlobalRandomType.Universal, minRad, maxRad);
        }

        public static Vector2 GetUniversalRandomVector2InsideCircle(float maxRad)
        {
            return GetGlobalRandomVector2InsideCircle(GlobalRandomType.Universal, maxRad);
        }

        public static Vector2 GetUniversalRandomVector2InsideRect(Rect rect)
        {
            return GetGlobalRandomVector2InsideRect(GlobalRandomType.Universal, rect);
        }
        #endregion

        #region Static Random
        private Dictionary<int, ObfuscatorInt> staticRandomSeeds = new ();
        private Dictionary<int, Random> staticRandoms = new ();

        public JArray ToJsonStaticRandomSeed()
        {
            var jArr = new JArray();
            foreach (KeyValuePair<int, ObfuscatorInt> pair in staticRandomSeeds)
            {
                var jObj = new JObject();
                jObj.Add("key", pair.Key);
                jObj.Add("seed", (int) pair.Value);
                jArr.Add(jObj);
            }

            return jArr;
        }

        public void AddStaticRandomSeed(int key)
        {
            if (!staticRandomSeeds.ContainsKey(key))
            {
                staticRandomSeeds.Add(key, GetUniversalRandomValue());
            }
        }

        public void AddStaticRandomSeed(int key, int seed)
        {
            if (!staticRandomSeeds.ContainsKey(key))
            {
                staticRandomSeeds.Add(key, seed);
            }
            else
            {
                staticRandomSeeds[key] = seed;
            }
        }

        private Random GetStaticRandom(int key)
        {
            if (!staticRandomSeeds.ContainsKey(key))
            {
                AddStaticRandomSeed(key);
            }

            Random random;
            if (staticRandoms.ContainsKey(key))
            {
                random = new Random(staticRandomSeeds[key]);
                staticRandoms[key] = random;
            }
            else
            {
                random = new Random(staticRandomSeeds[key]);
                staticRandoms.Add(key, random);
            }

            return random;
        }

        public static int GetStaticRandomValue(int key)
        {
            Random random = Instance.GetStaticRandom(key);
            int res = random.Next();
            Instance.AddStaticRandomSeed(key, random.Next());
            return res;
        }

        public static int GetStaticRandomValue(int key, int max)
        {
            Random random = Instance.GetStaticRandom(key);
            int res = random.Next(max);
            Instance.AddStaticRandomSeed(key, random.Next());
            return res;
        }

        public static int GetStaticRandomValue(int key, int min, int max)
        {
            Random random = Instance.GetStaticRandom(key);
            int res = random.Next(min, max);
            Instance.AddStaticRandomSeed(key, random.Next());
            return res;
        }

        public static float GetStaticRandomValue(int key, float min, float max)
        {
            Random random = Instance.GetStaticRandom(key);
            float res = random.Next(min, max);
            Instance.AddStaticRandomSeed(key, random.Next());
            return res;
        }

        public static void GetStaticShuffle<T>(int key, IList<T> targetList)
        {
            Random random = Instance.GetStaticRandom(key);
            random.Shuffle(targetList);
            Instance.AddStaticRandomSeed(key, random.Next());
        }
        #endregion
    }
}
