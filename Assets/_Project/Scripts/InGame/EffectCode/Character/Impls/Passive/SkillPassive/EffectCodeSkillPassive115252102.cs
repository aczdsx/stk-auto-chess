using System;
using CookApps.AutoBattler;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 유니 패시브
    /// 대상: 치유 대상
    /// 유니의 치유 대상의 경우 {0}초간 공격력이 {1}% 상승합니다.
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeSkillPassive115252102 : EffectCodeSkillPassiveBase
    {
        public const int CodeId = 115252102;
        public float _duration;
        public float _atkUpRate;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _duration = codeInfo.GetCodeStatToFloat(0);
            _atkUpRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _duration = codeInfo.GetCodeStatToFloat(0);
            _atkUpRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        }

        public override void OnAttackEnd(CharacterController target)
        {
            if (target.AllianceType == owner.AllianceType)
            {
                base.OnAttackEnd(target);
                Span<double> eccStats = stackalloc double[3];
                eccStats.Clear();
                eccStats[0] = CodeId;
                eccStats[1] = _duration;
                eccStats[2] = _atkUpRate;

                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_AD_PERCENT_UP, target, eccStats, source);
            }

        }
    }//115252102
}
