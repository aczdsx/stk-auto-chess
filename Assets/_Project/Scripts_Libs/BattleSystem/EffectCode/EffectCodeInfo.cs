using System;
using System.Buffers;
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
            statsLength = 1;
            stats = ArrayPool<ObfuscatorDouble>.Shared.Rent(statsLength);
            stats[0] = stat;
        }

        public EffectCodeInfo(int codeId, int priority, double stat1, double stat2)
        {
            this.codeId = codeId;
            this.priority = priority;
            statsLength = 2;
            stats = ArrayPool<ObfuscatorDouble>.Shared.Rent(statsLength);
            stats[0] = stat1;
            stats[1] = stat2;
        }

        public EffectCodeInfo(int codeId, int priority, double stat1, double stat2, double stat3)
        {
            this.codeId = codeId;
            this.priority = priority;
            statsLength = 3;
            stats = ArrayPool<ObfuscatorDouble>.Shared.Rent(statsLength);
            stats[0] = stat1;
            stats[1] = stat2;
            stats[2] = stat3;
        }

        public EffectCodeInfo(int codeId, int priority, double stat1, double stat2, double stat3, double stat4)
        {
            this.codeId = codeId;
            this.priority = priority;
            statsLength = 4;
            stats = ArrayPool<ObfuscatorDouble>.Shared.Rent(statsLength);
            stats[0] = stat1;
            stats[1] = stat2;
            stats[2] = stat3;
            stats[3] = stat4;
        }

        public EffectCodeInfo(int codeId, int priority, double stat1, double stat2, double stat3, double stat4, double stat5)
        {
            this.codeId = codeId;
            this.priority = priority;
            statsLength = 5;
            stats = ArrayPool<ObfuscatorDouble>.Shared.Rent(statsLength);
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
            statsLength = 6;
            stats = ArrayPool<ObfuscatorDouble>.Shared.Rent(statsLength);
            stats[0] = stat1;
            stats[1] = stat2;
            stats[2] = stat3;
            stats[3] = stat4;
            stats[4] = stat5;
            stats[5] = stat6;
        }

        public EffectCodeInfo(int codeId, int priority, int statsLength, params double[] stats)
        {
            this.codeId = codeId;
            this.priority = priority;
            this.statsLength = statsLength;
            this.stats = ArrayPool<ObfuscatorDouble>.Shared.Rent(statsLength);
            for (var i = 0; i < statsLength; i++)
            {
                this.stats[i] = stats[i];
            }
        }

        ~EffectCodeInfo()
        {
            ArrayPool<ObfuscatorDouble>.Shared.Return(stats);
        }

        public bool HasCodeStat(int idx)
        {
            return idx < statsLength;
        }

        public double GetCodeStat(int idx)
        {
            CADebug.Assert(idx < statsLength, "이펙트코드(" + codeId + ")의 없는 스텟 인덱스(" + idx + ")를 가져오려했다.");
            return stats[idx];
        }

        public float GetCodeStatToFloat(int idx)
        {
            CADebug.Assert(idx < statsLength, "이펙트코드(" + codeId + ")의 없는 스텟 인덱스(" + idx + ")를 가져오려했다.");
            return (float) stats[idx];
        }

        public int GetCodeStatToInt(int idx)
        {
            CADebug.Assert(idx < statsLength, "이펙트코드(" + codeId + ")의 없는 스텟 인덱스(" + idx + ")를 가져오려했다.");
            var ret = (int) Math.Round(stats[idx]);
            return ret;
        }

        public ObfuscatorDouble[] GetAllCodeStats()
        {
            return stats;
        }

        public void ModifyCodeStat(int idx, double value)
        {
            CADebug.Assert(idx < statsLength, "이펙트코드(" + codeId + ")의 없는 스텟 인덱스(" + idx + ")를 바꾸려했다.");
            stats[idx] = value;
        }

        public double GetLastCodeStat()
        {
            int lastIdx = statsLength - 1;
            return stats[lastIdx];
        }

        public void GetLastCodeStat(out int lastIdx, out double stat)
        {
            lastIdx = statsLength - 1;
            stat = stats[lastIdx];
        }

        private ObfuscatorInt codeId;
        public int CodeId => codeId;
        private ObfuscatorInt priority;
        public int Priority => priority;
        private ObfuscatorDouble[] stats;
        public ObfuscatorDouble[] Stats => stats;
        private int statsLength;

        public EffectCodeInfo Clone()
        {
            double[] statsArr = ArrayPool<double>.Shared.Rent(statsLength);
            Array.Copy(stats, statsArr, statsLength);
            var res = new EffectCodeInfo(codeId, priority, statsLength, statsArr);
            ArrayPool<double>.Shared.Return(statsArr);
            return res;
        }
    }
}
