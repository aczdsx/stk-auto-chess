using System;
using CookApps.AutoBattler;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 오데트 패시브
    /// 범위: 공격 대상
    /// 일반 공격 시 적에게 [한기]를 중첩시킵니다.
    /// 한기가 {0}회 중첩 시 한기를 소모해 {1}초간 빙결 상태가 됩니다.
    /// #빙결: {1}초간 행동 불가, 스킬 사용 불가
    /// #한기 지속시간: 영구
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeSkillPassive117613501 : EffectCodeSkillPassiveBase
    {
        public const int CodeId = 117613501;
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

        public override void OnAttack()
        {
            base.OnAttack();
            Span<double> eccStats = stackalloc double[4];
            eccStats.Clear();
            eccStats[0] = CodeId;
            eccStats[1] = 1;
            eccStats[2] = _overlapCount;
            eccStats[3] = _debuffDuration;

            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_SPECIAL_ODETTE_COLD, owner.Target, eccStats, source);
        }
    }//115252102
}
