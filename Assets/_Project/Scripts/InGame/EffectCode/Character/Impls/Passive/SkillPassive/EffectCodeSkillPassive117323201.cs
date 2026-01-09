using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Mono.Cecil.Cil;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 미사 패시브
    /// 범위: 아군 전체
    /// 일반 공격 시 미사의 최대 체력의 {0}% 만큼 아군의 체력을 회복시킵니다.
    /// </summary>
    [UseEffectCodeIds((int)CodeId)]
    public partial class EffectCodeSkillPassive117323201 : EffectCodeSkillPassiveBase
    {
        public const long CodeId = 117323201;
        private float _healRatePercent; // 힐 비율

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _healRatePercent = codeInfo.GetCodeStatToFloat(0) * 0.01f;

        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _healRatePercent = codeInfo.GetCodeStatToFloat(0) * 0.01f;
        }

        public override void OnAttack()
        {
            base.OnAttack();
            foreach (var ally in InGameObjectManager.Instance.GetCharacterList(owner.AllianceType))
            {
                double healAmount = owner.PostCalculateHealAmount(owner.HP * _healRatePercent, ally);
                ally.GetHealed(healAmount, owner, codeId, true);
            }
        }
    }
}//117323201
