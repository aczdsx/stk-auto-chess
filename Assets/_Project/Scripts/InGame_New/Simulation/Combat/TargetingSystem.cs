namespace CookApps.AutoChess
{
    /// <summary>
    /// 타겟 탐색 시스템. 유닛의 공격/스킬 대상을 결정.
    /// 기본 규칙: 가장 가까운 적 → HP 낮은 적 → 인덱스 낮은 적 (결정론적).
    /// </summary>
    public static class TargetingSystem
    {
        /// <summary>기본 타겟 탐색: 가장 가까운 생존 적</summary>
        public static int FindNearestEnemy(CombatMatchState state, ref CombatUnit unit)
        {
            int bestTarget = CombatUnit.InvalidId;
            int bestDist = int.MaxValue;
            int bestHP = int.MaxValue;
            int bestIndex = int.MaxValue;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var candidate = ref state.Units[i];
                if (!candidate.IsTargetable) continue;
                if (candidate.TeamIndex == unit.TeamIndex) continue;

                int dist = BoardHelper.ManhattanDistance(
                    unit.GridCol, unit.GridRow,
                    candidate.GridCol, candidate.GridRow);

                // 우선순위: 거리 → HP → 인덱스
                if (dist < bestDist ||
                    (dist == bestDist && candidate.CurrentHP < bestHP) ||
                    (dist == bestDist && candidate.CurrentHP == bestHP && i < bestIndex))
                {
                    bestTarget = candidate.CombatId;
                    bestDist = dist;
                    bestHP = candidate.CurrentHP;
                    bestIndex = i;
                }
            }

            return bestTarget;
        }

        /// <summary>가장 먼 적 탐색</summary>
        public static int FindFarthestEnemy(CombatMatchState state, ref CombatUnit unit)
        {
            int bestTarget = CombatUnit.InvalidId;
            int bestDist = -1;
            int bestIndex = int.MaxValue;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var candidate = ref state.Units[i];
                if (!candidate.IsTargetable) continue;
                if (candidate.TeamIndex == unit.TeamIndex) continue;

                int dist = BoardHelper.ManhattanDistance(
                    unit.GridCol, unit.GridRow,
                    candidate.GridCol, candidate.GridRow);

                if (dist > bestDist || (dist == bestDist && i < bestIndex))
                {
                    bestTarget = candidate.CombatId;
                    bestDist = dist;
                    bestIndex = i;
                }
            }

            return bestTarget;
        }

        /// <summary>HP가 가장 낮은 적 탐색</summary>
        public static int FindLowestHPEnemy(CombatMatchState state, ref CombatUnit unit)
        {
            int bestTarget = CombatUnit.InvalidId;
            int bestHP = int.MaxValue;
            int bestIndex = int.MaxValue;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var candidate = ref state.Units[i];
                if (!candidate.IsTargetable) continue;
                if (candidate.TeamIndex == unit.TeamIndex) continue;

                if (candidate.CurrentHP < bestHP ||
                    (candidate.CurrentHP == bestHP && i < bestIndex))
                {
                    bestTarget = candidate.CombatId;
                    bestHP = candidate.CurrentHP;
                    bestIndex = i;
                }
            }

            return bestTarget;
        }

        /// <summary>타겟 타입에 따른 단일 타겟 선택</summary>
        public static int FindTarget(CombatMatchState state, ref CombatUnit unit, SkillTargetType targetType)
        {
            return targetType switch
            {
                SkillTargetType.CurrentTarget => unit.CurrentTargetId,
                SkillTargetType.NearestEnemy => FindNearestEnemy(state, ref unit),
                SkillTargetType.FarthestEnemy => FindFarthestEnemy(state, ref unit),
                SkillTargetType.LowestHPEnemy => FindLowestHPEnemy(state, ref unit),
                SkillTargetType.Self => unit.CombatId,
                _ => FindNearestEnemy(state, ref unit),
            };
        }

        /// <summary>현재 타겟이 유효한지 검사 (죽었거나 없으면 false)</summary>
        public static bool IsTargetValid(CombatMatchState state, int targetCombatId)
        {
            if (targetCombatId == CombatUnit.InvalidId) return false;

            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return false;

            return state.Units[idx].IsTargetable;
        }

        /// <summary>타겟이 사거리 내에 있는지 검사</summary>
        public static bool IsTargetInRange(ref CombatUnit attacker, ref CombatUnit target)
        {
            return BoardHelper.IsInRange(
                attacker.GridCol, attacker.GridRow,
                target.GridCol, target.GridRow,
                attacker.AttackRange);
        }

        /// <summary>타겟 갱신: 유효하지 않으면 새 타겟 탐색</summary>
        public static void RefreshTarget(CombatMatchState state, ref CombatUnit unit)
        {
            if (!IsTargetValid(state, unit.CurrentTargetId))
            {
                unit.CurrentTargetId = FindNearestEnemy(state, ref unit);
            }
        }
    }
}
