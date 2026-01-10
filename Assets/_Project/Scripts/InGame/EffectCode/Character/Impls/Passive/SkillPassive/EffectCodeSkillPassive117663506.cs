using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Mono.Cecil.Cil;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 시라유키 패시브
    /// 대상: 자기 자신
    /// 회피에 성공할 경우 회피율이 {0}% 증가합니다.(최대 {1}회 중첩) 
    /// 상대의 공격을 회피할 경우 공격 대상에게 {2}%의 위력을 가진 참격을 날려 반격합니다.
    /// </summary>
    [UseEffectCodeIds((int)CodeId)]
    public partial class EffectCodeSkillPassive117663506 : EffectCodeSkillPassiveBase
    {
        public const long CodeId = 117663506;


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

        private void InjectPassiveBuff(EffectCodeInfo codeInfo, IEffectCodeSource source )
        {
            Span<double> buffStats = stackalloc double[4];
            buffStats.Clear();
            buffStats[0] = codeId;
            buffStats[1] = codeInfo.GetCodeStatToFloat(0) * 0.01f;// _avoidSuccessRatePercent
            buffStats[2] = codeInfo.GetCodeStatToInt(1);//_avoidSuccessMaxCount
            buffStats[3] = codeInfo.GetCodeStatToFloat(2) * 0.01f;//_DamageRatePercent

            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_SHIRAYUKI_AVOID_AND_ATTACK, owner, buffStats, source);
        }
    }
}//115362202
