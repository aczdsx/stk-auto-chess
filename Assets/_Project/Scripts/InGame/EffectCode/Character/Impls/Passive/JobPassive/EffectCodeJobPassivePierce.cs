using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using System;


namespace CookApps.BattleSystem
{
    /// <summary>
    /// 평타 공격시 {0}% 확률로 상대방의 물방 마방 관통
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeJobPassivePierce : EffectCodeCharacterBase
    {
        public const int CodeId = (int)EffectCodeNameType.JOBS_PIERCE;
        private float _piercePercentage = 0f;//관통 확률
        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);

            owner.SetStateType(typeof(CharacterStateAttack), typeof(CharacterStateAttackPierce));
            _piercePercentage = codeInfo.GetCodeStatToFloat(1);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _piercePercentage = codeInfo.GetCodeStatToFloat(1);
        }

        public bool IsPierceDamage()
        {
            return InGameRandomManager.GetUniversalRandomValue(0f, 100f) < _piercePercentage * 100;
        }

        public override void OnPreRemoved()
        {
            owner.RemoveStateType(typeof(CharacterStateAttack));
            base.OnPreRemoved();
        }

    }
}
