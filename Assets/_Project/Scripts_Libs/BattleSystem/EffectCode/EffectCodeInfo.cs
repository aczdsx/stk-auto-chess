using System;
using System.Collections.Generic;
using CookApps.Obfuscator;
using UnityEngine;

namespace CookApps.TeamBattle.BattleSystem
{
    public class EffectCodeInfo
    {
        public EffectCodeInfo(int codeId, int priority, double stat)
        {
            this.codeId = codeId;
            this.priority = priority;
            stats = new ObfuscatorDouble[1];
            stats[0] = stat;
        }

        public EffectCodeInfo(int codeId, int priority, double stat1, double stat2)
        {
            this.codeId = codeId;
            this.priority = priority;
            stats = new ObfuscatorDouble[2];
            stats[0] = stat1;
            stats[1] = stat2;
        }

        public EffectCodeInfo(int codeId, int priority, double stat1, double stat2, double stat3)
        {
            this.codeId = codeId;
            this.priority = priority;
            stats = new ObfuscatorDouble[3];
            stats[0] = stat1;
            stats[1] = stat2;
            stats[2] = stat3;
        }

        public EffectCodeInfo(int codeId, int priority, double stat1, double stat2, double stat3, double stat4)
        {
            this.codeId = codeId;
            this.priority = priority;
            stats = new ObfuscatorDouble[4];
            stats[0] = stat1;
            stats[1] = stat2;
            stats[2] = stat3;
            stats[3] = stat4;
        }

        public EffectCodeInfo(int codeId, int priority, double stat1, double stat2, double stat3, double stat4, double stat5)
        {
            this.codeId = codeId;
            this.priority = priority;
            stats = new ObfuscatorDouble[5];
            stats[0] = stat1;
            stats[1] = stat2;
            stats[2] = stat3;
            stats[3] = stat4;
            stats[4] = stat5;
        }

        public EffectCodeInfo(int codeId, int priority, double stat1, double stat2, double stat3, double stat4, double stat5, double stat6)
        {
            this.codeId = codeId;
            this.priority = priority;
            stats = new ObfuscatorDouble[6];
            stats[0] = stat1;
            stats[1] = stat2;
            stats[2] = stat3;
            stats[3] = stat4;
            stats[4] = stat5;
            stats[5] = stat6;
        }

        public EffectCodeInfo(int codeId, int priority, params double[] stats)
        {
            this.codeId = codeId;
            this.priority = priority;
            this.stats = new ObfuscatorDouble[stats.Length];
            for (var i = 0; i < stats.Length; i++)
            {
                this.stats[i] = stats[i];
            }
        }

        public bool HasCodeStat(int idx)
        {
            return idx < stats.Length;
        }

        public double GetCodeStat(int idx)
        {
            CADebug.Assert(idx < stats.Length, "이펙트코드(" + codeId + ")의 없는 스텟 인덱스(" + idx + ")를 가져오려했다.");
            return stats[idx];
        }

        public float GetCodeStatToFloat(int idx)
        {
            CADebug.Assert(idx < stats.Length, "이펙트코드(" + codeId + ")의 없는 스텟 인덱스(" + idx + ")를 가져오려했다.");
            return (float) stats[idx];
        }

        public int GetCodeStatToInt(int idx)
        {
            CADebug.Assert(idx < stats.Length, "이펙트코드(" + codeId + ")의 없는 스텟 인덱스(" + idx + ")를 가져오려했다.");
            var ret = (int) Math.Round(stats[idx]);
            return ret;
        }

        public double GetCodeStatToDouble(int idx)
        {
            CADebug.Assert(idx < stats.Length, "이펙트코드(" + codeId + ")의 없는 스텟 인덱스(" + idx + ")를 가져오려했다.");
            var ret = (double) Math.Round(stats[idx]);
            return ret;
        }

        public ObfuscatorDouble[] GetAllCodeStats()
        {
            return stats;
        }

        public void ModifyCodeStat(int idx, double value)
        {
            CADebug.Assert(idx < stats.Length, "이펙트코드(" + codeId + ")의 없는 스텟 인덱스(" + idx + ")를 바꾸려했다.");
            stats[idx] = value;
        }

        public void AddCodeStat(double stat)
        {
            Array.Resize(ref stats, stats.Length + 1);
            stats[^1] = stat;
        }

        public void AddCodeStats(double stat1, double stat2)
        {
            Array.Resize(ref stats, stats.Length + 1);
            stats[^2] = stat1;
            stats[^1] = stat2;
        }

        public void AddCodeStats(double stat1, double stat2, double stat3)
        {
            Array.Resize(ref stats, stats.Length + 1);
            stats[^3] = stat1;
            stats[^2] = stat2;
            stats[^1] = stat3;
        }

        public void AddCodeStats(double[] addStats)
        {
            Array.Resize(ref stats, stats.Length + addStats.Length);
            for (var i = 0; i < addStats.Length; i++)
            {
                stats[stats.Length - addStats.Length + i] = addStats[i];
            }
        }

        public void AddCodeStats(List<double> addStats)
        {
            Array.Resize(ref stats, stats.Length + addStats.Count);
            for (var i = 0; i < addStats.Count; i++)
            {
                stats[stats.Length - addStats.Count + i] = addStats[i];
            }
        }

        public double GetLastCodeStat()
        {
            int lastIdx = stats.Length - 1;
            return stats[lastIdx];
        }

        public void GetLastCodeStat(out int lastIdx, out double stat)
        {
            lastIdx = stats.Length - 1;
            stat = stats[lastIdx];
        }

        private ObfuscatorInt codeId;
        public int CodeId => codeId;
        private ObfuscatorInt priority;
        public int Priority => priority;
        private ObfuscatorDouble[] stats;
        public ObfuscatorDouble[] Stats => stats;

        public EffectCodeInfo Clone()
        {
            var statsArr = new double[stats.Length];
            Array.Copy(stats, statsArr, stats.Length);
            return new EffectCodeInfo(codeId, priority, statsArr);
        }
    }
}
