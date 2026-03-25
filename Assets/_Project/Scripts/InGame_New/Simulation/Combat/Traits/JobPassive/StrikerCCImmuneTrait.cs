namespace CookApps.AutoChess
{
    /// <summary>
    /// STRIKER 패시브: 쿨타임마다 CC 면역 1회 부여.
    /// OnTick에서 타이머 누적, 쿨타임 도달 시 owner.CCImmuneCharges = 1.
    /// </summary>
    public sealed class StrikerCCImmuneTrait : CombatTraitBase
    {
        private readonly int _cooldownFrames;
        private int _timer;

        public StrikerCCImmuneTrait(int cooldownFrames)
        {
            _cooldownFrames = cooldownFrames > 0 ? cooldownFrames : 1;
        }

        public override void OnCombatStart(CombatMatchState state, ref CombatUnit owner)
        {
            // 전투 시작 시 즉시 1회 부여
            owner.CCImmuneCharges = 1;
            _timer = 0;
            state.EventQueue?.PushStatusEffectAdded(owner.CombatId, CombatVfxType.JobStriker, -1);
        }

        public override void OnTick(CombatMatchState state, ref CombatUnit owner, int tickRate)
        {
            // 이미 면역 보유 중이면 타이머 정지
            if (owner.CCImmuneCharges > 0) return;

            _timer++;
            if (_timer >= _cooldownFrames)
            {
                owner.CCImmuneCharges = 1;
                _timer = 0;
                state.EventQueue?.PushStatusEffectAdded(owner.CombatId, CombatVfxType.JobStriker, -1);
            }
        }

        public override void Reset()
        {
            _timer = 0;
        }
    }
}
