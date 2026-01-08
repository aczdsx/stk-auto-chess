using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 마리에 패시브브
    /// 범위: 공격 대상
    /// 대상에게 크리티컬 히트 적중 시 표식-아라크네를 남깁니다. 표식 아라크네의 중첩이 {0}가 되면 대상이 {1}초간 기절 상태가 됩니다.
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeSkillPassive117563405 : EffectCodeCharacterBase
    {
        public const int CodeId = 117563405;
        public int _overlapCount;
        public float _debuffDuration;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _overlapCount = codeInfo.GetCodeStatToInt(0);
            _debuffDuration = codeInfo.GetCodeStatToFloat(1);

        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _overlapCount = codeInfo.GetCodeStatToInt(0);
            _debuffDuration = codeInfo.GetCodeStatToFloat(1);
        }

        // public override void OnCritical(CharacterController target)
        // {
        //     base.OnCritical(target);
        //     Span<double> eccStats = stackalloc double[4];
        //     eccStats.Clear();
        //     eccStats[0] = CodeId;
        //     eccStats[1] = 1;
        //     eccStats[2] = _overlapCount;
        //     eccStats[3] = _debuffDuration;

        //     EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_MARIE_ARACNE, target, eccStats, source);
        // }
        public override void OnAttackEnd(CharacterController target)
        {
            base.OnAttackEnd(target);
            Span<double> eccStats = stackalloc double[4];
            eccStats.Clear();
            eccStats[0] = CodeId;
            eccStats[1] = 1;
            eccStats[2] = _overlapCount;
            eccStats[3] = _debuffDuration;

            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_MARIE_ARACNE, target, eccStats, source);
        }
    }//117613501
}
