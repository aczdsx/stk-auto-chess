using CookApps.AutoBattler;

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

        private bool IsPierceDamage()
        {
            return InGameRandomManager.GetUniversalRandomValue(0f, 100f) < _piercePercentage * 100;
        }

        public override CharacterController.DamageTestFlags GetDamageTestFlags()
        {
            // 관통 확률 체크 후 저항 관통 테스트 스킵
            if (IsPierceDamage())
            {
                return CharacterController.DamageTestFlags.SkipResistPierce;
            }
            return CharacterController.DamageTestFlags.None;
        }

        public override void OnPreRemoved()
        {
            owner.RemoveStateType(typeof(CharacterStateAttack));
            base.OnPreRemoved();
        }

    }
}
