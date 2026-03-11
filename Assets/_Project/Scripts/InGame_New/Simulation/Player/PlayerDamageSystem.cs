namespace CookApps.AutoChess
{
    /// <summary>
    /// 플레이어 데미지 시스템. 전투 패배 시 HP 데미지 계산 및 탈락 처리.
    /// 데미지 = BaseDamage(스테이지) + 생존 적 유닛 별 레벨 합산.
    /// </summary>
    public static class PlayerDamageSystem
    {
        /// <summary>
        /// 전투 결과 처리. 패배 플레이어에게 데미지 적용.
        /// </summary>
        public static void ProcessMatchResult(GameWorld world, int matchIndex)
        {
            ref var match = ref world.Matches[matchIndex];
            var state = world.CombatMatchStates[matchIndex];

            if (state == null || !state.IsFinished) return;

            // 매치 결과를 CombatMatch에 반영
            match.Winner = state.Winner;
            match.IsFinished = true;

            if (state.Winner == 0xFF)
            {
                // 무승부: 양쪽 데미지 없음
                return;
            }

            // 승자/패자 결정
            byte winnerTeam = state.Winner;
            byte loserPlayerIndex;

            if (winnerTeam == 0)
            {
                // TeamA 승리 → PlayerB 패배
                loserPlayerIndex = match.PlayerB;
            }
            else
            {
                // TeamB 승리 → PlayerA 패배
                loserPlayerIndex = match.PlayerA;
            }

            // 고스트 매치에서 패배한 경우에도 데미지 적용
            int damage = CalculatePlayerDamage(state, world.CurrentStage, winnerTeam);
            ApplyPlayerDamage(world, loserPlayerIndex, damage);
        }

        /// <summary>
        /// 플레이어 데미지 계산.
        /// BaseDamage = max(0, Stage - 1)
        /// SurvivingUnitDamage = 생존 유닛 별 레벨 합산 (1★=1, 2★=2, 3★=3)
        /// </summary>
        public static int CalculatePlayerDamage(CombatMatchState state, int currentStage, byte winnerTeam)
        {
            int baseDamage = currentStage > 1 ? currentStage - 1 : 0;
            int unitDamage = 0;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;
                if (unit.TeamIndex != winnerTeam) continue;

                unitDamage += unit.StarLevel; // 1★=1, 2★=2, 3★=3
            }

            return baseDamage + unitDamage;
        }

        /// <summary>플레이어에게 데미지 적용. HP ≤ 0이면 탈락 처리.</summary>
        public static void ApplyPlayerDamage(GameWorld world, byte playerIndex, int damage)
        {
            if (playerIndex >= world.MaxPlayers) return;
            if (!world.Players[playerIndex].IsAlive) return;

            world.Players[playerIndex].HP -= damage;

            if (world.Players[playerIndex].HP <= 0)
            {
                world.Players[playerIndex].HP = 0;
                EliminatePlayer(world, playerIndex);
            }
        }

        /// <summary>플레이어 탈락 처리</summary>
        private static void EliminatePlayer(GameWorld world, byte playerIndex)
        {
            world.Players[playerIndex].IsAlive = false;
            world.Players[playerIndex].IsEliminated = true;
            world.AlivePlayerCount--;

            // 풀에 유닛 반환
            ShopSystem.ReturnAllToPool(world, playerIndex);

            // 순위 계산: 남은 생존자 수 + 1 = 탈락 순위
            world.Players[playerIndex].Rank = (byte)(world.AlivePlayerCount + 1);

            world.EventQueue?.PushPlayerEliminated(playerIndex, world.Players[playerIndex].Rank);

            // 마지막 1명 남으면 우승
            if (world.AlivePlayerCount == 1)
            {
                for (int i = 0; i < world.MaxPlayers; i++)
                {
                    if (world.Players[i].IsAlive)
                    {
                        world.Players[i].Rank = 1;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 연승/연패 업데이트.
        /// 승리 시: WinStreak++, LoseStreak=0
        /// 패배 시: LoseStreak++, WinStreak=0
        /// </summary>
        public static void UpdateStreaks(GameWorld world, byte playerIndex, bool isWin)
        {
            if (playerIndex >= world.MaxPlayers) return;

            ref var economy = ref world.Economies[playerIndex];
            if (isWin)
            {
                economy.WinStreak++;
                economy.LoseStreak = 0;
                economy.TotalWins++;
            }
            else
            {
                economy.LoseStreak++;
                economy.WinStreak = 0;
                economy.TotalLosses++;
            }
        }
    }
}
