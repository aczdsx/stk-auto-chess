using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using System;
using UnityEngine;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// {0}초 마다 CC 효과를 막아내는 방어막이 생성된다 immune buff 주입.
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeJobPassiveBash : EffectCodeCharacterBase
    {
        public const int CodeId = (int)EffectCodeNameType.JOBS_BRAVE;

        private static readonly float _immuneDuration = 1f;
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
            InjectImmuneBuff();
            _isReadyToActivate = false;
            CoolTimeElapsedTime = 0f;
        }
        private void InjectImmuneBuff()
        {
            // owner.SetImmuneSuccessFx(InGameVfxNameType.fx_common_job_striker_02, owner.SkillMiddleFXTransformFollowable);
            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_job_striker_01, owner.SkillRootTransformFollowable);
            Span<double> buffStats = stackalloc double[3];

            buffStats.Clear();
            buffStats[0] = codeId;
            buffStats[1] = _immuneDuration;//duration
            buffStats[2] = 1;//value?

            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_IMMUNE, owner, buffStats, source);
        }

        public override void OnCanceledCC(CharacterController attacker, EffectCodeNameType effectCodeNameType)
        {
            base.OnCanceledCC(attacker, effectCodeNameType);

            var vfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_job_striker_02, owner.SkillMiddleFXTransformFollowable.GetPosition());
            vfx.CachedTr.rotation = Quaternion.LookRotation((attacker.ViewPosition3D - owner.ViewPosition3D).normalized) * Quaternion.Euler(0, -90, 0);
            owner.GetEffectCodeContainer().RemoveEffectCode((long)EffectCodeNameType.BUFF_IMMUNE);
        }
    }

}