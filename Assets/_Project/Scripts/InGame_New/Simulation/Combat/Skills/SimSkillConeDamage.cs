namespace CookApps.AutoChess
{
    /// <summary>전방 콘 범위 데미지 (caster 전방 N타일)</summary>
    public class SimSkillConeDamage : SimSkillBase
    {
        private int _range;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _range = p.Param0 > 0 ? p.Param0 : 2;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int dirCol = caster.TeamIndex == 0 ? 1 : -1;
            int attack = caster.Attack;
            int power = PowerPercent;
            var type = DamageType;
            byte team = caster.TeamIndex;

            SkillAreaHelper.ForEachEnemyInLine(state, team,
                caster.GridCol, caster.GridRow,
                dirCol, 0, _range,
                (ref CombatUnit t, int i) =>
                {
                    int raw = attack * power / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, type, ref t);
                    DamageSystem.ApplyDamage(state, ref t, dmg);
                    DamageSystem.ChargeMana(ref t, DamageSystem.ManaGainOnHit);
                });
        }

        public override void Reset()
        {
            _range = 2;
        }
    }
}