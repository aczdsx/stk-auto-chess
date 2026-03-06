namespace CookApps.AutoChess
{
    /// <summary>테토라: 단일 데미지 + 넉백, 넉백 착지 지점 AoE 스턴</summary>
    public class SimSkillTetoraKnockback : SimSkillBase
    {
        private int _knockbackDistance;
        private int _stunAoERange;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _knockbackDistance = p.Param0 > 0 ? p.Param0 : 4;
            _stunAoERange = p.Param1 > 0 ? p.Param1 : 1;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            SkillDamageHelper.DealDamage(state, ref caster, targetCombatId, PowerPercent, DamageType);

            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0 || !state.Units[idx].IsAlive) return;
            ref var target = ref state.Units[idx];

            int dirCol = caster.TeamIndex == 0 ? 1 : -1;
            SkillCCHelper.Knockback(state, ref target, dirCol, 0, _knockbackDistance);

            // 넉백 후 위치에서 AoE 스턴
            if (CCType != CrowdControlType.None && CCDurationFrames > 0)
            {
                int col = target.GridCol;
                int row = target.GridRow;
                byte team = caster.TeamIndex;
                var ccType = CCType;
                int ccFrames = CCDurationFrames;
                int attack = caster.Attack;
                int power = SecondaryPowerPercent;
                var type = DamageType;

                SkillAreaHelper.ForEachEnemyInRadius(state, team, col, row, _stunAoERange,
                    (ref CombatUnit t, int i) =>
                    {
                        if (power > 0)
                        {
                            int raw = attack * power / 100;
                            int dmg = DamageSystem.CalculateDamage(raw, type, ref t);
                            DamageSystem.ApplyDamage(state, ref t, dmg);
                        }
                        SkillCCHelper.ApplyCC(ref t, ccType, ccFrames);
                    });
            }
        }

        public override void Reset()
        {
            _knockbackDistance = 4;
            _stunAoERange = 1;
        }
    }
}
