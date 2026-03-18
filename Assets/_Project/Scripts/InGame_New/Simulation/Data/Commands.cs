namespace CookApps.AutoChess
{
    /// <summary>
    /// 플레이어 입력 커맨드. 모든 입력은 이 구조체로 변환되어 시뮬레이션에 전달.
    /// 네트워크 동기화 시 이 커맨드만 전송하면 결정론적 시뮬레이션 보장.
    /// </summary>
    public struct GameCommand
    {
        public CommandType Type;
        public byte PlayerIndex;
        public int Param0;
        public int Param1;
        public int Param2;
        public int Param3;

        // ── 팩토리 메서드 ──

        /// <summary>벤치 → 보드 배치</summary>
        /// <param name="entityId">유닛 EntityId</param>
        /// <param name="col">보드 열 (0-6)</param>
        /// <param name="row">보드 행 (0-3)</param>
        public static GameCommand PlaceUnit(byte player, int entityId, byte col, byte row)
        {
            return new GameCommand
            {
                Type = CommandType.PlaceUnit,
                PlayerIndex = player,
                Param0 = entityId,
                Param1 = col,
                Param2 = row,
            };
        }

        /// <summary>보드 내 위치 이동 (목표 위치에 유닛 있으면 교환)</summary>
        public static GameCommand MoveUnit(byte player, int entityId, byte toCol, byte toRow)
        {
            return new GameCommand
            {
                Type = CommandType.MoveUnit,
                PlayerIndex = player,
                Param0 = entityId,
                Param1 = toCol,
                Param2 = toRow,
            };
        }

        /// <summary>보드 → 벤치 회수</summary>
        public static GameCommand WithdrawUnit(byte player, int entityId)
        {
            return new GameCommand
            {
                Type = CommandType.WithdrawUnit,
                PlayerIndex = player,
                Param0 = entityId,
            };
        }

        /// <summary>두 유닛 위치 교환 (보드↔보드, 보드↔벤치, 벤치↔벤치)</summary>
        public static GameCommand SwapUnits(byte player, int entityA, int entityB)
        {
            return new GameCommand
            {
                Type = CommandType.SwapUnits,
                PlayerIndex = player,
                Param0 = entityA,
                Param1 = entityB,
            };
        }

        /// <summary>상점에서 챔피언 구매</summary>
        /// <param name="shopSlot">상점 슬롯 인덱스 (0-4)</param>
        public static GameCommand BuyUnit(byte player, byte shopSlot)
        {
            return new GameCommand
            {
                Type = CommandType.BuyUnit,
                PlayerIndex = player,
                Param0 = shopSlot,
            };
        }

        /// <summary>유닛 판매 (골드 환급)</summary>
        public static GameCommand SellUnit(byte player, int entityId)
        {
            return new GameCommand
            {
                Type = CommandType.SellUnit,
                PlayerIndex = player,
                Param0 = entityId,
            };
        }

        /// <summary>상점 리롤 (골드 소비)</summary>
        public static GameCommand RerollShop(byte player)
        {
            return new GameCommand
            {
                Type = CommandType.RerollShop,
                PlayerIndex = player,
            };
        }

        /// <summary>상점 잠금 토글</summary>
        public static GameCommand LockShop(byte player)
        {
            return new GameCommand
            {
                Type = CommandType.LockShop,
                PlayerIndex = player,
            };
        }

        /// <summary>XP 구매</summary>
        public static GameCommand BuyXP(byte player)
        {
            return new GameCommand
            {
                Type = CommandType.BuyXP,
                PlayerIndex = player,
            };
        }

        /// <summary>준비 완료 (타이머 스킵)</summary>
        public static GameCommand Ready(byte player)
        {
            return new GameCommand
            {
                Type = CommandType.Ready,
                PlayerIndex = player,
            };
        }

        /// <summary>커맨더 스킬 사용</summary>
        public static GameCommand UseCommanderSkill(byte player, byte skillIndex, byte targetCol, byte targetRow)
        {
            return new GameCommand
            {
                Type = CommandType.UseCommanderSkill,
                PlayerIndex = player,
                Param0 = skillIndex,
                Param1 = targetCol,
                Param2 = targetRow,
            };
        }

        /// <summary>아이템 장착 (인벤토리 → 유닛)</summary>
        public static GameCommand EquipItem(byte player, int itemInstanceId, int targetEntityId)
        {
            return new GameCommand
            {
                Type = CommandType.EquipItem,
                PlayerIndex = player,
                Param0 = itemInstanceId,
                Param1 = targetEntityId,
            };
        }

        /// <summary>아이템 해제 (유닛 → 인벤토리)</summary>
        public static GameCommand UnequipItem(byte player, int itemInstanceId)
        {
            return new GameCommand
            {
                Type = CommandType.UnequipItem,
                PlayerIndex = player,
                Param0 = itemInstanceId,
            };
        }

        /// <summary>준비 페이즈 시너지 타겟 설정 (Param1>0: 유닛 부여, Param1=-1: 오브젝트 위치 이동)</summary>
        public static GameCommand SetSynergyPrepTarget(byte player, int traitId, int targetEntityId,
            int col = 0, int row = 0)
            => new GameCommand
            {
                Type = CommandType.SetSynergyPrepTarget,
                PlayerIndex = player,
                Param0 = traitId,
                Param1 = targetEntityId,
                Param2 = col,
                Param3 = row,
            };

        /// <summary>튜토리얼 적 스폰 (전투 중 동적 추가)</summary>
        public static GameCommand SpawnTutorialEnemy(byte playerIndex, int monsterSpecId, int col, int row)
            => new GameCommand
            {
                Type = CommandType.SpawnTutorialEnemy,
                PlayerIndex = playerIndex,
                Param0 = monsterSpecId,
                Param1 = col,
                Param2 = row,
            };
    }
}
