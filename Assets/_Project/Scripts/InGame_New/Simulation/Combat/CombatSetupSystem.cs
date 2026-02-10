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

            for (int i = 0; i < PlayerBoard.BoardSize; i++)
            {
                int entityId = boardSlots[i];
                if (entityId == UnitData.InvalidId) continue;

                int unitIndex = world.FindUnitIndex(entityId);
                if (unitIndex < 0) continue;

                ref var srcUnit = ref world.Units[unitIndex];
                if (!srcUnit.IsValid) continue;

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

                combatUnit.CurrentTargetId = CombatUnit.InvalidId;
                combatUnit.AttackCooldown = 0;
                combatUnit.MoveCooldown = 0;

                // 스킬 ID 설정 (ChampionSpec에서 복사)
                combatUnit.SkillSpecId = FindSkillId(world, srcUnit.ChampionSpecId);
                combatUnit.SkillCastTimer = 0;

                // 아이템 스탯 적용 (기본 스탯 → 아이템 순서)
                ItemSystem.ApplyItemStats(world, ref combatUnit, ref srcUnit);

                // 그리드에 등록
                state.SetGrid(gridCol, gridRow, combatId);
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

        /// <summary>매치메이킹: 4인 → 2개 1v1 매치 배정 (간단한 라운드 로빈)</summary>
        public static void AssignMatches(GameWorld world)
        {
            // 생존 플레이어 목록
            int aliveCount = 0;
            byte[] alivePlayers = new byte[GameWorld.MaxPlayers];
            for (int i = 0; i < GameWorld.MaxPlayers; i++)
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
