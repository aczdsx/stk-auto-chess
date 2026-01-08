using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 시라유키 패시브
    /// 대상: 자기 자신
    /// 회피에 성공할 경우 회피율이 {0}% 증가합니다.(최대 {1}회 중첩) 상대의 공격을 회피할 경우 공격 대상에게 {2}%의 위력을 가진 참격을 날려 반격합니다.
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeSkillPassive117663506 : EffectCodeCharacterBase
    {
        public const int CodeId = 117663506;
        private int _avoidSuccessMaxCount; // 최대 증가 횟수
        private float _avoidSuccessRatePercent; // 회피율 증가 비율
        private int _currentAvoidSuccessCount; // 현재 증가 횟수
        private float _DamageRatePercent; // 반격 데미지 비율


        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _avoidSuccessRatePercent = codeInfo.GetCodeStatToFloat(0) * 0.01f;
            _avoidSuccessMaxCount = codeInfo.GetCodeStatToInt(1);
            _currentAvoidSuccessCount = 0;
            _DamageRatePercent = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _avoidSuccessRatePercent = codeInfo.GetCodeStatToFloat(0) * 0.01f;
            _avoidSuccessMaxCount = codeInfo.GetCodeStatToInt(1);
            _DamageRatePercent = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        }
        public override CharacterController.DamageInfo OnDamaged(CharacterController.DamageInfo damageInfo, CharacterController attacker, bool isPure)
        {
            if (damageInfo.isMissed)
            {
                ++_currentAvoidSuccessCount;
                _currentAvoidSuccessCount = Math.Min(_currentAvoidSuccessCount, _avoidSuccessMaxCount);

                var damage = owner.CalculateDamageAmount(attacker.AD, attacker.AP, attacker, owner.CharacterId, isSkill: true,
                CharacterController.DamageTestFlags.None);

                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.Skill_Passive_17663506, owner.SkillMiddleFXTransformFollowable.GetPosition());
                attacker.GetDamaged(damage, owner);
            }
            return damageInfo;
        }

        public override float ModifyAvoidRateAmount(float avoidRateAmount)
        {
            return avoidRateAmount * (1f + _avoidSuccessRatePercent * _currentAvoidSuccessCount);
        }
    }
}//117243102
