using System;

namespace CookApps.AutoChess
{
    /// <summary>베인: 바운스 프로젝타일 (타겟 간 이동, 데미지 감소)</summary>
    public class SimSkillVeinBounce : SimSkillBase
    {
        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int maxBounces = TargetCount;
            int currentPower = PowerPercent;
            int decayPercent = SecondaryPowerPercent > 0 ? SecondaryPowerPercent : 20;
            var type = DamageType;
            int currentTargetId = targetCombatId;
            byte team = caster.TeamIndex;

            Span<int> hitIds = stackalloc int[maxBounces];
            int hitCount = 0;

            for (int bounce = 0; bounce < maxBounces; bounce++)
            {
                int idx = state.FindUnitIndex(currentTargetId);
                if (idx < 0 || !state.Units[idx].IsAlive) break;

                int raw = caster.Attack * currentPower / 100;
                int dmg = DamageSystem.CalculateDamage(raw, type, ref state.Units[idx]);
                DamageSystem.ApplyDamage(state, ref state.Units[idx], dmg);
                DamageSystem.ChargeMana(ref state.Units[idx], DamageSystem.ManaGainOnHit);

                hitIds[hitCount++] = currentTargetId;
                currentPower = currentPower * (100 - decayPercent) / 100;

                currentTargetId = FindNextBounceTarget(state, team, idx, hitIds, hitCount);
                if (currentTargetId == CombatUnit.InvalidId) break;
            }
        }

        private static int FindNextBounceTarget(CombatMatchState state, byte myTeam,
            int currentIdx, Span<int> hitIds, int hitCount)
        {
            ref var current = ref state.Units[currentIdx];
            int bestId = CombatUnit.InvalidId;
            int bestDist = int.MaxValue;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var u = ref state.Units[i];
                if (u.TeamIndex == myTeam || !u.IsAlive) continue;

                bool alreadyHit = false;
                for (int j = 0; j < hitCount; j++)
                    if (hitIds[j] == u.CombatId) { alreadyHit = true; break; }
                if (alreadyHit) continue;

                int dist = Math.Abs(u.GridCol - current.GridCol)
                         + Math.Abs(u.GridRow - current.GridRow);
                if (dist < bestDist) { bestDist = dist; bestId = u.CombatId; }
            }
            return bestId;
        }
    }
}
