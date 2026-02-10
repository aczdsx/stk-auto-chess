namespace CookApps.AutoChess
{
    /// <summary>
    /// 게임 모드 설정. 모든 게임 규칙의 수치를 데이터로 정의.
    /// ScriptableObject가 아닌 순수 C# 클래스 (시뮬레이션 레이어용).
    /// </summary>
    public class GameConfig
    {
        // ── 기본 설정 ──
        public GameModeType GameMode;
        public int PlayerCount = 4;
        public int StartingHP = 100;
        public int TickRate = 30;          // 시뮬레이션 프레임/초

        // ── 페이즈 타이머 (초) ──
        public int PreparationDuration = 30;
        public int CombatTimeout = 45;
        public int ResultDuration = 5;
        public int SharedDraftDuration = 20;

        // ── 시스템 활성화 플래그 ──
        public bool EnableShop = true;
        public bool EnableEconomy = true;
        public bool EnableSynergy = true;
        public bool EnableItems = true;
        public bool EnableMatchmaking = true;
        public bool EnableCutscenes = false;

        // ── 경제 설정 ──
        public int StartingGold = 0;
        public int BaseIncome = 5;
        public int MaxInterest = 5;
        public int RerollCost = 2;
        public int XPPerRound = 2;
        public int XPPurchaseCost = 4;     // 골드
        public int XPPurchaseAmount = 4;   // 획득 XP

        // ── 레벨 설정 ──
        public byte StartingLevel = 1;
        public byte MaxLevel = 8;
        // XP 누적 테이블: [레벨] = 해당 레벨 달성에 필요한 총 XP
        public int[] XPTable = { 0, 0, 2, 6, 10, 20, 36, 56, 80 };

        // ── 보드 설정 ──
        public int MaxBenchSlots = PlayerBoard.BenchSize;

        // ── 스테이지 설정 ──
        public int RoundsPerStage = 3;     // 스테이지당 라운드 수

        // ── 합성 설정 ──
        public int DefaultCombineCount = 3; // 기본 합성 필요 수량 (시너지로 2 가능)
        public byte MaxStarLevel = 3;

        // ── 전투 설정 ──
        public int BaseDamageOnLoss = 2;   // 패배 시 기본 데미지
        public int CombatGridWidth = 7;
        public int CombatGridHeight = 8;   // 양쪽 4행씩

        // ── 상점 설정 ──
        public int ShopSlotCount = 5;
        public bool SharedPool = true; // 4인 공유 풀 (PvE는 false)

        // ── GradeType → 코스트 매핑 ──
        // 인덱스: GradeType enum 값 (UNCOMMON=0 ~ MYTHIC=9) → 1~5코스트
        public int[] GradeCostMap = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 };

        // ── 챔피언 풀 사이즈 (레어리티별) ──
        // 인덱스: [0]=미사용, [1]=1코스트, [2]=2코스트, ..., [5]=5코스트
        public int[] PoolSizeByRarity = { 0, 22, 18, 16, 12, 8 };

        // ── 연승/연패 보너스 [streakCount] = bonusGold ──
        // 인덱스 0,1 = 0골드, 2-3 = 1골드, 4-5 = 2골드, 6+ = 3골드
        public int[] StreakBonusTable = { 0, 0, 1, 1, 2, 2, 3 };
        public int MaxStreakBonusIndex = 6; // 6+ 이상은 마지막 값 사용

        // ── 승리 보너스 ──
        public int VictoryBonusGold = 1;

        // ── 레벨별 레어리티 출현 확률 (퍼센트) ──
        // RarityOdds[level][rarity-1] = 확률%, 합계 100
        // level 0은 미사용, level 1-8
        public int[][] RarityOdds =
        {
            null,                              // 0: 미사용
            new[] { 100,  0,  0,  0,  0 },     // Lv1: 1코 100%
            new[] { 100,  0,  0,  0,  0 },     // Lv2: 1코 100%
            new[] {  75, 25,  0,  0,  0 },     // Lv3
            new[] {  55, 30, 15,  0,  0 },     // Lv4
            new[] {  40, 35, 20,  5,  0 },     // Lv5
            new[] {  25, 35, 30, 10,  0 },     // Lv6
            new[] {  19, 30, 35, 15,  1 },     // Lv7
            new[] {  14, 20, 35, 25,  6 },     // Lv8
        };

        // ── 스테이지별 기본 수입 ──
        // BaseIncomeByStage[stage] = gold (0은 미사용)
        public int[] BaseIncomeByStage = { 0, 2, 5, 5, 5, 5, 5, 5, 5, 5, 5 };

        // ── 프리셋 팩토리 ──

        /// <summary>1인 클래식 전투 (보유 캐릭터, 상점/경제 없음)</summary>
        public static GameConfig ClassicBattle()
        {
            return new GameConfig
            {
                GameMode = GameModeType.ClassicBattle,
                PlayerCount = 1,
                EnableShop = false,
                EnableEconomy = false,
                EnableSynergy = false,
                EnableItems = false,
                EnableMatchmaking = false,
                EnableCutscenes = true,
                PreparationDuration = 0, // 준비 없음
                StartingLevel = 5,
                StartingHP = 100,
            };
        }

        /// <summary>1인 PvE 캠페인 (상점/경제 있음)</summary>
        public static GameConfig PvECampaign()
        {
            return new GameConfig
            {
                GameMode = GameModeType.PvECampaign,
                PlayerCount = 1,
                EnableShop = true,
                EnableEconomy = true,
                EnableSynergy = true,
                EnableItems = true,
                EnableMatchmaking = false,
                EnableCutscenes = true,
                StartingGold = 5,
                StartingHP = 100,
            };
        }

        /// <summary>4인 경쟁 멀티플레이 (풀 시스템)</summary>
        public static GameConfig Competitive()
        {
            return new GameConfig
            {
                GameMode = GameModeType.Competitive,
                PlayerCount = 4,
                EnableShop = true,
                EnableEconomy = true,
                EnableSynergy = true,
                EnableItems = true,
                EnableMatchmaking = true,
                EnableCutscenes = false, // 멀티에서는 컷씬 비활성
                StartingGold = 0,
                StartingHP = 100,
            };
        }
    }
}
