namespace CookApps.AutoChess
{
    /// <summary>
    /// 타겟 탐색 시스템. 유닛의 공격/스킬 대상을 결정.
    /// 기본 규칙: 가장 가까운 적 → HP 낮은 적 → 인덱스 낮은 적 (결정론적).
    /// </summary>
    public static class TargetingSystem
    {
        /// <summary>기본 타겟 탐색: 가장 가까운 생존 적 (풋프린트 기반 거리)</summary>
        public static int FindNearestEnemy(CombatMatchState state, ref CombatUnit unit)
        {
            int bestTarget = CombatUnit.InvalidId;
            int bestDist = int.MaxValue;
            int bestHP = int.MaxValue;
            int bestIndex = int.MaxValue;

            byte uSizeW = unit.SizeW > 0 ? unit.SizeW : (byte)1;
            byte uSizeH = unit.SizeH > 0 ? unit.SizeH : (byte)1;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var candidate = ref state.Units[i];
                if (!candidate.IsTargetable) continue;
                if (candidate.TeamIndex == unit.TeamIndex) continue;

                byte cSizeW = candidate.SizeW > 0 ? candidate.SizeW : (byte)1;
                byte cSizeH = candidate.SizeH > 0 ? candidate.SizeH : (byte)1;

                int dist = BoardHelper.MinManhattanDistance(
                    unit.GridCol, unit.GridRow, uSizeW, uSizeH,
                    candidate.GridCol, candidate.GridRow, cSizeW, cSizeH);

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

        /// <summary>특정 대상을 제외하고 가장 가까운 적 탐색</summary>
        public static int FindNearestEnemyExcluding(CombatMatchState state, ref CombatUnit unit, int excludeCombatId)
        {
            int bestTarget = CombatUnit.InvalidId;
            int bestDist = int.MaxValue;
            int bestHP = int.MaxValue;
            int bestIndex = int.MaxValue;

            byte uSizeW = unit.SizeW > 0 ? unit.SizeW : (byte)1;
            byte uSizeH = unit.SizeH > 0 ? unit.SizeH : (byte)1;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var candidate = ref state.Units[i];
                if (!candidate.IsTargetable) continue;
                if (candidate.TeamIndex == unit.TeamIndex) continue;
                if (candidate.CombatId == excludeCombatId) continue;

                byte cSizeW = candidate.SizeW > 0 ? candidate.SizeW : (byte)1;
                byte cSizeH = candidate.SizeH > 0 ? candidate.SizeH : (byte)1;

                int dist = BoardHelper.MinManhattanDistance(
                    unit.GridCol, unit.GridRow, uSizeW, uSizeH,
                    candidate.GridCol, candidate.GridRow, cSizeW, cSizeH);

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

        /// <summary>가장 먼 적 탐색 (풋프린트 기반 거리)</summary>
        public static int FindFarthestEnemy(CombatMatchState state, ref CombatUnit unit)
        {
            int bestTarget = CombatUnit.InvalidId;
            int bestDist = -1;
            int bestIndex = int.MaxValue;

            byte uSizeW = unit.SizeW > 0 ? unit.SizeW : (byte)1;
            byte uSizeH = unit.SizeH > 0 ? unit.SizeH : (byte)1;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var candidate = ref state.Units[i];
                if (!candidate.IsTargetable) continue;
                if (candidate.TeamIndex == unit.TeamIndex) continue;

                byte cSizeW = candidate.SizeW > 0 ? candidate.SizeW : (byte)1;
                byte cSizeH = candidate.SizeH > 0 ? candidate.SizeH : (byte)1;

                int dist = BoardHelper.MinManhattanDistance(
                    unit.GridCol, unit.GridRow, uSizeW, uSizeH,
                    candidate.GridCol, candidate.GridRow, cSizeW, cSizeH);

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

        /// <summary>공격력이 가장 높은 적 탐색</summary>
        public static int FindHighestAttackEnemy(CombatMatchState state, ref CombatUnit unit)
        {
            int bestTarget = CombatUnit.InvalidId;
            int bestAtk = -1;
            int bestIndex = int.MaxValue;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var candidate = ref state.Units[i];
                if (!candidate.IsTargetable) continue;
                if (candidate.TeamIndex == unit.TeamIndex) continue;

                if (candidate.Attack > bestAtk ||
                    (candidate.Attack == bestAtk && i < bestIndex))
                {
                    bestTarget = candidate.CombatId;
                    bestAtk = candidate.Attack;
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
                SkillTargetType.HighestAttackEnemy => FindHighestAttackEnemy(state, ref unit),
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

        /// <summary>타겟이 사거리 내에 있는지 검사 (풋프린트 기반)</summary>
        public static bool IsTargetInRange(ref CombatUnit attacker, ref CombatUnit target)
        {
            return BoardHelper.IsInRangeMulti(
                attacker.GridCol, attacker.GridRow,
                attacker.SizeW > 0 ? attacker.SizeW : (byte)1,
                attacker.SizeH > 0 ? attacker.SizeH : (byte)1,
                target.GridCol, target.GridRow,
                target.SizeW > 0 ? target.SizeW : (byte)1,
                target.SizeH > 0 ? target.SizeH : (byte)1,
                attacker.AttackRange);
        }

        /// <summary>힐러용 타겟 탐색: HP threshold 미만, 힐러 아닌, 사거리(+보정) 내 아군 중 최저 HP비율</summary>
        public static int FindHealTarget(CombatMatchState state, ref CombatUnit unit, int hpThreshold, int rangeBonus = 0)
        {
            int bestTarget = CombatUnit.InvalidId;
            int bestHPPercent = int.MaxValue;
            int bestIndex = int.MaxValue;

            int effectiveRange = unit.AttackRange + rangeBonus;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var candidate = ref state.Units[i];
                if (!candidate.IsAlive) continue;
                if (candidate.TeamIndex != unit.TeamIndex) continue;
                if (candidate.CombatId == unit.CombatId) continue;
                if (candidate.IsHealer) continue;

                int hpPercent = candidate.CurrentHP * 100 / candidate.MaxHP;
                if (hpPercent >= hpThreshold) continue;

                // 사거리(+보정) 내만
                if (!BoardHelper.IsInRangeMulti(
                    unit.GridCol, unit.GridRow,
                    unit.SizeW > 0 ? unit.SizeW : (byte)1,
                    unit.SizeH > 0 ? unit.SizeH : (byte)1,
                    candidate.GridCol, candidate.GridRow,
                    candidate.SizeW > 0 ? candidate.SizeW : (byte)1,
                    candidate.SizeH > 0 ? candidate.SizeH : (byte)1,
                    effectiveRange)) continue;

                // 우선순위: HP비율 낮은 순 → 인덱스 낮은 순
                if (hpPercent < bestHPPercent ||
                    (hpPercent == bestHPPercent && i < bestIndex))
                {
                    bestTarget = candidate.CombatId;
                    bestHPPercent = hpPercent;
                    bestIndex = i;
                }
            }

            return bestTarget;
        }

        /// <summary>타겟 갱신: 유효하지 않으면 새 타겟 탐색, 이동 중이면 더 가까운 적으로 전환</summary>
        public static void RefreshTarget(CombatMatchState state, ref CombatUnit unit)
        {
            // 힐러: 아군 힐 타겟 우선, 없으면 적 공격
            if (unit.IsHealer)
            {
                int healTarget = FindHealTarget(state, ref unit, OracleHealerTrait.HealTargetHPThreshold, OracleHealerTrait.HealRangeBonus);
                if (healTarget != CombatUnit.InvalidId)
                {
                    unit.CurrentTargetId = healTarget;
                    return;
                }
                // 힐 대상 없으면 적으로 강제 전환 (이전 아군 타겟이 남아있으면 안 됨)
                unit.CurrentTargetId = FindNearestEnemy(state, ref unit);
                return;
            }

            if (!IsTargetValid(state, unit.CurrentTargetId))
            {
                unit.CurrentTargetId = FindNearestEnemy(state, ref unit);
                return;
            }

            // 사거리 밖(이동 중)이면 더 가까운 적이 있는지 확인
            int targetIdx = state.FindUnitIndex(unit.CurrentTargetId);
            if (targetIdx < 0) return;

            ref var currentTarget = ref state.Units[targetIdx];
            if (!IsTargetInRange(ref unit, ref currentTarget))
            {
                int nearestId = FindNearestEnemy(state, ref unit);
                if (nearestId != CombatUnit.InvalidId)
                    unit.CurrentTargetId = nearestId;
            }
        }
    }
}
