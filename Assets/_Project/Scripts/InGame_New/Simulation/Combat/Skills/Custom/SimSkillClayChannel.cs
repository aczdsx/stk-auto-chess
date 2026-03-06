namespace CookApps.AutoChess
{
    /// <summary>클레이: 채널링 존 (아군 힐 + 적 데미지)</summary>
    public class SimSkillClayChannel : SimSkillBase
    {
        private int _healPercent;
        private int _damagePercent;
        private int _zoneRange;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _healPercent = p.Param0;
            _damagePercent = p.Param1;
            _zoneRange = p.Param3 > 0 ? p.Param3 : 1;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return caster.CombatId;
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int col = caster.GridCol;
            int row = caster.GridRow;
            int attack = caster.Attack;
            var type = DamageType;
            byte team = caster.TeamIndex;

            // 아군 힐
            int healPct = _healPercent;
            SkillAreaHelper.ForEachAllyInRadius(state, team, col, row, _zoneRange,
                (ref CombatUnit ally, int i) =>
                {
                    int heal = attack * healPct / 100;
                    SkillDamageHelper.Heal(ref ally, heal);
                });

            // 적 데미지
            int dmgPct = _damagePercent;
            SkillAreaHelper.ForEachEnemyInRadius(state, team, col, row, _zoneRange,
                (ref CombatUnit enemy, int i) =>
                {
                    int raw = attack * dmgPct / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, type, ref enemy);
                    DamageSystem.ApplyDamage(state, ref enemy, dmg);
                });
        }

        public override void Reset()
        {
            _healPercent = 0;
            _damagePercent = 0;
            _zoneRange = 1;
        }
    }
}
