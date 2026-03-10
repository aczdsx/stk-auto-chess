namespace CookApps.AutoChess
{
    /// <summary>
    /// 테토라: 단일 데미지 + 넉백
    /// - 충돌 시: 착지 지점 AoE 데미지 + 스턴
    /// - 미충돌 시: 추가 효과 없음 (타격딜만)
    /// </summary>
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
            int actualMoved = SkillCCHelper.Knockback(state, ref target, dirCol, 0, _knockbackDistance);
            bool hitWall = actualMoved < _knockbackDistance;

            // 충돌 시에만 착지 지점 AoE 데미지 + 스턴
            if (hitWall && CCType != CrowdControlType.None && CCDurationFrames > 0)
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
                        SkillCCHelper.ApplyCC(state, ref t, ccType, ccFrames);
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
