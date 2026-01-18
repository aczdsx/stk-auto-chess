namespace CookApps.BattleSystem
{
    /// <summary>
    /// 필리아 패시브
    /// 범위: 자기 자신
    /// {0}회 타격 시 마다 다음 공격은 강화탄으로 변경됩니다. 
    /// #강화탄: 반드시 명중, 크리티컬 히트
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeSkillPassive115532401 : EffectCodeSkillPassiveBase
    {
        public const int CodeId = 115532401;
        private int _requiredAttackCount; // 강화탄 발동에 필요한 공격 횟수
        private int _currentAttackCount; // 현재 공격 횟수
        private bool _isNextAttackEnhanced; // 다음 공격이 강화탄인지 여부

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _requiredAttackCount = codeInfo.GetCodeStatToInt(0);
            _currentAttackCount = 0;
            _isNextAttackEnhanced = false;
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _requiredAttackCount = codeInfo.GetCodeStatToInt(0);
        }

        public override void OnAttack()
        {
            base.OnAttack();
            
            // 강화탄이 이미 준비되어 있으면 카운트 증가하지 않음
            if (_isNextAttackEnhanced)
                return;

            _currentAttackCount++;
            
            // 필요한 공격 횟수에 도달하면 다음 공격을 강화탄으로 설정
            if (_currentAttackCount >= _requiredAttackCount)
            {
                _isNextAttackEnhanced = true;
                _currentAttackCount = 0; // 카운터 리셋
            }
        }

        public override CharacterController.DamageTestFlags GetDamageTestFlags()
        {
            // 강화탄일 경우 회피 테스트 스킵 (반드시 명중)
            if (_isNextAttackEnhanced)
            {
                return CharacterController.DamageTestFlags.SkipAvoidTest;
            }
            return CharacterController.DamageTestFlags.None;
        }

        public override void OnAttackEnd(CharacterController target)
        {
            base.OnAttackEnd(target);
            
            // 강화탄 사용 후 플래그 리셋
            if (_isNextAttackEnhanced)
            {
                _isNextAttackEnhanced = false;
            }
        }

    }//117653505
}
