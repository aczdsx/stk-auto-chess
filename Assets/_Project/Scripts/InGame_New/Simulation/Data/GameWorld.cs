namespace CookApps.AutoChess
{
    /// <summary>
    /// 전체 게임 상태 컨테이너.
    /// 모든 시뮬레이션 시스템은 이 객체를 통해 상태를 읽고 쓴다.
    /// 스냅샷 가능 (직렬화하면 게임 상태 복원 가능).
    /// </summary>
    public class GameWorld
    {
        // ── 상수 ──
        public const int MaxPlayers = 4;
        public const int MaxUnits = 128;
        public const int MaxCombatMatches = 2;    // 4인 → 2개 동시 매치
        public const int MaxCutsceneQueue = 8;

        // ── 글로벌 상태 ──
        public GamePhase CurrentPhase;
        public GameModeType GameMode;
        public int BoardWidth;
        public int BoardHeight;
        public int BoardSize;  // BoardWidth * BoardHeight
        public int CurrentStage;       // 스테이지 번호 (1, 2, 3, ...)
        public int CurrentRound;       // 라운드 번호 (1-1, 1-2, ...)
        public int PhaseTimerFrames;   // 현재 페이즈 남은 프레임
        public int PhaseElapsedFrames; // 현재 페이즈 경과 프레임
        public int TickRate;           // 프레임/초 (기본 30)
        public int FrameCount;         // 총 경과 프레임
        public int AlivePlayerCount;
        public bool IsGameOver;
        public DeterministicRNG RNG;

        // ── 플레이어 상태 ──
        public PlayerState[] Players;        // [MaxPlayers]
        public PlayerEconomy[] Economies;    // [MaxPlayers]

        // ── 보드 상태 ──
        // Boards[i]는 메타데이터(유닛수 등), 실제 슬롯은 아래 배열
        public PlayerBoard[] Boards;         // [MaxPlayers]
        public int[][] BoardSlots;           // [MaxPlayers][BoardSize] - EntityId
        public int[][] BenchSlots;           // [MaxPlayers][BenchSize] - EntityId

        // ── 상점 상태 ──
        public ShopSlot[][] Shops;           // [MaxPlayers][ShopSlotCount]
        public bool[] ShopLocked;            // [MaxPlayers]

        // ── 챔피언 풀 ──
        public ChampionPool Pool;            // 공유 챔피언 재고

        // ── 시너지 ──
        public PlayerSynergy[] Synergies;    // [MaxPlayers]
        public SynergySpec[] SynergySpecs;   // 시너지 스펙 데이터 (외부에서 주입)
        public int SynergySpecCount;

        // ── 아이템 ──
        public const int MaxItems = 64;
        public const int MaxItemInventory = 10;
        public ItemData[] Items;             // [MaxItems] 전체 아이템 인스턴스
        public int NextItemInstanceId;
        public ItemSpec[] ItemSpecs;         // 아이템 스펙 데이터 (외부에서 주입)
        public int ItemSpecCount;
        public int[][] ItemInventory;        // [MaxPlayers][MaxItemInventory] - ItemInstanceId
        public byte[] ItemInventoryCount;    // [MaxPlayers]

        // ── 유닛 저장소 ──
        public UnitData[] Units;             // [MaxUnits]
        public int NextEntityId;

        // ── 전투 상태 ──
        public bool IsCombatActive;
        public CombatMatch[] Matches;                  // [MaxCombatMatches] 매치 메타 정보
        public CombatMatchState[] CombatMatchStates;   // [MaxCombatMatches] 매치별 전투 상태

        // ── 컷씬 큐 ──
        public CutsceneRequest[] CutsceneQueue;  // [MaxCutsceneQueue]
        public int CutsceneCount;
        public int CutsceneCurrentIndex;
        public int CutsceneElapsedFrames;
        public bool IsCutscenePlaying;

        // ── PvE 적 데이터 ──
        public const int MaxPvEEnemies = 16;
        public PvEEnemyData[] PvEEnemies;    // 외부에서 주입, 전투 시작 시 CombatUnit으로 변환
        public int PvEEnemyCount;

        // ── 이벤트 큐 (View 전달용) ──
        public SimEventQueue EventQueue;

        // ── 설정 ──
        public GameConfig Config;

        // ── 초기화 ──

        /// <summary>GameConfig 기반으로 GameWorld 생성</summary>
        public static GameWorld Create(GameConfig config)
        {
            var world = new GameWorld
            {
                Config = config,
                GameMode = config.GameMode,
                BoardWidth = config.BoardWidth,
                BoardHeight = config.BoardHeight,
                BoardSize = config.BoardWidth * config.BoardHeight,
                TickRate = config.TickRate,
                CurrentStage = 1,
                CurrentRound = 1,
                CurrentPhase = GamePhase.Preparation,
                AlivePlayerCount = config.PlayerCount,
                RNG = new DeterministicRNG(1), // 시드는 외부에서 설정
            };

            // 플레이어 초기화
            world.Players = new PlayerState[MaxPlayers];
            world.Economies = new PlayerEconomy[MaxPlayers];
            world.Boards = new PlayerBoard[MaxPlayers];
            world.BoardSlots = new int[MaxPlayers][];
            world.BenchSlots = new int[MaxPlayers][];
            world.Shops = new ShopSlot[MaxPlayers][];
            world.ShopLocked = new bool[MaxPlayers];

            for (int i = 0; i < MaxPlayers; i++)
            {
                bool isActive = i < config.PlayerCount;
                world.Players[i] = isActive
                    ? PlayerState.CreateDefault(config.StartingHP)
                    : new PlayerState { IsAlive = false, IsEliminated = true };

                world.Economies[i] = isActive
                    ? PlayerEconomy.CreateDefault(config.StartingGold, config.StartingLevel)
                    : default;

                world.Boards[i] = new PlayerBoard();

                world.BoardSlots[i] = new int[world.BoardSize];
                for (int j = 0; j < world.BoardSize; j++)
                    world.BoardSlots[i][j] = UnitData.InvalidId;

                world.BenchSlots[i] = new int[PlayerBoard.BenchSize];
                for (int j = 0; j < PlayerBoard.BenchSize; j++)
                    world.BenchSlots[i][j] = UnitData.InvalidId;

                world.Shops[i] = new ShopSlot[config.ShopSlotCount];
                for (int j = 0; j < config.ShopSlotCount; j++)
                    world.Shops[i][j] = ShopSlot.CreateEmpty();
            }

            // 시너지 초기화
            world.Synergies = new PlayerSynergy[MaxPlayers];
            for (int i = 0; i < MaxPlayers; i++)
                world.Synergies[i] = new PlayerSynergy();

            // 아이템 초기화
            world.Items = new ItemData[MaxItems];
            for (int i = 0; i < MaxItems; i++)
                world.Items[i] = ItemData.CreateEmpty();
            world.ItemInventory = new int[MaxPlayers][];
            world.ItemInventoryCount = new byte[MaxPlayers];
            for (int i = 0; i < MaxPlayers; i++)
            {
                world.ItemInventory[i] = new int[MaxItemInventory];
                for (int j = 0; j < MaxItemInventory; j++)
                    world.ItemInventory[i][j] = ItemData.InvalidId;
            }

            // 유닛 풀 초기화
            world.Units = new UnitData[MaxUnits];
            for (int i = 0; i < MaxUnits; i++)
                world.Units[i] = UnitData.CreateEmpty();
            world.NextEntityId = 0;

            // 전투 매치 초기화
            world.Matches = new CombatMatch[MaxCombatMatches];
            world.CombatMatchStates = new CombatMatchState[MaxCombatMatches];
            for (int i = 0; i < MaxCombatMatches; i++)
                world.Matches[i] = CombatMatch.CreateEmpty();

            // PvE 적 초기화
            world.PvEEnemies = new PvEEnemyData[MaxPvEEnemies];

            // 컷씬 큐 초기화
            world.CutsceneQueue = new CutsceneRequest[MaxCutsceneQueue];

            // 이벤트 큐 초기화
            world.EventQueue = new SimEventQueue();

            return world;
        }

        // ── 유닛 조회 유틸리티 ──

        /// <summary>EntityId로 유닛 인덱스 조회 (-1이면 없음)</summary>
        public int FindUnitIndex(int entityId)
        {
            if (entityId == UnitData.InvalidId) return -1;
            for (int i = 0; i < MaxUnits; i++)
            {
                if (Units[i].EntityId == entityId)
                    return i;
            }
            return -1;
        }

        /// <summary>EntityId로 유닛 참조 가져오기</summary>
        public ref UnitData GetUnit(int entityId)
        {
            int index = FindUnitIndex(entityId);
            return ref Units[index];
        }

        // ── 스펙 데이터 주입 ──

        /// <summary>챔피언 풀 설정 (외부에서 스펙 데이터 주입)</summary>
        public void SetChampionPool(ChampionSpec[] specs, int count)
        {
            Pool = ChampionPool.Create(specs, count, Config.PoolSizeByRarity);
        }

        /// <summary>시너지 스펙 설정</summary>
        public void SetSynergySpecs(SynergySpec[] specs, int count)
        {
            SynergySpecs = specs;
            SynergySpecCount = count;
        }

        /// <summary>아이템 스펙 설정</summary>
        public void SetItemSpecs(ItemSpec[] specs, int count)
        {
            ItemSpecs = specs;
            ItemSpecCount = count;
        }

        /// <summary>빈 유닛 슬롯 할당</summary>
        public int AllocateUnit()
        {
            for (int i = 0; i < MaxUnits; i++)
            {
                if (!Units[i].IsValid)
                    return i;
            }
            return -1; // 풀 가득 참
        }
    }
}
