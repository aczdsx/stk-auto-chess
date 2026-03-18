namespace CookApps.AutoChess
{
    /// <summary>
    /// 커맨드 처리기. 플레이어 입력 커맨드를 검증하고 적절한 시스템으로 위임.
    /// 페이즈별로 허용되는 커맨드가 다름.
    /// </summary>
    public static class CommandProcessor
    {
        /// <summary>커맨드 배열 일괄 처리</summary>
        public static void ProcessCommands(GameWorld world, GameCommand[] commands, int count)
        {
            for (int i = 0; i < count; i++)
            {
                ProcessCommand(world, in commands[i]);
            }
        }

        /// <summary>단일 커맨드 처리</summary>
        public static void ProcessCommand(GameWorld world, in GameCommand cmd)
        {
            // 유효성 검사
            if (cmd.PlayerIndex >= world.MaxPlayers) return;
            if (!world.Players[cmd.PlayerIndex].IsAlive) return;
            if (!IsCommandAllowedInPhase(cmd.Type, world.CurrentPhase)) return;

            switch (cmd.Type)
            {
                case CommandType.PlaceUnit:
                    ProcessPlaceUnit(world, in cmd);
                    break;
                case CommandType.MoveUnit:
                    ProcessMoveUnit(world, in cmd);
                    break;
                case CommandType.WithdrawUnit:
                    ProcessWithdrawUnit(world, in cmd);
                    break;
                case CommandType.SwapUnits:
                    ProcessSwapUnits(world, in cmd);
                    break;
                case CommandType.BuyUnit:
                    ProcessBuyUnit(world, in cmd);
                    break;
                case CommandType.SellUnit:
                    ProcessSellUnit(world, in cmd);
                    break;
                case CommandType.RerollShop:
                    ProcessRerollShop(world, in cmd);
                    break;
                case CommandType.LockShop:
                    ProcessLockShop(world, in cmd);
                    break;
                case CommandType.BuyXP:
                    ProcessBuyXP(world, in cmd);
                    break;
                case CommandType.Ready:
                    ProcessReady(world, in cmd);
                    break;
                case CommandType.UseCommanderSkill:
                    ProcessCommanderSkill(world, in cmd);
                    break;
                case CommandType.EquipItem:
                    ProcessEquipItem(world, in cmd);
                    break;
                case CommandType.UnequipItem:
                    ProcessUnequipItem(world, in cmd);
                    break;
                case CommandType.SpawnTutorialEnemy:
                    ProcessSpawnTutorialEnemy(world, in cmd);
                    break;
                case CommandType.SetSynergyPrepTarget:
                    ProcessSetSynergyPrepTarget(world, in cmd);
                    break;
            }
        }

        /// <summary>페이즈별 커맨드 허용 여부</summary>
        private static bool IsCommandAllowedInPhase(CommandType type, GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Preparation:
                    // 준비 페이즈: 유닛 조작, 상점, 아이템, XP, Ready 가능
                    return type != CommandType.UseCommanderSkill;

                case GamePhase.Combat:
                    // 전투 페이즈: 커맨더 스킬 + 아이템 장착 + 튜토리얼 적 스폰만 가능
                    return type == CommandType.UseCommanderSkill ||
                           type == CommandType.EquipItem ||
                           type == CommandType.SpawnTutorialEnemy;

                case GamePhase.Result:
                    // 결과 페이즈: 입력 불가
                    return false;

                case GamePhase.SharedDraft:
                    // 드래프트: 특수 처리 (별도 커맨드)
                    return false;

                default:
                    return false;
            }
        }

        // ── 개별 커맨드 처리 ──

        /// <summary>보드 변경 시 시너지 재계산 + prep 동기화 + 이벤트 발행</summary>
        private static void OnBoardChanged(GameWorld world, byte playerIndex)
        {
            SynergySystem.Recalculate(world, playerIndex);
            SynergySystem.SyncPrepBehaviors(world, playerIndex);
            world.EventQueue.PushSynergyUpdated(playerIndex);
        }

        private static void ProcessPlaceUnit(GameWorld world, in GameCommand cmd)
        {
            int entityId = cmd.Param0;
            byte col = (byte)cmd.Param1;
            byte row = (byte)cmd.Param2;

            if (BoardSystem.PlaceUnit(world, cmd.PlayerIndex, entityId, col, row))
            {
                OnBoardChanged(world, cmd.PlayerIndex);
            }
        }

        private static void ProcessMoveUnit(GameWorld world, in GameCommand cmd)
        {
            int entityId = cmd.Param0;
            byte toCol = (byte)cmd.Param1;
            byte toRow = (byte)cmd.Param2;

            if (BoardSystem.MoveUnit(world, cmd.PlayerIndex, entityId, toCol, toRow))
            {
                // 보드 내 이동은 시너지 유닛 수 변화 없으므로 Recalculate 불필요
                // PrepBehavior의 OnBoardChanged만 호출 (오브젝트 위치 충돌 감지)
                SynergySystem.NotifyPrepBoardChanged(world, cmd.PlayerIndex);
            }
        }

        private static void ProcessWithdrawUnit(GameWorld world, in GameCommand cmd)
        {
            int entityId = cmd.Param0;

            if (BoardSystem.WithdrawUnit(world, cmd.PlayerIndex, entityId))
            {
                OnBoardChanged(world, cmd.PlayerIndex);
            }
        }

        private static void ProcessSwapUnits(GameWorld world, in GameCommand cmd)
        {
            int entityA = cmd.Param0;
            int entityB = cmd.Param1;

            if (BoardSystem.SwapUnits(world, cmd.PlayerIndex, entityA, entityB))
            {
                OnBoardChanged(world, cmd.PlayerIndex);
            }
        }

        private static void ProcessBuyUnit(GameWorld world, in GameCommand cmd)
        {
            int shopSlotIndex = cmd.Param0;
            ShopSystem.TryPurchase(world, cmd.PlayerIndex, shopSlotIndex);
        }

        private static void ProcessSellUnit(GameWorld world, in GameCommand cmd)
        {
            int entityId = cmd.Param0;
            ShopSystem.TrySellUnit(world, cmd.PlayerIndex, entityId);
        }

        private static void ProcessRerollShop(GameWorld world, in GameCommand cmd)
        {
            ShopSystem.TryReroll(world, cmd.PlayerIndex);
        }

        private static void ProcessLockShop(GameWorld world, in GameCommand cmd)
        {
            ShopSystem.ToggleLock(world, cmd.PlayerIndex);
        }

        private static void ProcessBuyXP(GameWorld world, in GameCommand cmd)
        {
            EconomySystem.TryBuyXP(world, cmd.PlayerIndex);
        }

        private static void ProcessReady(GameWorld world, in GameCommand cmd)
        {
            world.Players[cmd.PlayerIndex].IsReady = true;
        }

        private static void ProcessEquipItem(GameWorld world, in GameCommand cmd)
        {
            int itemInstanceId = cmd.Param0;
            int targetEntityId = cmd.Param1;
            ItemSystem.TryEquip(world, cmd.PlayerIndex, itemInstanceId, targetEntityId);
        }

        private static void ProcessUnequipItem(GameWorld world, in GameCommand cmd)
        {
            // 해제는 Preparation 페이즈에서만 허용 (IsCommandAllowedInPhase에서 이미 필터됨)
            int itemInstanceId = cmd.Param0;
            ItemSystem.TryUnequip(world, cmd.PlayerIndex, itemInstanceId);
        }

        private static void ProcessCommanderSkill(GameWorld world, in GameCommand cmd)
        {
            // TODO: 커맨더 스킬 시스템
        }

        private static void ProcessSetSynergyPrepTarget(GameWorld world, in GameCommand cmd)
        {
            int traitId = cmd.Param0;
            int idx = SynergySystem.FindPrepBehavior(world, cmd.PlayerIndex, traitId);
            if (idx >= 0)
                world.PrepBehaviors[cmd.PlayerIndex][idx].HandleCommand(world, in cmd);
        }

        private static void ProcessSpawnTutorialEnemy(GameWorld world, in GameCommand cmd)
        {
            // PlayerIndex가 포함된 매치를 찾아 튜토리얼 적 스폰
            for (int i = 0; i < GameWorld.MaxCombatMatches; i++)
            {
                var matchState = world.CombatMatchStates[i];
                if (matchState == null) continue;
                if (matchState.IsFinished) continue;
                if (matchState.PlayerA == cmd.PlayerIndex || matchState.PlayerB == cmd.PlayerIndex)
                {
                    CombatSetupSystem.SpawnTutorialUnit(ref matchState, cmd.Param0, cmd.Param1, cmd.Param2);
                    return;
                }
            }
        }
    }
}
