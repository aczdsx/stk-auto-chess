using System;
using System.Buffers;
using System.Collections.Generic;
using CookApps.Obfuscator;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.Pool;

namespace CookApps.BattleSystem
{
    public struct EffectCodeInfo
    {
        private ObfuscatorLong codeId;
        public long CodeId => codeId;
        public int StatsLength => statsLength;
        private int priority;
        public int Priority => priority;
        private ObfuscatorDouble stat1;
        private ObfuscatorDouble stat2;
        private ObfuscatorDouble stat3;
        private ObfuscatorDouble stat4;
        private ObfuscatorDouble stat5;
        private ObfuscatorDouble stat6;
        private ObfuscatorDouble stat7;
        private ObfuscatorDouble stat8;
        private int statsLength;

        public EffectCodeInfo(long codeId, int priority, in ReadOnlySpan<double> stats)
        {
            this.codeId = codeId;
            this.priority = priority;
            statsLength = stats.Length; // stats 배열의 실제 길이를 사용합니다.

            this.stat8 = statsLength > 7 ? stats[7] : 0;
            if (stat8 == 0 && statsLength > 7) statsLength = 7;

            this.stat7 = statsLength > 6 ? stats[6] : 0;
            if (stat7 == 0 && statsLength > 6) statsLength = 6;

            this.stat6 = statsLength > 5 ? stats[5] : 0;
            if (stat6 == 0 && statsLength > 5) statsLength = 5;

            this.stat5 = statsLength > 4 ? stats[4] : 0;
            if (stat5 == 0 && statsLength > 4) statsLength = 4;

            this.stat4 = statsLength > 3 ? stats[3] : 0;
            if (stat4 == 0 && statsLength > 3) statsLength = 3;

            this.stat3 = statsLength > 2 ? stats[2] : 0;
            if (stat3 == 0 && statsLength > 2) statsLength = 2;

            this.stat2 = statsLength > 1 ? stats[1] : 0;
            if (stat2 == 0 && statsLength > 1) statsLength = 1;

            this.stat1 = statsLength > 0 ? stats[0] : 0;
            if (stat1 == 0 && statsLength > 0) statsLength = 0;
        }

        // [TODO] 우선 -1로 임시처리 0도 애매함
        public EffectCodeInfo(long codeId, int priority, double stat1 = -1, double stat2 = -1, double stat3 = -1, double stat4 = -1, double stat5 = -1, double stat6 = -1, double stat7 = -1, double stat8 = -1)
        {
            this.codeId = codeId;
            this.priority = priority;
            statsLength = 8;
            this.stat8 = stat8;
            if (stat8 == -1) statsLength = 7;
            this.stat7 = stat7;
            if (stat7 == -1) statsLength = 6;
            this.stat6 = stat6;
            if (stat6 == -1) statsLength = 5;
            this.stat5 = stat5;
            if (stat5 == -1) statsLength = 4;
            this.stat4 = stat4;
            if (stat4 == -1) statsLength = 3;
            this.stat3 = stat3;
            if (stat3 == -1) statsLength = 2;
            this.stat2 = stat2;
            if (stat2 == -1) statsLength = 1;
            this.stat1 = stat1;
            if (stat1 == -1) statsLength = 0;
        }

        public bool HasCodeStat(int idx)
        {
            return idx < statsLength;
        }

        public double GetCodeStat(int idx)
        {
            CADebug.Assert(idx < statsLength, "이펙트코드(" + codeId + ")의 없는 스텟 인덱스(" + idx + ")를 가져오려했다.");
            return this[idx];
        }

        public float GetCodeStatToFloat(int idx)
        {
            CADebug.Assert(idx < statsLength, "이펙트코드(" + codeId + ")의 없는 스텟 인덱스(" + idx + ")를 가져오려했다.");
            return (float) this[idx];
        }

        public int GetCodeStatToInt(int idx)
        {
            CADebug.Assert(idx < statsLength, "이펙트코드(" + codeId + ")의 없는 스텟 인덱스(" + idx + ")를 가져오려했다.");
            var ret = (int) Math.Round(this[idx]);
            return ret;
        }

        public void ModifyCodeStat(int idx, double value)
        {
            CADebug.Assert(idx < statsLength, "이펙트코드(" + codeId + ")의 없는 스텟 인덱스(" + idx + ")를 바꾸려했다.");
            this[idx] = value;
        }

        public double GetLastCodeStat()
        {
            int lastIdx = statsLength - 1;
            return this[lastIdx];
        }

        public void GetLastCodeStat(out int lastIdx, out double stat)
        {
            lastIdx = statsLength - 1;
            stat = this[lastIdx];
        }

        private double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return stat1;
                    case 1: return stat2;
                    case 2: return stat3;
                    case 3: return stat4;
                    case 4: return stat5;
                    case 5: return stat6;
                    case 6: return stat7;
                    case 7: return stat8;
                    default: throw new IndexOutOfRangeException("Index is out of range!");
                }
            }
            set
            {
                switch (index)
                {
                    case 0: stat1 = value; break;
                    case 1: stat2 = value; break;
                    case 2: stat3 = value; break;
                    case 3: stat4 = value; break;
                    case 4: stat5 = value; break;
                    case 5: stat6 = value; break;
                    case 6: stat7 = value; break;
                    case 7: stat8 = value; break;
                    default: throw new IndexOutOfRangeException("Index is out of range!");
                }
            }
        }

        public EffectCodeInfo Clone()
        {
            var res = new EffectCodeInfo
            {
                codeId = codeId,
                priority = priority,
                statsLength = statsLength,
                stat1 = stat1,
                stat2 = stat2,
                stat3 = stat3,
                stat4 = stat4,
                stat5 = stat5,
                stat6 = stat6,
                stat7 = stat7,
                stat8 = stat8
            };
            return res;
        }
    }
}
