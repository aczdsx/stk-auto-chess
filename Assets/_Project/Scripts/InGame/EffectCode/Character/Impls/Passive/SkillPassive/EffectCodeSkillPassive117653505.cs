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
        private int _healUpMaxCount; // 최대 증가 횟수
        private int _currentHealUpCount; // 현재 증가 횟수
        private float _healUpRatePercent; // 치유량 증가 비율

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _healUpMaxCount = codeInfo.GetCodeStatToInt(0);
            _currentHealUpCount = 0;
            _healUpRatePercent = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _healUpMaxCount = codeInfo.GetCodeStatToInt(0);
            _healUpRatePercent = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        }

        public override void OnAttackEnd(CharacterController target)
        {
            base.OnAttackEnd(target);
            if (target.AllianceType == owner.AllianceType)
            {
                ++_currentHealUpCount;
                _currentHealUpCount = Math.Min(_currentHealUpCount, _healUpMaxCount);
                owner.GetEffectCodeContainer().SetDirtyFlag(this);
            }
        }

        public override CharacterController.DamageInfo OnDamaged(CharacterController.DamageInfo damageInfo, CharacterController attacker, bool isPure)
        {
            if (attacker.AllianceType == owner.AllianceType)
            {
                --_currentHealUpCount;
                _currentHealUpCount = Math.Max(_currentHealUpCount, 0);

                owner.GetEffectCodeContainer().SetDirtyFlag(this);
            }
            return damageInfo;
        }

        public override float GetIncrementPercentGivenHealRate()
        {
            return _healUpRatePercent * _currentHealUpCount;
        }
    }//117513401
}
