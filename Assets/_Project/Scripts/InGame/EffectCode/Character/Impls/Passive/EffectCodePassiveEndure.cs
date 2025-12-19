using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using System;
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]

    /// <summary>
    /// {0} 초 마다 일반공격 3회 무시 베리어 부여
    /// </summary>
    public partial class EffectCodePassiveEndure : EffectCodeCharacterBase
    {
        public const int CodeId = (int)EffectCodeNameType.JOBS_ENDURE;
        private bool _isReadyToActivate = false;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            CoolTimeElapsedTime = 0f;
            CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(1);
            _isReadyToActivate = false;
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            CoolTimeElapsedTime = 0f;
            CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(1);
            _isReadyToActivate = false;
        }

        public override void OnCooltime(float dt)
        {
            base.OnCooltime(dt);
            if (_isReadyToActivate || IsSkillActivated)
                return;
            CoolTimeElapsedTime += dt;
            if (CoolTimeElapsedTime >= CoolTimeDurationTime)
            {
                _isReadyToActivate = true;
            }
        }

        public override bool IsReadyToActivate()
        {
            return _isReadyToActivate;
        }

        public override void Activate()
        {
            base.Activate();
            InjectNormalAttackShield();
            _isReadyToActivate = false;
            CoolTimeElapsedTime = 0f;
        }
        private void InjectNormalAttackShield()
        {
            Span<double> buffStats = stackalloc double[3];
            buffStats.Clear();
            buffStats[0] = codeId;
            buffStats[1] = 999f;
            buffStats[2] = 3;
            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_NORMAL_ATTACK_SHIELD, owner, buffStats, source);
        }


    }
}
