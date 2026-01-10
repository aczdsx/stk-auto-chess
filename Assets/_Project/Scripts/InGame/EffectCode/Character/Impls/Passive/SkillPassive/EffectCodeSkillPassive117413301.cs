using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Mono.Cecil.Cil;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 테토라 패시브 
    /// 범위: 자기 자신
    /// 테토라의 체력을 {0}% 잃을 때 마다, 분노를 1 획득합니다. 이 공격력 증가분은 체력을 회복할 경우 취소 됩니다.
    /// #분노: 공격력이 {1}% 상승합니다.
    /// </summary>
    [UseEffectCodeIds((int)CodeId)]
    public partial class EffectCodeSkillPassive117413301 : EffectCodeSkillPassiveBase
    {
        public const long CodeId = 117413301;
        private float _angerRatePercent; // 분노 비율
        private float _attackRatePercent; // 공격력 상승 비율
        private SkillPassive _specSkill;


        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _angerRatePercent = codeInfo.GetCodeStatToFloat(0) * 0.01f;
            _attackRatePercent = codeInfo.GetCodeStatToFloat(1) * 0.01f;
            _specSkill = base.GetSpecSkillPassive(CodeId);

        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _angerRatePercent = codeInfo.GetCodeStatToFloat(0) * 0.01f;
            _attackRatePercent = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        }
        

    }
}//117413301
