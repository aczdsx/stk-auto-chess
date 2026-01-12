using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Unity.VisualScripting;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 블린 패시브
    /// 범위: 자기 자신
    /// 에스퍼의 효과로 폭격(3*3)피해를 입힌 대상에 따라 ‘열기’중첩을 획득합니다. 
    /// 열기 중첩이 {0}회가 되면 다음 공격이 강화 됩니다.
    /// #폭염: 3*3 범위에 블린에 공격력의 {1}%에 해당하는 피해를 입히고 {2}초간 {3}%의 위력의 지속피해를 입히는 불지대를 만듭니다. 
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeSkillPassive117243102 : EffectCodeSkillPassiveBase
    {
        public const int CodeId = 117243102;
       
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
            
            Span<double> buffStats = stackalloc double[5];
            buffStats.Clear();
            buffStats[0] = CodeId;
            buffStats[1] = codeInfo.GetCodeStatToFloat(0);
            buffStats[2] = codeInfo.GetCodeStatToFloat(1) * 0.01f;
            buffStats[3] = codeInfo.GetCodeStatToFloat(2);
            buffStats[4] = codeInfo.GetCodeStatToFloat(3) * 0.01f;

            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_SPECIAL_BLIN_HEAT, owner, buffStats, source);
        }

    

    }
}//117243102
