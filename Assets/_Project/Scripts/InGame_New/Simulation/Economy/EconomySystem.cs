namespace CookApps.AutoChess
{
    /// <summary>
    /// 경제 시스템. 골드 수입, 이자, 연승/연패 보너스, XP, 레벨업.
    /// 매 라운드 시작(Preparation 진입) 시 GrantRoundIncome 호출.
    /// </summary>
    public static class EconomySystem
    {
        // ── 라운드 수입 ──

        /// <summary>
        /// 라운드 시작 시 수입 지급 (모든 생존 플레이어).
        /// BaseIncome + Interest + StreakBonus + VictoryBonus(이전 라운드 승리 시).
        /// </summary>
        public static void GrantRoundIncome(GameWorld world)
        {
            if (!world.Config.EnableEconomy) return;

            for (int i = 0; i < world.Config.PlayerCount; i++)
            {
                if (!world.Players[i].IsAlive) continue;
                GrantIncomeToPlayer(world, (byte)i);
            }
        }

        /// <summary>개별 플레이어 수입 지급</summary>
        public static void GrantIncomeToPlayer(GameWorld world, byte playerIndex)
        {
            ref var economy = ref world.Economies[playerIndex];
            var config = world.Config;

            int baseIncome = GetBaseIncome(config, world.CurrentStage);
            int interest = CalculateInterest(economy.Gold, config.MaxInterest);
            int streakBonus = GetStreakBonus(config, economy.WinStreak, economy.LoseStreak);

            int totalIncome = baseIncome + interest + streakBonus;
            economy.Gold += totalIncome;

            world.EventQueue?.PushGoldChanged(playerIndex, economy.Gold, totalIncome);
        }

        /// <summary>스테이지별 기본 수입</summary>
        public static int GetBaseIncome(GameConfig config, int stage)
        {
            if (stage < 0) return config.BaseIncome;
            if (stage >= config.BaseIncomeByStage.Length)
                return config.BaseIncome;
            return config.BaseIncomeByStage[stage];
        }

        /// <summary>이자 계산: floor(Gold / 10), 최대 MaxInterest</summary>
        public static int CalculateInterest(int currentGold, int maxInterest)
        {
            int interest = currentGold / 10;
            if (interest > maxInterest) interest = maxInterest;
            return interest;
        }

        /// <summary>연승/연패 보너스</summary>
        public static int GetStreakBonus(GameConfig config, int winStreak, int loseStreak)
        {
            int streak = winStreak > 0 ? winStreak : loseStreak;
            if (streak <= 0) return 0;

            if (streak >= config.MaxStreakBonusIndex)
                return config.StreakBonusTable[config.MaxStreakBonusIndex];

            if (streak < config.StreakBonusTable.Length)
                return config.StreakBonusTable[streak];

            return config.StreakBonusTable[config.StreakBonusTable.Length - 1];
        }

        /// <summary>승리 보너스 (전투 승리 시 호출)</summary>
        public static void GrantVictoryBonus(GameWorld world, byte playerIndex)
        {
            if (!world.Config.EnableEconomy) return;
            world.Economies[playerIndex].Gold += world.Config.VictoryBonusGold;
        }

        // ── XP / 레벨 ──

        /// <summary>라운드 종료 시 자동 XP 지급 (모든 생존 플레이어)</summary>
        public static void GrantRoundXP(GameWorld world)
        {
            if (!world.Config.EnableEconomy) return;

            for (int i = 0; i < world.Config.PlayerCount; i++)
            {
                if (!world.Players[i].IsAlive) continue;
                AddXP(world, (byte)i, world.Config.XPPerRound);
            }
        }

        /// <summary>XP 구매 (BuyXP 커맨드)</summary>
        public static bool TryBuyXP(GameWorld world, byte playerIndex)
        {
            ref var economy = ref world.Economies[playerIndex];
            var config = world.Config;

            // 이미 최대 레벨
            if (economy.Level >= config.MaxLevel) return false;

            // 골드 부족
            if (economy.Gold < config.XPPurchaseCost) return false;

            economy.Gold -= config.XPPurchaseCost;
            AddXP(world, playerIndex, config.XPPurchaseAmount);
            return true;
        }

        /// <summary>XP 추가 및 레벨업 체크</summary>
        public static void AddXP(GameWorld world, byte playerIndex, int amount)
        {
            ref var economy = ref world.Economies[playerIndex];
            var config = world.Config;

            if (economy.Level >= config.MaxLevel) return;

            economy.XP += amount;

            // 레벨업 체크 (연속 레벨업 가능)
            while (economy.Level < config.MaxLevel)
            {
                int required = GetXPForNextLevel(config, economy.Level);
                if (required <= 0 || economy.XP < required) break;

                economy.XP -= required;
                economy.Level++;

                world.EventQueue?.PushLevelUp(playerIndex, economy.Level);
            }

            // 최대 레벨 도달 시 XP 초기화
            if (economy.Level >= config.MaxLevel)
            {
                economy.XP = 0;
            }
        }

        /// <summary>현재 레벨에서 다음 레벨까지 필요한 XP</summary>
        public static int GetXPForNextLevel(GameConfig config, byte currentLevel)
        {
            int nextLevel = currentLevel + 1;
            if (nextLevel >= config.XPTable.Length) return 0;
            return config.XPTable[nextLevel];
        }

        /// <summary>현재 레벨의 필드 유닛 제한 수 (레벨 = 필드 수)</summary>
        public static int GetUnitCap(byte level)
        {
            return level; // 레벨 = 필드 유닛 수
        }

        // ── 골드 유틸리티 ──

        /// <summary>골드 차감 (검증 포함)</summary>
        public static bool TrySpendGold(ref PlayerEconomy economy, int amount)
        {
            if (economy.Gold < amount) return false;
            economy.Gold -= amount;
            return true;
        }

        /// <summary>판매 가격 계산: 1★=cost, 2★=cost×3, 3★=cost×9</summary>
        public static int GetSellPrice(byte cost, byte starLevel)
        {
            return starLevel switch
            {
                1 => cost,
                2 => cost * 3,
                3 => cost * 9,
                _ => cost,
            };
        }
    }
}
