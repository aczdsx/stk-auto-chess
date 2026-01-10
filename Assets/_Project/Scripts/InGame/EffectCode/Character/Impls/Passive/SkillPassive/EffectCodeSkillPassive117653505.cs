using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 엔키 패시브
    /// 대상: 자기 자신
    /// 엔키가 아군을 치유할 때 마다 ‘조류’중첩 1 획득(최대 {0})
    /// 중첩 1당 엔키의 치유량이 {1}% 증가하며 전투 종료 시 까지 유지됩니다. 피격 시 조류 중첩이 1 차감됩니다.
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeSkillPassive117653505 : EffectCodeSkillPassiveBase
    {
        public const int CodeId = 117653505;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            InjectEnkiPassiveHealUp(codeInfo, source);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            InjectEnkiPassiveHealUp(codeInfo, source);
        }

        private void InjectEnkiPassiveHealUp(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            Span<double> buffStats = stackalloc double[3];
            buffStats.Clear();
            buffStats[0] = codeId;
            buffStats[1] = codeInfo.GetCodeStatToInt(0);
            buffStats[2] = codeInfo.GetCodeStatToFloat(1) * 0.01f;

            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_ENKI_PASSIVE_HEALUP, owner, buffStats, source);
        }

        
    }//117513401
}
