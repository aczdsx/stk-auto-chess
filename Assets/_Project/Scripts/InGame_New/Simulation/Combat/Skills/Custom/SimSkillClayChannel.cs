namespace CookApps.AutoChess
{
    /// <summary>클레이: 3초 채널링 존 (매 틱 아군 힐 + CC제거, 적 데미지 + 회복감소)</summary>
    public class SimSkillClayChannel : SimSkillBase
    {
        private int _healPercent;
        private int _damagePercent;
        private int _healReductionPercent;
        private int _zoneRange;

        private int _remainingTicks;
        private int _tickInterval;
        private int _tickTimer;

        // 원본: 3초, 0.5초 간격 = 6틱. 30fps 기준 15프레임 간격.
        private const int DefaultTickCount = 6;
        private const int DefaultTickInterval = 15;

        public override bool IsChanneling => _remainingTicks > 0;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _healPercent = p.Param0;
            _damagePercent = p.Param1;
            _healReductionPercent = p.Param2;
            _zoneRange = p.Param3 > 0 ? p.Param3 : 2; // 맨해튼 거리 2
        }

        public override int GetCastFrames() => DefaultTickCount * DefaultTickInterval;

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return caster.CombatId;
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            _tickInterval = DefaultTickInterval;
            _remainingTicks = DefaultTickCount - 1; // 첫 틱은 지금 실행
            _tickTimer = _tickInterval;

            DoZoneTick(state, ref caster);
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (_remainingTicks <= 0) return false;

            _tickTimer--;
            if (_tickTimer > 0) return true;

            _tickTimer = _tickInterval;
            DoZoneTick(state, ref caster);
            _remainingTicks--;

            return _remainingTicks > 0;
        }

        private void DoZoneTick(CombatMatchState state, ref CombatUnit caster)
        {
            int col = caster.GridCol;
            int row = caster.GridRow;
            int attack = caster.Attack;
            var type = DamageType;
            byte team = caster.TeamIndex;

            // 타일 이펙트 이벤트 (맨해튼 거리 기반 다이아몬드)
            state.EventQueue?.PushSkillAreaEffect(
                caster.SourceEntityId, (byte)col, (byte)row, _zoneRange);

            // 아군 힐 (틱당 배율 = 전체 배율 / 틱수)
            int healPct = _healPercent;
            SkillAreaHelper.ForEachAllyInRadius(state, team, col, row, _zoneRange,
                (ref CombatUnit ally, int i) =>
                {
                    int heal = attack * healPct / 100 / DefaultTickCount;
                    SkillDamageHelper.Heal(state, ref ally, heal);
                });

            // 적 데미지 (틱당 배율 = 전체 배율 / 틱수)
            int dmgPct = _damagePercent;
            SkillAreaHelper.ForEachEnemyInRadius(state, team, col, row, _zoneRange,
                (ref CombatUnit enemy, int i) =>
                {
                    int raw = attack * dmgPct / 100 / DefaultTickCount;
                    int dmg = DamageSystem.CalculateDamage(raw, type, ref enemy);
                    DamageSystem.ApplyDamage(state, ref enemy, dmg);
                    DamageSystem.ChargeMana(ref enemy, DamageSystem.ManaGainOnHit);
                });
        }

        public override void Reset()
        {
            _healPercent = 0;
            _damagePercent = 0;
            _healReductionPercent = 0;
            _zoneRange = 2;
            _remainingTicks = 0;
            _tickTimer = 0;
        }
    }
}
