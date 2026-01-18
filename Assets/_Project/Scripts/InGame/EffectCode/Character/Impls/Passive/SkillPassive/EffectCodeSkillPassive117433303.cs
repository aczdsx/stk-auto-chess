using CookApps.AutoBattler;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 하티 패시브
    /// 대상: 자기 자신
    /// 효과: 대상 과의 거리가 1칸당 위력이 {0}% 증가합니다.
    /// </summary>
    [UseEffectCodeIds((int)CodeId)]
    public partial class EffectCodeSkillPassive117433303 : EffectCodeSkillPassiveBase
    {
        public const long CodeId = 117433303;
        private float _powerRatePercent; // 위력 증가 비율
        private SkillPassive _specSkill;


        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _powerRatePercent = codeInfo.GetCodeStatToFloat(0) * 0.01f;
            _specSkill = base.GetSpecSkillPassive(CodeId);

            owner.SetStateType(typeof(CharacterStateIdle), typeof(CharacterStateIdleFarTarget));
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _powerRatePercent = codeInfo.GetCodeStatToFloat(0) * 0.01f;
        }

        public override void OnAttack()
        {
            base.OnAttack();
            if (owner.Target == null)
                return;
            var distance = InGameObjectManager.Instance.InGameGrid.GetManhattanDistance(owner.CurrentTile, owner.Target.CurrentTile);

            _powerRatePercent = _powerRatePercent * distance;
            owner.GetEffectCodeContainer().SetDirtyFlag(this);
        }


        public override void OnAttackEnd(CharacterController target)
        {
            base.OnAttackEnd(target);
            if(_powerRatePercent > 0f)
            {
                _powerRatePercent = 0f;
                owner.GetEffectCodeContainer().SetDirtyFlag(this);
                return;
            }
        }

        public override double GetIncrementPercentAD()
        {
            return _powerRatePercent;
        }

        public override double ModifyDamageAmount(double damageAmount)
        {
            return damageAmount * (1 + _powerRatePercent);
        }

        public override void OnPreRemoved()
        {
            owner.RemoveStateType(typeof(CharacterStateIdle));
            base.OnPreRemoved();
        }
        
    }
}//117413301
