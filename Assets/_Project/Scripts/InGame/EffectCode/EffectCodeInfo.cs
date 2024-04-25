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
        private ObfuscatorInt codeId;
        public int CodeId => codeId;
        private ObfuscatorInt priority;
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

        public EffectCodeInfo(int codeId, int priority, int statsLength, double stat1, double stat2 = 0, double stat3 = 0, double stat4 = 0, double stat5 = 0, double stat6 = 0, double stat7 = 0, double stat8 = 0)
        {
            this.codeId = codeId;
            this.priority = priority;
            this.statsLength = statsLength;
            this.stat1 = stat1;
            this.stat2 = stat2;
            this.stat3 = stat3;
            this.stat4 = stat4;
            this.stat5 = stat5;
            this.stat6 = stat6;
            this.stat7 = stat7;
            this.stat8 = stat8;
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
