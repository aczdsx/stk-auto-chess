namespace CookApps.AutoChess
{
    /// <summary>
    /// GHOST 패시브: N번 공격마다 확정 크리티컬.
    /// OnPostAttack에서 스택 누적, 도달 시 다음 공격 확정 크리티컬 플래그 설정.
    /// OnPreAttack에서 CritRate를 100으로 올려 확정 크리, OnPostAttack에서 복원.
    /// </summary>
    public sealed class GhostCritStackTrait : CombatTraitBase
    {
        private readonly int _maxStack;
        private int _stack;
        private bool _nextCrit;
        private int _savedCritRate;
        private bool _critOverrideActive;

        public GhostCritStackTrait(int maxStack)
        {
            _maxStack = maxStack > 0 ? maxStack : 1;
        }

        public override void OnPreAttack(CombatMatchState state, ref CombatUnit attacker, ref CombatUnit target)
        {
            _critOverrideActive = false;
            if (_nextCrit)
            {
                _savedCritRate = attacker.CritRate;
                attacker.CritRate = 100;
                _critOverrideActive = true;
                _nextCrit = false;
            }
        }

        public override void OnPostAttack(CombatMatchState state, ref CombatUnit attacker, ref CombatUnit target)
        {
            // 크리티컬 오버라이드 복원
            if (_critOverrideActive)
            {
                attacker.CritRate = _savedCritRate;
                _critOverrideActive = false;
            }

            // 스택 누적
            _stack++;
            if (_stack >= _maxStack)
            {
                _nextCrit = true;
                _stack = 0;
            }
        }

        public override void Reset()
        {
            _stack = 0;
            _nextCrit = false;
            _critOverrideActive = false;
        }
    }
}
