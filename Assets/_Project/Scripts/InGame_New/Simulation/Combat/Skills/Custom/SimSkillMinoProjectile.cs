namespace CookApps.AutoChess
{
    /// <summary>미노: 최저 HP 적 3명에게 프로젝타일 + 주변 스플래시</summary>
    public class SimSkillMinoProjectile : SimSkillBase
    {
        private readonly int[] _targetBuffer = new int[16];

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int count = FindLowestHPEnemies(state, caster.TeamIndex, TargetCount, _targetBuffer);
            int attack = caster.Attack;
            int mainPower = PowerPercent;
            int splashPower = SecondaryPowerPercent > 0 ? SecondaryPowerPercent : PowerPercent;
            var type = DamageType;
            byte team = caster.TeamIndex;

            for (int t = 0; t < count; t++)
            {
                int mainIdx = state.FindUnitIndex(_targetBuffer[t]);
                if (mainIdx < 0) continue;
                ref var mainTarget = ref state.Units[mainIdx];
                if (!mainTarget.IsAlive) continue;

                // 메인 타겟 데미지
                int mainRaw = attack * mainPower / 100;
                int mainDmg = DamageSystem.CalculateDamage(mainRaw, type, ref caster, ref mainTarget);
                DamageSystem.ApplyDamage(state, ref mainTarget, mainDmg);
                DamageSystem.ChargeMana(ref mainTarget, DamageSystem.ManaGainOnHit);

                // 주변 1타일 스플래시
                int col = mainTarget.GridCol;
                int row = mainTarget.GridRow;
                int mainId = mainTarget.CombatId;
                int casterIdx = state.FindUnitIndex(caster.CombatId);
                SkillAreaHelper.ForEachEnemyInRadius(state, team, col, row, 1,
                    (ref CombatUnit u, int i) =>
                    {
                        if (u.CombatId == mainId) return;
                        int raw = attack * splashPower / 100;
                        int dmg = DamageSystem.CalculateDamage(raw, type, ref state.Units[casterIdx], ref u);
                        DamageSystem.ApplyDamage(state, ref u, dmg);
                        DamageSystem.ChargeMana(ref u, DamageSystem.ManaGainOnHit);
                    });
            }
        }

        private static int FindLowestHPEnemies(CombatMatchState state, byte myTeam, int count, int[] buffer)
        {
            int found = 0;
            for (int c = 0; c < count; c++)
            {
                int bestIdx = -1;
                int bestHP = int.MaxValue;
                for (int i = 0; i < state.UnitCount; i++)
                {
                    ref var u = ref state.Units[i];
                    if (u.TeamIndex == myTeam || !u.IsAlive) continue;
                    bool already = false;
                    for (int j = 0; j < found; j++)
                        if (buffer[j] == u.CombatId) { already = true; break; }
                    if (already) continue;
                    if (u.CurrentHP < bestHP) { bestHP = u.CurrentHP; bestIdx = i; }
                }
                if (bestIdx < 0) break;
                buffer[found++] = state.Units[bestIdx].CombatId;
            }
            return found;
        }
    }
}
