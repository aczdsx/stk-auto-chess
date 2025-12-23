using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
namespace CookApps.BattleSystem
{    /// <summary>
     /// 평타 공격시 아군중 체력이 가장 낮은 캐릭터 대상 공격력 비례 회복 적용.
     /// 일단 자힐도 함께해야함.
     /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodePassiveRecovery : EffectCodeCharacterBase
    {
        public const int CodeId = (int)EffectCodeNameType.JOBS_RECOVERY;
        private float _recoveryPercentage = 0f;//회복비율
        private const InGameVfxNameType _recoveryVfxEnum = InGameVfxNameType.fx_common_buff_heal;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);

            _recoveryPercentage = codeInfo.GetCodeStatToFloat(1);

            owner.SetStateType(typeof(CharacterStateAttack), typeof(CharacterStateAttackHealer));
            owner.SetStateType(typeof(CharacterStateIdle), typeof(CharacterStateIdleHealer));
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _recoveryPercentage = codeInfo.GetCodeStatToFloat(1);
        }

        public override void OnAttackEnd(CharacterController target)
        {
            base.OnAttackEnd(target);
            //target에게 공격력 비례 회복적용
            
            double attackPower = owner.SpecCharacter.atk_type == AtkType.AD ? owner.AD : owner.AP;

            double recoveryAmount = attackPower * _recoveryPercentage;
            recoveryAmount = owner.PostCalculateHealAmount(recoveryAmount, target);
            target.GetHealed(recoveryAmount, owner, codeId, true);
            
            InGameVfxManager.Instance.AddInGameVfx(_recoveryVfxEnum, target.CurrentTile.View.CachedTr.position);

        }
        public override void OnPreRemoved()
        {
            owner.RemoveStateType(typeof(CharacterStateAttack));
            owner.RemoveStateType(typeof(CharacterStateIdle));

            base.OnPreRemoved();
        }
        

    }
}
