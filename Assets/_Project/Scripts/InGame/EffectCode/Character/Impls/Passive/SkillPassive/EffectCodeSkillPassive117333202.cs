using System;
using CookApps.AutoBattler;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 에이프릴릴 패시브
    /// 대상: 자기 자신
    /// 에이프릴이 공격위치를 유지할수록 {0}초당 공격속도가 {1}% 상승합니다. 이 효과는 최대 60%를 넘길 수 없습니다.
    /// 이동할 경우 이 증가분은 천천히 하락합니다.
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeSkillPassive117333202 : EffectCodeSkillPassiveBase
    {
        public const int CodeId = 117333202;

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
            buffStats[1] = codeInfo.GetCodeStatToFloat(0);// increase time
            buffStats[2] = codeInfo.GetCodeStatToFloat(1) * 0.01f;//_attackSpeedIncreaseRate

            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_SPECIAL_APRIL_STANDER, owner, buffStats, source);
        }

    }
}//117333202
