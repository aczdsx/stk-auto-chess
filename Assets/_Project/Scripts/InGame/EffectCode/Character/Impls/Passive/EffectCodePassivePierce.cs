using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using System;


namespace CookApps.BattleSystem
{
    /// <summary>
    /// 평타 공격시 {0}% 확률로 상대방의 물방 마방 관통
    /// </summary>
    public partial class EffectCodePassivePierce : EffectCodeCharacterBase
    {
        public const int CodeId = (int)EffectCodeNameType.PASSIVE_PIERCE;
        private float _piercePercentage = 0f;//관통확률

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _piercePercentage = codeInfo.GetCodeStatToFloat(1);

            Span<double> stats = stackalloc double[1];
            stats.Clear();
            stats[0] = _piercePercentage;
            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.PURE_DAMAGE_PROB_UP, owner, stats, source);

        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _piercePercentage = codeInfo.GetCodeStatToFloat(1);

            Span<double> stats = stackalloc double[1];
            stats.Clear();
            stats[0] = _piercePercentage;
            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.PURE_DAMAGE_PROB_UP, owner, stats, source);
        }

    }
}
