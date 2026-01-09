using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Mono.Cecil.Cil;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 루키다 패시브
    /// 대상: 자기자신
    /// 일반공격 시 {0}% 확률로 여우불을 획득합니다. 
    /// 여우불의 갯수 당 {1}%의 추가 피해를 입힙니다. 
    /// 여우불은 개별 지속시간을 가지며 {2}초간 유지됩니다.
    /// </summary>
    [UseEffectCodeIds((int)CodeId)]
    public partial class EffectCodeSkillPassive117263103 : EffectCodeSkillPassiveBase
    {
        public const long CodeId = 117263103;
        private float _successRatePercent; // 성공 확률
        private float _damageRatePercent; // 추가 피해 비율
        private float _fireBuffTime; // 지속 시간
        private SkillPassive _specSkill;
        private EffectCodeSkill217263103 _skillEffectCode;// 루키다 패시브

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _successRatePercent = codeInfo.GetCodeStatToFloat(0);
            _damageRatePercent = codeInfo.GetCodeStatToFloat(1) * 0.01f;
            _fireBuffTime = codeInfo.GetCodeStatToFloat(2);

            _specSkill = base.GetSpecSkillPassive(CodeId);
            _skillEffectCode = base.GetActiveSkillEffectCodeId(CodeId) as EffectCodeSkill217263103;
            _skillEffectCode.SetFoxFireDuration(_fireBuffTime);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _successRatePercent = codeInfo.GetCodeStatToFloat(0);
            _damageRatePercent = codeInfo.GetCodeStatToFloat(1) * 0.01f;
            _fireBuffTime = codeInfo.GetCodeStatToFloat(2);
        }

        public override void OnAttack()
        {
            base.OnAttack();
            if (InGameRandomManager.GetUniversalRandomValue(0, 100) <= _successRatePercent)
            {
                _skillEffectCode.AddFoxFire(1);
            }
        }

        public override double ModifyDamageAmount(double damageAmount)
        {
            var foxFireCount = _skillEffectCode.GetCurrentFoxFireCount();
            if (foxFireCount > 0)
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01, owner.SkillRootTransformFollowable);
                return damageAmount * (1f + _damageRatePercent * foxFireCount);
            }
            return damageAmount;
        }
       
    }
}//117323201
