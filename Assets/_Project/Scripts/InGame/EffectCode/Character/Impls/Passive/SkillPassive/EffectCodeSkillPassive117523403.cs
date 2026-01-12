using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Mono.Cecil.Cil;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 아드리아 패시브
    /// 대상: 자기 자신
    /// 3*3 범위에 있는 적들의 수에 따라 자신의 물리/마법 저항력이 {0}% 상승하고, {1}% 만큼 치유력이 올라갑니다.
    /// </summary>
    [UseEffectCodeIds((int)CodeId)]
    public partial class EffectCodeSkillPassive117523403 : EffectCodeSkillPassiveBase
    {
        public const long CodeId = 117523403;


        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            InjectPassiveBuff(codeInfo, source);

        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            InjectPassiveBuff(codeInfo, source);
        }

        private void InjectPassiveBuff(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            Span<double> buffStats = stackalloc double[3];
            buffStats.Clear();
            buffStats[0] = codeId;
            buffStats[1] = codeInfo.GetCodeStatToFloat(0) * 0.01f;
            buffStats[2] = codeInfo.GetCodeStatToFloat(1) * 0.01f;

            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_SPECIAL_ADRIA_PASSIVE_TEAM_HELP, owner, buffStats, source);
        }

        
    }
}//117523403
