namespace CookApps.AutoChess
{
    /// <summary>범위 데미지 스킬 (최적 AoE 타겟 중심 원형 범위)</summary>
    public class SimSkillAoEDamage : SimSkillBase
    {
        private int _radius;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _radius = p.Param0 > 0 ? p.Param0 : 1;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return SkillAreaHelper.FindBestAoETarget(state, ref caster, _radius);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var center = ref state.Units[idx];

            int centerCol = center.GridCol;
            int centerRow = center.GridRow;
            int attack = caster.Attack;
            int power = PowerPercent;
            var type = DamageType;
            byte team = caster.TeamIndex;
            int casterIdx = state.FindUnitIndex(caster.CombatId);

            SkillAreaHelper.ForEachEnemyInRadius(state, team,
                centerCol, centerRow, _radius,
                (ref CombatUnit t, int i) =>
                {
                    int raw = attack * power / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, type, ref state.Units[casterIdx], ref t);
                    DamageSystem.ApplyDamage(state, ref t, dmg);
                });
        }

        public override void Reset()
        {
            _radius = 1;
        }
    }
}
