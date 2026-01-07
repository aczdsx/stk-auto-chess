using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 평타 공격 시 스택으로 횟수 체크 후 다음공격 크리티컬 적용.
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeJobPassiveCriticalStack : EffectCodeCharacterBase
    {
        public const int CodeId = (int)EffectCodeNameType.JOBS_CRITICAL_STACK;

        private int _curCrriticalStackCount = 0;
        private int _maxCriticalStackCount = 0;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _maxCriticalStackCount = (int)codeInfo.GetCodeStatToInt(1);
            _curCrriticalStackCount = 0;
            // stats[0] = (int)passiveData.skill_value_type;
            // stats[1] = passiveData.passive_rate;
            // stats[2] = passiveData.grade;
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _maxCriticalStackCount = (int)codeInfo.GetCodeStatToInt(1);
            _curCrriticalStackCount = 0;
        }

        public override void OnAttackEnd(CharacterController target)
        {
            base.OnAttackEnd(target);
            owner.ResetFixedCriticalProb();
            
            _curCrriticalStackCount++;
            if (_curCrriticalStackCount >= _maxCriticalStackCount)
            {
                _curCrriticalStackCount = 0;
                owner.SetFixedCriticalProb(100f);
                Debug.Log("CriticalStack Passive On ");
            }
        }
    }
}
