namespace CookApps.AutoChess
{
    /// <summary>직선 관통 데미지 스킬 (전방 N칸 관통)</summary>
    public class SimSkillLineDamage : SimSkillBase
    {
        private int _length;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _length = p.Param0 > 0 ? p.Param0 : 4;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];

            // 시전자 → 타겟 방향 계산
            int dc = target.GridCol - caster.GridCol;
            int dr = target.GridRow - caster.GridRow;
            int dirCol = dc > 0 ? 1 : (dc < 0 ? -1 : 0);
            int dirRow = dr > 0 ? 1 : (dr < 0 ? -1 : 0);

            // 방향이 없으면 (같은 위치) 팀에 따라 기본 방향 설정
            if (dirCol == 0 && dirRow == 0)
            {
                dirRow = caster.TeamIndex == 0 ? 1 : -1;
            }

            int attack = caster.Attack;
            int power = PowerPercent;
            var type = DamageType;
            byte team = caster.TeamIndex;
            int startCol = caster.GridCol;
            int startRow = caster.GridRow;

            SkillAreaHelper.ForEachEnemyInLine(state, team,
                startCol, startRow, dirCol, dirRow, _length,
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
            _length = 4;
        }
    }
}
