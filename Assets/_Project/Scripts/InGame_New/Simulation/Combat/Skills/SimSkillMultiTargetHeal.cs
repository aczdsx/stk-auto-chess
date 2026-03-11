namespace CookApps.AutoChess
{
    /// <summary>최저 HP 아군 N명 힐 스킬</summary>
    public class SimSkillMultiTargetHeal : SimSkillBase
    {
        private readonly int[] _targetBuffer = new int[16];

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return SkillAreaHelper.FindLowestHPAlly(state, caster.TeamIndex);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int count = FindLowestHPAllies(state, caster.TeamIndex, TargetCount, _targetBuffer);
            int healAmount = caster.Attack * PowerPercent / 100;

            for (int i = 0; i < count; i++)
            {
                int idx = state.FindUnitIndex(_targetBuffer[i]);
                if (idx < 0) continue;
                SkillDamageHelper.Heal(state, ref state.Units[idx], healAmount);
            }
        }

        private static int FindLowestHPAllies(CombatMatchState state, byte teamIndex,
            int count, int[] buffer)
        {
            int found = 0;
            for (int c = 0; c < count; c++)
            {
                int bestIdx = -1;
                int bestHP = int.MaxValue;
                for (int i = 0; i < state.UnitCount; i++)
                {
                    ref var u = ref state.Units[i];
                    if (u.TeamIndex != teamIndex || !u.IsAlive) continue;

                    bool alreadySelected = false;
                    for (int j = 0; j < found; j++)
                    {
                        if (buffer[j] == u.CombatId) { alreadySelected = true; break; }
                    }
                    if (alreadySelected) continue;

                    if (u.CurrentHP < bestHP)
                    {
                        bestHP = u.CurrentHP;
                        bestIdx = i;
                    }
                }
                if (bestIdx < 0) break;
                buffer[found++] = state.Units[bestIdx].CombatId;
            }
            return found;
        }
    }
}
