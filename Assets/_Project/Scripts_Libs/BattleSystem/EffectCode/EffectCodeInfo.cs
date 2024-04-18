using System;
using System.Buffers;
using System.Collections.Generic;
using CookApps.Obfuscator;
using UnityEngine;
using UnityEngine.Pool;

namespace CookApps.TeamBattle.BattleSystem
{
    public class EffectCodeInfo
    {
        public void Set(int codeId, int priority, int statsLength, double stat1, double stat2 = 0, double stat3 = 0, double stat4 = 0, double stat5 = 0, double stat6 = 0, double stat7 = 0, double stat8 = 0)
        {
            this.codeId = codeId;
            this.priority = priority;
            this.statsLength = statsLength;
            stats[0] = stat1;
            stats[1] = stat2;
            stats[2] = stat3;
            stats[3] = stat4;
            stats[4] = stat5;
            stats[5] = stat6;
            stats[6] = stat7;
            stats[7] = stat8;
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
        private ObfuscatorDouble[] stats = ArrayPool<ObfuscatorDouble>.Shared.Rent(8);
        public ObfuscatorDouble[] Stats => stats;
        private int statsLength;

        public EffectCodeInfo Clone()
        {
            var res = GenericPool<EffectCodeInfo>.Get();
            res.codeId = codeId;
            res.priority = priority;
            res.statsLength = statsLength;
            for (int i = 0; i < statsLength; i++)
            {
                res.stats[i] = stats[i];
            }
            return res;
        }
    }
}
