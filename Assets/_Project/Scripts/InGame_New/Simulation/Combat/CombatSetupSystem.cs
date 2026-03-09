namespace CookApps.AutoChess
{
    /// <summary>
    /// 전투 초기화 시스템. 보드 유닛을 CombatUnit으로 복제하고,
    /// 상대 유닛을 미러 배치하여 전투 그리드를 세팅.
    /// </summary>
    public static class CombatSetupSystem
    {
        /// <summary>매치 하나의 전투 세팅. 양쪽 보드 유닛을 CombatUnit으로 복제.</summary>
        public static CombatMatchState SetupMatch(GameWorld world, byte matchIndex, byte playerA, byte playerB)
        {
            var state = CombatMatchState.Create(matchIndex, playerA, playerB);
            state.EventQueue = world.EventQueue;

            // PlayerA 유닛: 하단 배치 (row 0-3)
            SpawnTeamUnits(world, state, playerA, teamIndex: 0, mirrorGrid: false);

            // PlayerB 유닛: 상단 배치 (미러링, row 4-7)
            SpawnTeamUnits(world, state, playerB, teamIndex: 1, mirrorGrid: true);

            state.AliveCountA = CountAliveByTeam(state, 0);
            state.AliveCountB = CountAliveByTeam(state, 1);

            return state;
        }

        /// <summary>한 팀의 보드 유닛을 CombatUnit으로 복제하여 전투 그리드에 배치</summary>
        private static void SpawnTeamUnits(GameWorld world, CombatMatchState state,
            byte playerIndex, byte teamIndex, bool mirrorGrid)
        {
            var boardSlots = world.BoardSlots[playerIndex];

            for (int i = 0; i < world.BoardSize; i++)
            {
                int entityId = boardSlots[i];
                if (entityId == UnitData.InvalidId) continue;

                int unitIndex = world.FindUnitIndex(entityId);
                if (unitIndex < 0) continue;

                ref var srcUnit = ref world.Units[unitIndex];
                if (!srcUnit.IsValid) continue;

                // 멀티타일 중복 스폰 방지: anchor 좌표(BoardCol, BoardRow)의 슬롯 인덱스와 일치할 때만 스폰
                int anchorIndex = BoardHelper.ToIndex(srcUnit.BoardCol, srcUnit.BoardRow);
                if (i != anchorIndex) continue;

                // 그리드 좌표 계산
                BoardHelper.FromIndex(i, out int col, out int row);

                int gridCol, gridRow;
                if (mirrorGrid)
                {
                    BoardHelper.MirrorPosition(col, row, out gridCol, out gridRow);
                }
                else
                {
                    gridCol = col;
                    gridRow = row;
                }

                // CombatUnit 생성
                int combatId = state.NextCombatId++;
                int slotIndex = state.UnitCount++;

                ref var combatUnit = ref state.Units[slotIndex];
                combatUnit.CombatId = combatId;
                combatUnit.SourceEntityId = entityId;
                combatUnit.ChampionSpecId = srcUnit.ChampionSpecId;
                combatUnit.StarLevel = srcUnit.StarLevel;
                combatUnit.OwnerIndex = playerIndex;
                combatUnit.TeamIndex = teamIndex;
                combatUnit.GridCol = (byte)gridCol;
                combatUnit.GridRow = (byte)gridRow;
                combatUnit.State = CombatState.Idle;
                combatUnit.IsAlive = true;

                // 스탯 복사
                combatUnit.MaxHP = srcUnit.MaxHP;
                combatUnit.CurrentHP = srcUnit.MaxHP;
                combatUnit.Attack = srcUnit.Attack;
                combatUnit.Armor = srcUnit.Armor;
                combatUnit.MagicResist = srcUnit.MagicResist;
                combatUnit.AttackSpeed = srcUnit.AttackSpeed;
                combatUnit.AttackRange = srcUnit.AttackRange;
                combatUnit.MoveSpeed = srcUnit.MoveSpeed;
                combatUnit.MaxMana = srcUnit.MaxMana;
                combatUnit.CurrentMana = 0;
                combatUnit.CritChance = 25;       // 기본 25%
                combatUnit.CritMultiplier = 150;   // 기본 1.5x
                combatUnit.TraitFlags = srcUnit.TraitFlags;

                // 크기 복사
                combatUnit.SizeW = srcUnit.SizeW > 0 ? srcUnit.SizeW : (byte)1;
                combatUnit.SizeH = srcUnit.SizeH > 0 ? srcUnit.SizeH : (byte)1;

                combatUnit.CurrentTargetId = CombatUnit.InvalidId;
                combatUnit.AttackCooldown = 0;
                combatUnit.MoveTimer = 0;
                combatUnit.MoveDuration = 0;

                // 스킬 ID 설정 (ChampionSpec에서 복사)
                combatUnit.SkillSpecId = FindSkillId(world, srcUnit.ChampionSpecId);
                combatUnit.SkillCastTimer = 0;

                // 범위 기본공격 패턴 플래그
                combatUnit.HasAreaAttack = AreaAttackRegistry.TryGetPattern(srcUnit.ChampionSpecId, out _);

                // 아이템 스탯 적용 (기본 스탯 → 아이템 순서)
                ItemSystem.ApplyItemStats(world, ref combatUnit, ref srcUnit);

                // 그리드에 등록 (multi-tile)
                state.SetGridMulti(gridCol, gridRow, combatUnit.SizeW, combatUnit.SizeH, combatId);

                if (CombatLogger.Enabled) CombatLogger.LogSpawn(combatId, teamIndex, gridCol, gridRow, combatUnit.MaxHP, combatUnit.Attack, combatUnit.AttackRange);
            }
        }

        /// <summary>ChampionSpec에서 SkillId 조회</summary>
        private static int FindSkillId(GameWorld world, int championSpecId)
        {
            if (world.Pool == null) return 0;
            for (int i = 0; i < world.Pool.SpecCount; i++)
            {
                if (world.Pool.Specs[i].ChampionId == championSpecId)
                    return world.Pool.Specs[i].SkillId;
            }
            return 0;
        }

        /// <summary>팀별 생존 유닛 수</summary>
        public static int CountAliveByTeam(CombatMatchState state, byte teamIndex)
        {
            int count = 0;
            for (int i = 0; i < state.UnitCount; i++)
            {
                if (state.Units[i].TeamIndex == teamIndex && state.Units[i].IsAlive)
                    count++;
            }
            return count;
        }

        // ── PvE 매치 셋업 ──

        /// <summary>PvE 매치 세팅. 플레이어 보드 유닛 + PvE 적 데이터로 전투 구성.</summary>
        public static CombatMatchState SetupPvEMatch(GameWorld world, byte matchIndex, byte playerIndex)
        {
            var state = CombatMatchState.Create(matchIndex, playerIndex, 0xFF);
            state.EventQueue = world.EventQueue;

            // 플레이어 유닛 (team 0, 하단)
            SpawnTeamUnits(world, state, playerIndex, teamIndex: 0, mirrorGrid: false);

            // PvE 적 (team 1, 상단, 미러링)
            SpawnPvEEnemies(world, state);

            state.AliveCountA = CountAliveByTeam(state, 0);
            state.AliveCountB = CountAliveByTeam(state, 1);

            return state;
        }

        /// <summary>PvE 적 유닛을 CombatUnit으로 직접 생성 (보드 거치지 않음)</summary>
        private static void SpawnPvEEnemies(GameWorld world, CombatMatchState state)
        {
            for (int i = 0; i < world.PvEEnemyCount; i++)
            {
                ref var enemy = ref world.PvEEnemies[i];

                // PvE 좌표는 이미 전투 그리드 기준 (예: (0,6), (3,4))
                int gridCol = enemy.GridCol;
                int gridRow = enemy.GridRow;

                int combatId = state.NextCombatId++;
                int slotIndex = state.UnitCount++;

                ref var unit = ref state.Units[slotIndex];
                unit.CombatId = combatId;
                unit.SourceEntityId = -1;
                unit.ChampionSpecId = enemy.ChampionSpecId;
                unit.StarLevel = 1;
                unit.OwnerIndex = 0xFF;
                unit.TeamIndex = 1;
                unit.GridCol = (byte)gridCol;
                unit.GridRow = (byte)gridRow;
                unit.SizeW = enemy.SizeW;
                unit.SizeH = enemy.SizeH;
                unit.State = CombatState.Idle;
                unit.IsAlive = true;

                unit.MaxHP = enemy.MaxHP;
                unit.CurrentHP = enemy.MaxHP;
                unit.Attack = enemy.Attack;
                unit.Armor = enemy.Armor;
                unit.MagicResist = enemy.MagicResist;
                unit.AttackSpeed = enemy.AttackSpeed;
                unit.AttackRange = enemy.AttackRange;
                unit.MoveSpeed = enemy.MoveSpeed;
                unit.MaxMana = enemy.MaxMana;
                unit.CurrentMana = 0;
                unit.CritChance = 25;
                unit.CritMultiplier = 150;
                unit.TraitFlags = enemy.TraitFlags;
                unit.SkillSpecId = enemy.SkillSpecId;
                unit.HasAreaAttack = AreaAttackRegistry.TryGetPattern(enemy.ChampionSpecId, out _);
                unit.CurrentTargetId = CombatUnit.InvalidId;
                unit.AttackCooldown = 0;
                unit.MoveTimer = 0;
                unit.MoveDuration = 0;
                unit.SkillCastTimer = 0;

                state.SetGridMulti(gridCol, gridRow, enemy.SizeW, enemy.SizeH, combatId);

                if (CombatLogger.Enabled) CombatLogger.LogSpawn(combatId, 1, gridCol, gridRow, unit.MaxHP, unit.Attack, unit.AttackRange);
            }
        }

        /// <summary>매치메이킹: 4인 → 2개 1v1 매치 배정 (간단한 라운드 로빈)</summary>
        public static void AssignMatches(GameWorld world)
        {
            // 생존 플레이어 목록
            int aliveCount = 0;
            byte[] alivePlayers = new byte[world.MaxPlayers];
            for (int i = 0; i < world.MaxPlayers; i++)
            {
                if (i < world.Config.PlayerCount && world.Players[i].IsAlive)
                    alivePlayers[aliveCount++] = (byte)i;
            }

            if (aliveCount <= 1) return;

            // 셔플 (매칭 다양성)
            world.RNG.Shuffle(alivePlayers, aliveCount);

            // 매치 배정
            if (aliveCount >= 4)
            {
                // 4인: 2개 매치
                world.Matches[0] = new CombatMatch
                {
                    PlayerA = alivePlayers[0], PlayerB = alivePlayers[1],
                    Winner = 0xFF
                };
                world.Matches[1] = new CombatMatch
                {
                    PlayerA = alivePlayers[2], PlayerB = alivePlayers[3],
                    Winner = 0xFF
                };
            }
            else if (aliveCount == 3)
            {
                // 3인: 1 실제 매치 + 1 고스트 매치
                world.Matches[0] = new CombatMatch
                {
                    PlayerA = alivePlayers[0], PlayerB = alivePlayers[1],
                    Winner = 0xFF
                };
                world.Matches[1] = new CombatMatch
                {
                    PlayerA = alivePlayers[2], PlayerB = alivePlayers[0], // 고스트: P0 보드 복제
                    IsGhostMatch = true,
                    Winner = 0xFF
                };
            }
            else // 2인
            {
                world.Matches[0] = new CombatMatch
                {
                    PlayerA = alivePlayers[0], PlayerB = alivePlayers[1],
                    Winner = 0xFF
                };
                world.Matches[1] = new CombatMatch { IsFinished = true, Winner = 0xFF };
            }
        }
    }
}
