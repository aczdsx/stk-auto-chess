namespace CookApps.AutoChess
{
    /// <summary>
    /// 투사체 시스템. Homing/Linear/AreaTarget 투사체 생성 및 틱 처리.
    /// 매 전투 프레임 ProcessAll()을 호출하여 모든 활성 투사체를 업데이트.
    /// </summary>
    public static class ProjectileSystem
    {
        /// <summary>모든 활성 투사체 처리 (매 전투 틱 호출)</summary>
        public static void ProcessAll(CombatMatchState state, ref DeterministicRNG rng)
        {
            for (int i = state.ProjectileCount - 1; i >= 0; i--)
            {
                if (!state.Projectiles[i].IsActive) continue;

                switch (state.Projectiles[i].Type)
                {
                    case ProjectileType.Homing:
                        ProcessHoming(state, ref state.Projectiles[i], ref rng);
                        break;
                    case ProjectileType.Linear:
                        ProcessLinear(state, ref state.Projectiles[i], ref rng);
                        break;
                    case ProjectileType.AreaTarget:
                        ProcessAreaTarget(state, ref state.Projectiles[i], ref rng);
                        break;
                }

                // 비활성화된 투사체 정리 (swap-back)
                if (!state.Projectiles[i].IsActive)
                {
                    RemoveProjectile(state, i);
                }
            }
        }

        // ── Homing 투사체 ──

        private static void ProcessHoming(CombatMatchState state, ref Projectile proj, ref DeterministicRNG rng)
        {
            proj.RemainingFrames--;

            if (proj.RemainingFrames > 0) return;

            // 도착: 타겟이 살아있으면 데미지 적용
            int targetIdx = state.FindUnitIndex(proj.TargetCombatId);
            if (targetIdx >= 0 && state.Units[targetIdx].IsValidTarget)
            {
                ref var target = ref state.Units[targetIdx];
                int finalDamage = DamageSystem.CalculateDamage(proj.Damage, proj.DamageType, ref target);

                if (CombatLogger.Enabled) CombatLogger.LogProjectileHit(target.CombatId, proj.SourceCombatId, finalDamage, proj.IsCrit);

                DamageSystem.ApplyDamage(state, ref target, finalDamage, isCrit: proj.IsCrit);
                DamageSystem.ChargeMana(ref target, DamageSystem.ManaGainOnHit);

                // 흡혈 적용 (발사자가 살아있으면)
                int srcIdx = state.FindUnitIndex(proj.SourceCombatId);
                if (srcIdx >= 0 && state.Units[srcIdx].IsAlive)
                {
                    DamageSystem.ApplyLifeSteal(ref state.Units[srcIdx], finalDamage);
                }
            }

            proj.IsActive = false;
        }

        // ── Linear 투사체 ──

        private static void ProcessLinear(CombatMatchState state, ref Projectile proj, ref DeterministicRNG rng)
        {
            proj.MoveTimer--;

            if (proj.MoveTimer > 0) return;

            // 다음 타일로 이동
            int nextCol = proj.CurrentCol + proj.DirCol;
            int nextRow = proj.CurrentRow + proj.DirRow;

            // 범위 밖 → 소멸
            if (!BoardHelper.IsValidCombatPosition(nextCol, nextRow))
            {
                state.EventQueue?.PushProjectileExpired(proj.ProjectileId, proj.SourceCombatId);
                proj.IsActive = false;
                return;
            }

            proj.CurrentCol = (byte)nextCol;
            proj.CurrentRow = (byte)nextRow;
            proj.TraveledDistance++;
            proj.MoveTimer = proj.MoveInterval;

            // 뷰 이벤트: 타일 이동 (방향 + width 정보 전달)
            state.EventQueue?.PushProjectileMoved(proj.ProjectileId, proj.SourceCombatId,
                (byte)nextCol, (byte)nextRow, proj.DirCol, proj.DirRow, proj.Width);

            // 발사자 팀 조회 (충돌 검사용)
            int srcIdx = state.FindUnitIndex(proj.SourceCombatId);
            byte srcTeam = srcIdx >= 0 ? state.Units[srcIdx].TeamIndex : (byte)0xFF;

            // 충돌 검사: Width에 따라 진행 방향 수직으로 확장
            int effectiveWidth = proj.Width > 1 ? proj.Width : 1;
            int halfW = effectiveWidth / 2; // width=3 → halfW=1

            for (int offset = -halfW; offset <= halfW; offset++)
            {
                int checkCol, checkRow;
                if (offset == 0)
                {
                    checkCol = nextCol;
                    checkRow = nextRow;
                }
                else
                {
                    // 진행 방향 수직으로 오프셋
                    // 세로 이동(dirRow != 0, dirCol == 0) → col 방향 확장
                    // 가로 이동(dirCol != 0, dirRow == 0) → row 방향 확장
                    // 대각선 → 양쪽 다 확장 (십자형)
                    if (proj.DirCol == 0)
                    {
                        checkCol = nextCol + offset;
                        checkRow = nextRow;
                    }
                    else if (proj.DirRow == 0)
                    {
                        checkCol = nextCol;
                        checkRow = nextRow + offset;
                    }
                    else
                    {
                        // 대각선: col 확장 + row 확장 두 타일 모두 검사
                        HitOccupant(state, ref proj, nextCol + offset, nextRow, srcIdx, srcTeam);
                        checkCol = nextCol;
                        checkRow = nextRow + offset;
                    }
                }

                HitOccupant(state, ref proj, checkCol, checkRow, srcIdx, srcTeam);
            }

            // 최대 거리 도달 → 소멸
            if (proj.TraveledDistance >= proj.MaxDistance)
            {
                state.EventQueue?.PushProjectileExpired(proj.ProjectileId, proj.SourceCombatId);
                proj.IsActive = false;
            }
        }

        // ── AreaTarget 투사체 ──

        private static void ProcessAreaTarget(CombatMatchState state, ref Projectile proj, ref DeterministicRNG rng)
        {
            proj.RemainingFrames--;

            if (proj.RemainingFrames > 0) return;

            // 도착: 범위 내 모든 적에게 데미지
            int srcIdx = state.FindUnitIndex(proj.SourceCombatId);
            byte srcTeam = srcIdx >= 0 ? state.Units[srcIdx].TeamIndex : (byte)0xFF;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsValidTarget) continue;
                if (unit.TeamIndex == srcTeam) continue;

                int dist = BoardHelper.MinManhattanDistance(
                    unit.GridCol, unit.GridRow,
                    unit.SizeW > 0 ? unit.SizeW : (byte)1,
                    unit.SizeH > 0 ? unit.SizeH : (byte)1,
                    proj.TargetCol, proj.TargetRow, 1, 1);

                if (dist > proj.AreaRadius) continue;

                int finalDamage = DamageSystem.CalculateDamage(proj.Damage, proj.DamageType, ref unit);
                DamageSystem.ApplyDamage(state, ref unit, finalDamage, isCrit: proj.IsCrit);
                DamageSystem.ChargeMana(ref unit, DamageSystem.ManaGainOnHit);
            }

            // 흡혈 (발사자)
            if (srcIdx >= 0 && state.Units[srcIdx].IsAlive)
            {
                // AreaTarget은 흡혈 미적용 (밸런스)
            }

            state.EventQueue?.PushProjectileExploded(proj.TargetCol, proj.TargetRow, proj.AreaRadius, proj.SkillSpecId);

            proj.IsActive = false;
        }

        // ── 투사체 생성 ──

        /// <summary>Homing 투사체 생성</summary>
        public static void CreateHomingProjectile(
            CombatMatchState state, int sourceCombatId, int targetCombatId,
            int damage, bool isCrit, DamageType damageType, int travelFrames)
        {
            int slot = FindEmptyProjectileSlot(state);
            if (slot < 0) return;

            ref var proj = ref state.Projectiles[slot];
            proj.ProjectileId = state.NextProjectileId++;
            proj.SourceCombatId = sourceCombatId;
            proj.TargetCombatId = targetCombatId;
            proj.Type = ProjectileType.Homing;
            proj.DamageType = damageType;
            proj.Damage = damage;
            proj.IsCrit = isCrit;
            proj.RemainingFrames = travelFrames;
            proj.IsActive = true;

            if (slot >= state.ProjectileCount)
                state.ProjectileCount = slot + 1;

            // 뷰 이벤트 발행
            int srcIdx = state.FindUnitIndex(sourceCombatId);
            byte srcCol = srcIdx >= 0 ? state.Units[srcIdx].GridCol : (byte)0;
            byte srcRow = srcIdx >= 0 ? state.Units[srcIdx].GridRow : (byte)0;
            state.EventQueue?.PushProjectileSpawned(sourceCombatId, targetCombatId, ProjectileType.Homing, srcCol, srcRow, projectileId: proj.ProjectileId);
        }

        /// <summary>Linear 투사체 생성</summary>
        public static void CreateLinearProjectile(
            CombatMatchState state, int sourceCombatId,
            byte startCol, byte startRow, sbyte dirCol, sbyte dirRow,
            int damage, bool isCrit, DamageType damageType,
            int moveInterval, int maxDistance, int width = 1, int skillSpecId = 0)
        {
            int slot = FindEmptyProjectileSlot(state);
            if (slot < 0) return;

            ref var proj = ref state.Projectiles[slot];
            proj.ProjectileId = state.NextProjectileId++;
            proj.SourceCombatId = sourceCombatId;
            proj.TargetCombatId = CombatUnit.InvalidId;
            proj.Type = ProjectileType.Linear;
            proj.DamageType = damageType;
            proj.Damage = damage;
            proj.IsCrit = isCrit;
            proj.CurrentCol = startCol;
            proj.CurrentRow = startRow;
            proj.DirCol = dirCol;
            proj.DirRow = dirRow;
            proj.MoveInterval = moveInterval;
            proj.MoveTimer = moveInterval;
            proj.MaxDistance = maxDistance;
            proj.TraveledDistance = 0;
            proj.HitMask = 0;
            proj.Width = width;
            proj.SkillSpecId = skillSpecId;
            proj.IsActive = true;

            if (slot >= state.ProjectileCount)
                state.ProjectileCount = slot + 1;

            state.EventQueue?.PushProjectileSpawned(sourceCombatId, CombatUnit.InvalidId, ProjectileType.Linear,
                startCol, startRow, dirCol, dirRow, proj.ProjectileId, skillSpecId);
        }

        /// <summary>AreaTarget 투사체 생성</summary>
        public static void CreateAreaProjectile(
            CombatMatchState state, int sourceCombatId,
            byte targetCol, byte targetRow, int areaRadius,
            int damage, bool isCrit, DamageType damageType, int travelFrames)
        {
            int slot = FindEmptyProjectileSlot(state);
            if (slot < 0) return;

            ref var proj = ref state.Projectiles[slot];
            proj.ProjectileId = state.NextProjectileId++;
            proj.SourceCombatId = sourceCombatId;
            proj.TargetCombatId = CombatUnit.InvalidId;
            proj.Type = ProjectileType.AreaTarget;
            proj.DamageType = damageType;
            proj.Damage = damage;
            proj.IsCrit = isCrit;
            proj.TargetCol = targetCol;
            proj.TargetRow = targetRow;
            proj.AreaRadius = areaRadius;
            proj.RemainingFrames = travelFrames;
            proj.IsActive = true;

            if (slot >= state.ProjectileCount)
                state.ProjectileCount = slot + 1;

            state.EventQueue?.PushProjectileSpawned(sourceCombatId, CombatUnit.InvalidId, ProjectileType.AreaTarget,
                targetCol, targetRow, projectileId: proj.ProjectileId);
        }

        // ── Linear 충돌 헬퍼 ──

        private static void HitOccupant(CombatMatchState state, ref Projectile proj, int col, int row, int srcIdx, byte srcTeam)
        {
            int occupantId = state.GetUnitAtGrid(col, row);
            if (occupantId == CombatUnit.InvalidId) return;

            int occIdx = state.FindUnitIndex(occupantId);
            if (occIdx < 0) return;

            ref var occupant = ref state.Units[occIdx];
            if (!occupant.IsValidTarget || occupant.TeamIndex == srcTeam) return;

            // HitMask로 중복 피격 방지
            int bitIndex = occIdx % 64;
            long bit = 1L << bitIndex;
            if ((proj.HitMask & bit) != 0) return;

            int finalDamage = DamageSystem.CalculateDamage(proj.Damage, proj.DamageType, ref occupant);
            DamageSystem.ApplyDamage(state, ref occupant, finalDamage, isCrit: proj.IsCrit);
            DamageSystem.ChargeMana(ref occupant, DamageSystem.ManaGainOnHit);
            proj.HitMask |= bit;

            // 흡혈
            if (srcIdx >= 0 && state.Units[srcIdx].IsAlive)
            {
                DamageSystem.ApplyLifeSteal(ref state.Units[srcIdx], finalDamage);
            }
        }

        // ── 유틸리티 ──

        private static int FindEmptyProjectileSlot(CombatMatchState state)
        {
            for (int i = 0; i < CombatMatchState.MaxProjectiles; i++)
            {
                if (!state.Projectiles[i].IsActive)
                    return i;
            }
            return -1; // 풀 가득 참
        }

        private static void RemoveProjectile(CombatMatchState state, int index)
        {
            // swap-back 제거
            int last = state.ProjectileCount - 1;
            if (index < last)
            {
                state.Projectiles[index] = state.Projectiles[last];
            }
            state.Projectiles[last].IsActive = false;
            state.ProjectileCount--;
            if (state.ProjectileCount < 0) state.ProjectileCount = 0;
        }
    }
}
