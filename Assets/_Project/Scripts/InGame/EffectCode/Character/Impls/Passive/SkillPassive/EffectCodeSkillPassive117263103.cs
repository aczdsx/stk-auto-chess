using System;
using CookApps.AutoBattler;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 루키다 패시브
    /// 대상: 자기자신
    /// 일반공격 시 {0}% 확률로 여우불을 획득합니다. 
    /// 여우불의 갯수 당 {1}%의 추가 피해를 입힙니다. 
    /// 여우불은 개별 지속시간을 가지며 {2}초간 유지됩니다.
    /// </summary>
    [UseEffectCodeIds((int)CodeId)]
    public partial class EffectCodeSkillPassive117263103 : EffectCodeSkillPassiveBase
    {
        public const long CodeId = 117263103;


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
            long skillEffectCodeId = base.GetOnlyActiveSkillEffectCodeId(CodeId);

            Span<double> buffStats = stackalloc double[5];
            buffStats.Clear();
            buffStats[0] = CodeId;
            buffStats[1] = codeInfo.GetCodeStatToFloat(0);
            buffStats[2] = codeInfo.GetCodeStatToFloat(1) * 0.01f;
            buffStats[3] = codeInfo.GetCodeStatToFloat(2);
            buffStats[4] = skillEffectCodeId;

            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_SPECIAL_RUKIDA_FOXFIRE, owner, buffStats, source);
        }
       
    }
}//117323201
