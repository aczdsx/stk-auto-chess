using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 클레이이 패시브
    /// 범위: 치유 대상
    /// 클레이의 일반 공격으로 치유된 아군의 공격력과 방어력이 {0}% 증가하며, {1}초 동안 유지됩니다.
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeSkillPassive117553404 : EffectCodeCharacterBase
    {
        public const int CodeId = 117553404;
        private float _attackResUpRate;
        private float _duration;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _attackResUpRate = codeInfo.GetCodeStatToFloat(0);
            _duration = codeInfo.GetCodeStatToFloat(1);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _attackResUpRate = codeInfo.GetCodeStatToFloat(0);
            _duration = codeInfo.GetCodeStatToFloat(1);
        }

        public override void OnAttackEnd(CharacterController target)
        {
            base.OnAttackEnd(target);
            if (target.AllianceType == owner.AllianceType)
            {
                var atkbuffNameType = target.SpecCharacter.atk_type == AtkType.AD ? EffectCodeNameType.BUFF_AD_PERCENT_UP :
                EffectCodeNameType.BUFF_AP_PERCENT_UP;
                Span<double> eccStats = stackalloc double[3];
                eccStats.Clear();
                eccStats[0] = codeId;
                eccStats[1] = _duration;
                eccStats[2] = _attackResUpRate;

                EffectCodeHelper.AddOrMergeEffectCode(atkbuffNameType, target, eccStats, source);

                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_DEF_PERCENT_UP, target, eccStats, source);
            }
        }

    
    }
}
