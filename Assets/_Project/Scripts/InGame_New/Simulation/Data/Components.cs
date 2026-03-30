using System.Collections.Generic;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 유닛 데이터. 보드 또는 벤치에 존재하는 유닛의 전체 정보.
    /// EntityId == -1이면 빈 슬롯.
    /// </summary>
    public struct UnitData
    {
        public const int InvalidId = -1;
        public const int MaxItemSlots = 3;

        public int EntityId;
        public int ChampionSpecId;
        public byte StarLevel;        // 1, 2, 3
        public UnitLocation Location;
        public byte BoardCol;
        public byte BoardRow;
        public byte BenchIndex;
        public byte OwnerIndex;       // 소유 플레이어 (0-3)
        public byte SizeW;            // 가로 타일 수 (기본 1)
        public byte SizeH;            // 세로 타일 수 (기본 1)

        // 기본 스탯 (스펙에서 복사 + 별 보정 적용된 값)
        public int MaxHP;
        public int Attack;
        public int Def;               // DEF (최종 데미지 감산)
        public int AttackSpeed;       // 100 = 1.0 공속 (정수 기반)
        public int AttackRange;       // 1 = 근접, 2+ = 원거리
        public int MoveSpeed;         // 100 = 1.0 이속
        public int MaxMana;

        // 관통/크리 (정수 퍼센트)
        public int AtkPierce;         // 물리 관통 (0-100)
        public int ResPierce;         // 마법 관통 (0-100)
        public int CritRate;          // 크리 확률 (0-100)
        public int CritPower;         // 크리 배율 (150 = 1.5x)

        // 저항 스탯
        public int AdReduce;          // 물리 저항률 (정수 퍼센트)
        public int ApReduce;          // 마법 저항률 (정수 퍼센트)
        public int HealPower;         // 힐파워 (정수 퍼센트)
        public int ImmuneType;        // 이뮨 타입

        // 특성 (시너지용)
        public int TraitFlags;        // 비트마스크 (최대 32개 특성)

        // 아이템 슬롯 (최대 3개, -1 = 비어있음)
        public int ItemSlot0;
        public int ItemSlot1;
        public int ItemSlot2;

        public bool IsValid => EntityId != InvalidId;

        public int GetItemSlot(int index)
        {
            return index switch
            {
                0 => ItemSlot0,
                1 => ItemSlot1,
                2 => ItemSlot2,
                _ => InvalidId,
            };
        }

        public void SetItemSlot(int index, int itemId)
        {
            switch (index)
            {
                case 0: ItemSlot0 = itemId; break;
                case 1: ItemSlot1 = itemId; break;
                case 2: ItemSlot2 = itemId; break;
            }
        }

        public int GetEmptyItemSlot()
        {
            if (ItemSlot0 == InvalidId) return 0;
            if (ItemSlot1 == InvalidId) return 1;
            if (ItemSlot2 == InvalidId) return 2;
            return InvalidId;
        }

        public static UnitData CreateEmpty()
        {
            return new UnitData
            {
                EntityId = InvalidId,
                ItemSlot0 = InvalidId,
                ItemSlot1 = InvalidId,
                ItemSlot2 = InvalidId,
            };
        }
    }

    /// <summary>
    /// 플레이어 보드. 7×4 전투 그리드 + 9칸 벤치.
    /// 각 슬롯에는 UnitData의 EntityId를 저장 (-1 = 비어있음).
    /// </summary>
    public struct PlayerBoard
    {
        public const int BoardWidth = 7;
        public const int BoardHeight = 4;
        public const int BoardSize = 28;  // 7 × 4
        public const int BenchSize = 9;

        // 보드 슬롯 [col + row * BoardWidth] = EntityId
        // 고정 크기 배열 대신 개별 접근은 GameWorld의 int[] 배열 사용
        // (이 구조체는 상수 및 메타데이터만 보유)

        public byte UnitCount;  // 현재 보드 위 유닛 수
        public byte BenchCount; // 현재 벤치 위 유닛 수
    }

    /// <summary>플레이어 경제 상태</summary>
    public struct PlayerEconomy
    {
        public int Gold;
        public int XP;
        public byte Level;         // 1-8
        public int WinStreak;      // 연승 (양수), 0이면 없음
        public int LoseStreak;     // 연패 (양수), 0이면 없음
        public int TotalWins;
        public int TotalLosses;

        public static PlayerEconomy CreateDefault(int startingGold, byte startingLevel)
        {
            return new PlayerEconomy
            {
                Gold = startingGold,
                XP = 0,
                Level = startingLevel,
            };
        }
    }

    /// <summary>상점 슬롯 1개</summary>
    public struct ShopSlot
    {
        public int ChampionSpecId;  // 0 = 비어있음 / 구매됨
        public byte Cost;
        public bool IsPurchased;

        public bool IsAvailable => ChampionSpecId != 0 && !IsPurchased;

        public static ShopSlot CreateEmpty()
        {
            return new ShopSlot { ChampionSpecId = 0 };
        }
    }

    /// <summary>플레이어 상태 (HP, 생존, 순위)</summary>
    public struct PlayerState
    {
        public int HP;
        public int MaxHP;
        public bool IsAlive;
        public bool IsEliminated;
        public byte Rank;          // 탈락 시 순위 (1 = 우승)
        public bool IsReady;       // 준비 완료 플래그
        public bool IsConnected;   // 접속 상태

        public static PlayerState CreateDefault(int maxHP)
        {
            return new PlayerState
            {
                HP = maxHP,
                MaxHP = maxHP,
                IsAlive = true,
                IsEliminated = false,
                Rank = 0,
                IsReady = false,
                IsConnected = true,
            };
        }
    }

    /// <summary>전투 매치 정보 (2명의 1v1)</summary>
    public struct CombatMatch
    {
        public const int MaxUnitsPerMatch = 32; // 양쪽 합쳐서 최대

        public byte PlayerA;
        public byte PlayerB;
        public bool IsGhostMatch;  // 유령 매치 (3인 시)
        public bool IsFinished;
        public byte Winner;        // 0xFF = 무승부

        public static CombatMatch CreateEmpty()
        {
            return new CombatMatch { Winner = 0xFF };
        }
    }

    /// <summary>컷씬 요청</summary>
    public struct CutsceneRequest
    {
        public int SourceEntityId;   // 스킬 시전 유닛
        public int SkillSpecId;      // 스킬 스펙
        public int TargetEntityId;   // 대상 유닛
        public int DurationFrames;   // 컷씬 길이 (프레임)
        public bool IsActive;
    }

    // ═══════════════════════════════════════════════
    //  챔피언 풀 / 상점 시스템
    // ═══════════════════════════════════════════════

    /// <summary>
    /// 챔피언 스펙 정보 (시뮬레이션용 경량 데이터).
    /// 실제 에셋은 View 레이어에서 관리. 시뮬레이션은 이 구조체만 사용.
    /// </summary>
    public struct ChampionSpec
    {
        public int ChampionId;
        public byte Cost;            // 1-5
        public byte Rarity;          // 1-5 (Cost와 동일)
        public int TraitFlags;       // 비트마스크 (특성/시너지)

        // 기본 스탯
        public int BaseHP;
        public int BaseAttack;
        public int BaseDef;
        public int BaseApReduce;
        public int AttackSpeed;      // 100 = 1.0
        public int AttackRange;
        public int MoveSpeed;        // 100 = 1.0
        public int MaxMana;
        public int StartingMana;
        public int SkillId;           // 기본 스킬 ID
        public int PrefabId;          // 프리팹 ID (AnimKeyframeData 조회용)

        // 관통/크리 기본값
        public int BaseAtkPierce;     // 물리 관통 (퍼센트, 0-100)
        public int BaseResPierce;     // 마법 관통 (퍼센트, 0-100)
        public int BaseCritRate;      // 크리 확률 (퍼센트, 0-100)
        public int BaseCritPower;     // 크리 배율 (퍼센트, 150 = 1.5x)

        // 추가 스탯
        public int BaseAdReduce;      // 물리 저항률 (정수 퍼센트)
        public int BaseHealPower;     // 힐파워 (정수 퍼센트)
        public int BaseImmuneType;    // 이뮨 타입
        public byte PositionType;    // CharacterPositionType (직업군 패시브 결정용)

        // 유닛 크기 (타일 수)
        public byte SizeW;           // 가로 (기본 1)
        public byte SizeH;           // 세로 (기본 1)

        // 별 업그레이드 배율 (퍼센트: 180 = 1.8x)
        public int Star2Multiplier;  // 기본 180
        public int Star3Multiplier;  // 기본 320

        public bool IsValid => ChampionId > 0;
    }

    /// <summary>
    /// 공유 챔피언 풀. 4인이 공유하는 챔피언 재고.
    /// ChampionSpecId → 남은 수량을 관리.
    /// </summary>
    public class ChampionPool
    {
        public const int MaxChampionTypes = 64;

        // 등록된 챔피언 스펙 (인덱스 = 챔피언 종류)
        public ChampionSpec[] Specs;
        public int SpecCount;

        // 남은 재고: Stock[specIndex] = 남은 수량
        public int[] Stock;

        // 레어리티별 챔피언 인덱스 목록 (빠른 조회)
        // RarityIndices[rarity-1] = 해당 레어리티의 specIndex 배열
        public int[][] RarityIndices;
        public int[] RarityIndexCounts;

        public static ChampionPool Create(ChampionSpec[] specs, int specCount, int[] poolSizeByRarity)
        {
            var pool = new ChampionPool
            {
                Specs = new ChampionSpec[MaxChampionTypes],
                Stock = new int[MaxChampionTypes],
                SpecCount = specCount,
                RarityIndices = new int[5][],
                RarityIndexCounts = new int[5],
            };

            // 레어리티별 인덱스 카운트
            var rarityCounts = new int[5];
            for (int i = 0; i < specCount; i++)
            {
                pool.Specs[i] = specs[i];
                int rarity = specs[i].Rarity;
                if (rarity >= 1 && rarity <= 5)
                {
                    pool.Stock[i] = poolSizeByRarity[rarity];
                    rarityCounts[rarity - 1]++;
                }
            }

            // 레어리티별 인덱스 배열 구성
            for (int r = 0; r < 5; r++)
            {
                pool.RarityIndices[r] = new int[rarityCounts[r]];
            }

            var fillIdx = new int[5];
            for (int i = 0; i < specCount; i++)
            {
                int rarity = specs[i].Rarity;
                if (rarity >= 1 && rarity <= 5)
                {
                    int r = rarity - 1;
                    pool.RarityIndices[r][fillIdx[r]] = i;
                    pool.RarityIndexCounts[r] = fillIdx[r] + 1;
                    fillIdx[r]++;
                }
            }

            return pool;
        }
    }

    // ═══════════════════════════════════════════════
    //  시너지 시스템
    // ═══════════════════════════════════════════════

    /// <summary>시너지 효과 1개</summary>
    public struct SynergyEffect
    {
        public SynergyEffectType Type;
        public SynergyTarget Target;   // TraitUnits / AllAllies / AllEnemies
        public int Value;              // 고정값 보너스
        public int ValuePercent;       // 퍼센트 보너스 (100 = 100%)
    }

    /// <summary>시너지 단계 (임계치 + 효과 목록)</summary>
    public struct SynergyTier
    {
        public byte RequiredCount;     // 필요 유닛 수
        public SynergyEffect[] Effects;
    }

    /// <summary>시너지 스펙 (특성 1종의 전체 정보)</summary>
    public struct SynergySpec
    {
        public int TraitId;            // 비트 인덱스 (0-31)
        public TraitCategory Category;
        public SynergyTier[] Tiers;    // 단계별 효과
        public bool HasBehavior;       // asterism처럼 행동 클래스가 필요한 시너지

        public bool IsValid => Tiers != null && Tiers.Length > 0;
    }

    /// <summary>
    /// 플레이어 시너지 상태. 보드 변경 시마다 재계산.
    /// 플레이어당 1개만 존재하며 GameWorld.Create()에서 1회 할당.
    /// </summary>
    public class PlayerSynergy
    {
        public const int MaxTraits = 32;

        public readonly byte[] TraitCounts = new byte[MaxTraits];
        public readonly byte[] TraitTiers = new byte[MaxTraits];
        public byte ActiveSynergyCount;

        public byte GetTraitCount(int index) => (uint)index < MaxTraits ? TraitCounts[index] : (byte)0;
        public void SetTraitCount(int index, byte value) { if ((uint)index < MaxTraits) TraitCounts[index] = value; }
        public byte GetTraitTier(int index) => (uint)index < MaxTraits ? TraitTiers[index] : (byte)0;
        public void SetTraitTier(int index, byte value) { if ((uint)index < MaxTraits) TraitTiers[index] = value; }

        public void Clear()
        {
            System.Array.Clear(TraitCounts, 0, MaxTraits);
            System.Array.Clear(TraitTiers, 0, MaxTraits);
            ActiveSynergyCount = 0;
        }
    }

    // ═══════════════════════════════════════════════
    //  아이템 시스템
    // ═══════════════════════════════════════════════

    /// <summary>아이템 스펙 (시뮬레이션용 경량 데이터)</summary>
    public struct ItemSpec
    {
        public int ItemId;
        public bool IsBaseItem;        // true=기본, false=합성
        public int RecipeItem1;        // 조합 재료 1
        public int RecipeItem2;        // 조합 재료 2
        public bool IsUnique;          // 합성 아이템은 유닛당 1개

        // 스탯 보너스
        public int BonusAttack;
        public int BonusAttackSpeedPercent;  // 퍼센트
        public int BonusSpellPowerPercent;   // 퍼센트
        public int BonusMana;
        public int BonusDef;
        public int BonusAdReduce;
        public int BonusApReduce;
        public int BonusHP;
        public int BonusCritChance;          // 퍼센트

        // 특수 효과
        public ItemEffectType SpecialEffect;
        public int EffectValue1;       // 효과 수치 1
        public int EffectValue2;       // 효과 수치 2

        public bool IsValid => ItemId > 0;
    }

    /// <summary>아이템 인스턴스 (인벤토리/장착 상태)</summary>
    public struct ItemData
    {
        public const int InvalidId = -1;

        public int ItemInstanceId;     // 인스턴스 고유 ID
        public int ItemSpecId;         // 아이템 스펙 ID
        public byte OwnerIndex;        // 소유 플레이어 (0-3)
        public ItemLocation Location;
        public int EquippedEntityId;   // 장착된 유닛 EntityId (-1 = 미장착)
        public byte SlotIndex;         // 장착 슬롯 (0-2)

        public bool IsValid => ItemInstanceId != InvalidId;

        public static ItemData CreateEmpty()
        {
            return new ItemData
            {
                ItemInstanceId = InvalidId,
                EquippedEntityId = InvalidId,
            };
        }
    }

    /// <summary>전투 유닛의 아이템 특수 효과 상태</summary>
    public struct CombatItemEffects
    {
        public int LifeStealPercent;
        public int SpellVampPercent;
        public int ReflectDamagePercent;
        public int OnHitMagicDamage;
        public int BurnDamagePerTick;
        public int AntiHealPercent;
        public int DodgeChanceBonus;
        public int CleavePercent;
        public int ExtraAttackInterval;    // N회마다 추가 타격
        public int AttackCounter;
        public bool HasCCImmunity;
        public bool CCImmunityUsed;
        public int ShieldThresholdPercent; // HP% 이하 시 보호막
        public int ShieldBonusAmount;
        public bool ShieldTriggered;
    }

    // ═══════════════════════════════════════════════
    //  범위 기본공격 시스템
    // ═══════════════════════════════════════════════

    public enum AreaAttackShape : byte
    {
        Single = 0,   // 단일 타겟
        Cross,        // 수직 방향 범위 (facing 수직으로 ±Size칸)
        Line,         // 직선 범위 (facing 방향 Size칸)
        Radius,       // 원형 범위 (체비셰프 Size반경)
    }

    public struct AreaAttackHit
    {
        public AreaAttackShape Shape;
        public int Size;          // Cross: 좌우폭, Line: 길이, Radius: 반경
        public int FrontOffset;   // facing 방향 오프셋 (0=시전자 위치)
        public int DelayMs;       // 공격 시작 시점부터의 딜레이 (밀리초, 애니메이션 키프레임 기준)
    }

    public struct AreaAttackPattern
    {
        public byte HitCount;     // 1~4
        public AreaAttackHit Hit0, Hit1, Hit2, Hit3;

        public AreaAttackHit GetHit(int i) => i switch
        {
            0 => Hit0,
            1 => Hit1,
            2 => Hit2,
            3 => Hit3,
            _ => Hit0,
        };
    }

    // ═══════════════════════════════════════════════
    //  전투 시스템 구조체
    // ═══════════════════════════════════════════════

    /// <summary>
    /// 전투 유닛. 보드 UnitData를 복제하여 전투 전용 상태를 추가.
    /// 전투 종료 후 폐기 (원본 UnitData에 영향 없음).
    /// </summary>
    public struct CombatUnit
    {
        public const int InvalidId = -1;

        public int CombatId;          // 전투 내 고유 ID
        public int SourceEntityId;    // 원본 UnitData EntityId
        public int ChampionSpecId;
        public byte StarLevel;
        public byte OwnerIndex;       // 소유 플레이어 (0-3)
        public byte TeamIndex;        // 매치 내 팀 (0 = PlayerA, 1 = PlayerB)
        public int TraitFlags;        // 시너지 특성 비트마스크 (원본에서 복사)

        // 그리드 위치 (앵커 = 좌하단)
        public byte GridCol;
        public byte GridRow;
        public byte SizeW;            // 가로 타일 수 (기본 1)
        public byte SizeH;            // 세로 타일 수 (기본 1)

        // 상태
        public CombatState State;
        public bool IsAlive;

        // 기본 스탯 (CombatSetup 시 복사, 퍼센트 버프의 기준값)
        public int BaseMaxHP;
        public int BaseAttack;
        public int BaseDef;
        public int BaseAttackSpeed;
        public int BaseAdReduce;
        public int BaseApReduce;

        // 전투 스탯 (시너지/아이템 보정 적용 후)
        public int MaxHP;
        public int CurrentHP;
        public int Attack;
        public int Def;               // DEF (최종 데미지 감산)
        public int AttackSpeed;       // 100 = 1.0
        public int AttackRange;
        public int MoveSpeed;         // 100 = 1.0
        public int MaxMana;
        public int CurrentMana;
        // ── 마나 리젠 ──
        public int ManaRegenPerSec;    // 초당 시간 리젠량
        public int ManaRegenAccum;     // 매 프레임 누적 카운터 (tickRate 도달 시 1 충전)
        public int ManaGainOnAttack;   // 타격 시 마나 획득량
        public int ManaGainOnHit;      // 피격 시 마나 획득량
        public int ManaRegenRateBonus; // 마나 리젠 속도 보너스 % (버프/디버프 누적)
        public int CritRate;          // 퍼센트 (0-100)
        public int CritPower;         // 퍼센트 (150 = 1.5x)
        public int HitChance;        // 명중률 (퍼센트, 기본 100, 최대 100)

        // 관통 (퍼센트, 0-100)
        public int AtkPierce;        // 물리 관통
        public int ResPierce;        // 마법 관통

        // 저항 스탯
        public int AdReduce;          // 물리 저항률 (정수 퍼센트)
        public int ApReduce;          // 마법 저항률 (정수 퍼센트)
        public int HealPower;         // 힐파워 (정수 퍼센트)
        public int ImmuneType;        // 이뮨 타입

        // 특수 스탯 (시너지/아이템 효과)
        public int LifeSteal;         // 퍼센트
        public int DodgeChance;       // 퍼센트
        public int ShieldAmount;      // 보호막

        // 타겟팅
        public int CurrentTargetId;   // 현재 타겟의 CombatId (-1 = 없음)

        // 쿨다운 (프레임 단위)
        public int AttackCooldown;    // 다음 공격까지 남은 프레임
        public int AtkHitDelay;       // ATK Execute 키프레임까지 프레임 수 (근접 데미지 지연)
        public int AttackActionFrames; // 공격 시작 후 상태를 유지할 전체 모션 프레임

        // 대기 중인 근접 공격
        public int PendingAtkTargetId;   // 대기 중인 공격 타겟 (-1 = 없음)
        public int PendingAtkTimer;      // 히트까지 남은 프레임
        public bool PendingAtkIsCrit;    // 선행 판정된 크리티컬 여부 (ATK 시작 시 확정)

        // 공격/스킬 액션 락 (모션 종료 전 상태 전환 방지)
        public int ActionLockTimer;

        // 이동 (프레임 단위)
        public byte MoveFromCol;      // 이동 출발 열 (View 보간용)
        public byte MoveFromRow;      // 이동 출발 행 (View 보간용)
        public int MoveTimer;         // 이동 중 남은 프레임 (0이면 이동 중 아님)
        public int MoveDuration;      // 이동 총 프레임 (View 보간 비율 계산용)

        // 특수 이동
        public bool HasBacklineJump;
        public bool BacklineJumpDone;
        public bool IsBacklineJumping;    // 백라인 점프 이동 중 (타겟 불가)
        public bool IsKnockbackMoving;   // 넉백 이동 중 (View에서 OutExpo 이징 적용)

        // 대쉬 (빅마우스 등 — DashSystem 관리, 런타임 상태는 SkillState.Custom.Dash)
        public MoveEaseType DashEase;       // 현재 페이즈 Ease (None이면 대쉬 아님)
        public DashPhase DashPhase;         // 현재 대쉬 페이즈 (None이면 대쉬 아님)

        // CC 상태
        public CrowdControlType ActiveCC;
        public int CCRemainingFrames;
        public byte CCImmuneCharges;    // 직업 패시브: CC 면역 횟수 (Striker)
        public bool IsHealer;              // 직업 패시브: Oracle 평타 힐러 (타겟팅/AI 분기용)
        public byte ProjectileVfxOverride; // 투사체 VFX 오버라이드 (0=기본, 1+=View 프리팹 인덱스, 1회 소비)

        // 스킬
        public int SkillSpecId;       // 사용 스킬 ID
        public int SkillCastTimer;    // 시전 중 남은 프레임
        public bool IsSkillReady;     // 마나 충전 완료
        public bool HasPushedManaFull; // ManaFull 이벤트 발행 여부

        // 범위 기본공격
        public bool HasAreaAttack;        // AreaAttackRegistry에 패턴 있으면 true
        public bool IsAreaAttacking;      // 범위 공격 진행 중
        public byte AreaHitIndex;         // 다음 처리할 히트 인덱스
        public int AreaHitTimer;          // 다음 히트까지 남은 프레임
        public int AreaHitDamage;         // 히트당 데미지 (미리 계산)
        public bool AreaHitIsCrit;        // 크리 여부
        public sbyte AreaDirCol;          // facing 방향
        public sbyte AreaDirRow;

        public bool IsUntargetable;  // StatusEffectType.TargetImpossible 캐시

        public bool IsValidTarget => IsAlive && State != CombatState.Dead;

        /// <summary>타겟 선택 가능 여부 (백라인 점프 중, 지정불가 제외)</summary>
        public bool IsTargetable => IsValidTarget && !IsBacklineJumping && !IsUntargetable;

        /// <summary>공격 쿨다운 프레임 수 계산 (AttackSpeed 기반)</summary>
        public int GetAttackInterval(int tickRate)
        {
            if (AttackSpeed <= 0) return tickRate; // 1초
            // AttackSpeed 100 = 1.0 공속 = tickRate 프레임마다 공격
            return tickRate * 100 / AttackSpeed;
        }

        /// <summary>1칸 이동에 걸리는 프레임 수 (MoveSpeed 기반)</summary>
        public int GetMoveFrames(int tickRate)
        {
            if (MoveSpeed <= 0) return tickRate;
            return tickRate * 100 / MoveSpeed;
        }

        /// <summary>이동 중인지 여부</summary>
        public bool IsMoving => MoveTimer > 0;
    }

    /// <summary>
    /// PvE 적 유닛 데이터. 외부에서 스펙+배율을 계산하여 주입.
    /// 전투 시작 시 CombatUnit으로 변환 (보드를 거치지 않음).
    /// </summary>
    public struct PvEEnemyData
    {
        public int ChampionSpecId;
        public int PrefabId;           // 프리팹 ID (AnimKeyframeData 조회용)
        public byte GridCol;       // 보드 좌표 (미러링 전)
        public byte GridRow;
        public byte SizeW;
        public byte SizeH;

        // 계산 완료된 스탯
        public int MaxHP;
        public int Attack;
        public int Def;               // DEF (최종 데미지 감산)
        public int AttackSpeed;    // 100 = 1.0
        public int AttackRange;
        public int MoveSpeed;      // 100 = 1.0
        public int MaxMana;
        public int TraitFlags;
        public int SkillSpecId;

        // 관통/크리
        public int AtkPierce;         // 물리 관통 (0-100)
        public int ResPierce;         // 마법 관통 (0-100)
        public int CritRate;          // 크리 확률 (0-100)
        public int CritPower;         // 크리 배율 (150 = 1.5x)

        // 저항 스탯
        public int AdReduce;          // 물리 저항률 (정수 퍼센트)
        public int ApReduce;          // 마법 저항률 (정수 퍼센트)
        public int HealPower;         // 힐파워 (정수 퍼센트)
        public int ImmuneType;        // 이뮨 타입
    }

    /// <summary>
    /// 전투 그리드. 7×8 (양쪽 4행씩). 각 타일에 CombatUnit의 CombatId 저장.
    /// </summary>
    public struct CombatGrid
    {
        public const int Width = 7;
        public const int Height = 8;
        public const int Size = 56; // 7 × 8

        // Tiles[col + row * Width] = CombatId (-1 = 비어있음)
        // 실제 배열은 CombatMatchState에서 관리
    }

    /// <summary>
    /// 상태효과 엔트리. 쉴드/DOT/버프 등 지속시간 기반 효과를 통합 관리.
    /// CombatMatchState.StatusEffects[] 배열에 저장.
    /// </summary>
    public struct StatusEffect
    {
        public int OwnerUnitIndex;       // 대상 유닛의 Units[] 인덱스
        public StatusEffectType Type;
        public int Value;                // 쉴드량 / 틱당 데미지 / 스탯 변경량
        public int RemainingFrames;      // 남은 지속 프레임 (-1 = 영구)
        public int TickInterval;         // 주기적 효과 간격 (프레임, 0 = 비주기적)
        public int TickTimer;            // 다음 틱까지 남은 프레임
        public StatModType StatType;     // StatBuff/Debuff 시 대상 스탯
        public DamageType DmgType;       // DOT 데미지 타입
        public bool IsActive;
        public int SourceSkillId;        // 동일 스킬의 효과 갱신용 (0 = 미지정)
    }

    /// <summary>투사체 데이터</summary>
    public struct Projectile
    {
        public int ProjectileId;
        public int SourceCombatId;      // 발사한 유닛
        public int TargetCombatId;      // 대상 유닛 (Homing용)
        public ProjectileType Type;
        public DamageType DamageType;
        public int Damage;
        public bool IsCrit;
        public int SkillSpecId;         // 0이면 기본공격 투사체

        // Homing / AreaTarget 공통
        public int RemainingFrames;     // 도착까지 남은 프레임

        // Linear 전용
        public byte CurrentCol;
        public byte CurrentRow;
        public sbyte DirCol;
        public sbyte DirRow;
        public int MoveInterval;        // 몇 프레임마다 1칸 이동
        public int MoveTimer;
        public int MaxDistance;
        public int TraveledDistance;
        public long HitMask;            // 이미 맞은 유닛 비트마스크 (중복 피격 방지)
        public int Width;               // 투사체 폭 (0 또는 1 = 1칸, 3 = 3칸). 진행 방향 수직으로 확장.
        public sbyte SkillVfxIndex;     // -1 = 기본 프리팹, 0+ = skillPrefabs[index] 사용 (뷰 전용)
        public ProjectileHitBehavior HitBehavior; // 유닛에 닿았을 때 행동 (None=VFX전용, DamageEnemy=적데미지, HealAlly=아군힐)

        // HealAlly 전용
        public int HotPerTick;           // HoT 틱당 힐량 (0이면 HoT 없음)
        public int HotDuration;          // HoT 지속 프레임
        public int HotInterval;          // HoT 틱 간격
        public byte AreaEffectHalfWidth; // >0이면 이동 시 PushSkillAreaEffect 발행

        // AreaTarget 전용
        public byte TargetCol;
        public byte TargetRow;
        public int AreaRadius;

        public bool IsActive;
    }

    /// <summary>
    /// 전투 매치의 전체 상태 (1v1 하나의 전투 필드).
    /// GameWorld.CombatMatchStates[]에 저장.
    /// </summary>
    public class CombatMatchState
    {
        public const int MaxCombatUnits = 32;
        public const int MaxProjectiles = 32;
        public const int MaxStatusEffects = 128;

        // 매치 메타
        public byte MatchIndex;        // 0 또는 1
        public byte PlayerA;
        public byte PlayerB;
        public bool IsFinished;
        public bool IgnoreEndCondition; // idle 전투 등 종료 판정 스킵
        public byte Winner;            // 0=A승, 1=B승, 0xFF=무승부

        // 유닛
        public CombatUnit[] Units;     // [MaxCombatUnits]
        public int UnitCount;
        public int NextCombatId;
        public Dictionary<int, int> CombatIdToUnitIndex;

        // 그리드 (크기는 BoardHelper.CombatWidth/CombatHeight 참조)
        public int[] GridTiles;

        // 투사체
        public Projectile[] Projectiles; // [MaxProjectiles]
        public int ProjectileCount;
        public int NextProjectileId;

        // 상태효과 (쉴드/DOT/버프 통합)
        public StatusEffect[] StatusEffects; // [MaxStatusEffects]
        public int StatusEffectCount;

        // 팀별 생존 수
        public int AliveCountA;
        public int AliveCountB;

        // 이벤트 큐 (GameWorld.EventQueue 참조)
        public SimEventQueue EventQueue;

        // RNG (Trait에서 접근용 — CombatAISystem.Tick 시작 시 저장)
        public DeterministicRNG Rng;

        // 스킬 (매치별 관리, SkillSystem에서 사용)
        public SkillConfig[] SkillConfigs;    // [MaxCombatUnits] 초기화 후 읽기전용
        public SkillState[] SkillStates;      // [MaxCombatUnits] 매 시전마다 Reset

        // 특성 인스턴스 (유닛별 최대 MaxTraitsPerUnit개)
        public CombatTraitBase[][] Traits; // [MaxCombatUnits][CombatTraitBase.MaxTraitsPerUnit]
        public int[] TraitCounts;          // [MaxCombatUnits] 유닛별 부착된 특성 수
        internal bool _traitCombatStartDone; // OnCombatStart 1회 실행 플래그

        public static CombatMatchState Create(byte matchIndex, byte playerA, byte playerB)
        {
            int gridSize = BoardHelper.CombatWidth * BoardHelper.CombatHeight;
            var state = new CombatMatchState
            {
                MatchIndex = matchIndex,
                PlayerA = playerA,
                PlayerB = playerB,
                Winner = 0xFF,
                Units = new CombatUnit[MaxCombatUnits],
                CombatIdToUnitIndex = new Dictionary<int, int>(MaxCombatUnits),
                GridTiles = new int[gridSize],
                Projectiles = new Projectile[MaxProjectiles],
                StatusEffects = new StatusEffect[MaxStatusEffects],
                SkillConfigs = new SkillConfig[MaxCombatUnits],
                SkillStates = new SkillState[MaxCombatUnits],
                Traits = new CombatTraitBase[MaxCombatUnits][],
                TraitCounts = new int[MaxCombatUnits],
            };

            for (int i = 0; i < MaxCombatUnits; i++)
                state.Traits[i] = new CombatTraitBase[CombatTraitBase.MaxTraitsPerUnit];

            for (int i = 0; i < MaxCombatUnits; i++)
                state.Units[i].CombatId = CombatUnit.InvalidId;
            for (int i = 0; i < gridSize; i++)
                state.GridTiles[i] = CombatUnit.InvalidId;
            for (int i = 0; i < MaxProjectiles; i++)
                state.Projectiles[i].IsActive = false;
            state.NextCombatId = 100000; // EntityId(0~)와 겹치지 않도록 오프셋 (혼용 방지)
            state.NextProjectileId = 1; // 0은 뷰 레이어에서 무효 ID로 사용되므로 1부터 할당

            return state;
        }

        /// <summary>CombatId로 유닛 인덱스 조회</summary>
        public int FindUnitIndex(int combatId)
        {
            if (combatId == CombatUnit.InvalidId) return -1;

            if (CombatIdToUnitIndex != null && CombatIdToUnitIndex.TryGetValue(combatId, out int unitIndex))
            {
                if (unitIndex >= 0 && unitIndex < UnitCount && Units[unitIndex].CombatId == combatId)
                    return unitIndex;
            }

            return -1;
        }

        /// <summary>그리드 위치의 유닛 CombatId 조회</summary>
        public int GetUnitAtGrid(int col, int row)
        {
            if (col < 0 || col >= BoardHelper.CombatWidth || row < 0 || row >= BoardHelper.CombatHeight)
                return CombatUnit.InvalidId;
            return GridTiles[col + row * BoardHelper.CombatWidth];
        }

        /// <summary>그리드에 유닛 배치</summary>
        public void SetGrid(int col, int row, int combatId)
        {
            GridTiles[col + row * BoardHelper.CombatWidth] = combatId;
        }

        /// <summary>그리드에서 유닛 제거</summary>
        public void ClearGrid(int col, int row)
        {
            GridTiles[col + row * BoardHelper.CombatWidth] = CombatUnit.InvalidId;
        }

        // ── Multi-Tile 헬퍼 ──

        /// <summary>유닛의 전체 풋프린트 그리드 등록</summary>
        public void SetGridMulti(int anchorCol, int anchorRow, byte sizeW, byte sizeH, int combatId)
        {
            for (int dc = 0; dc < sizeW; dc++)
                for (int dr = 0; dr < sizeH; dr++)
                    SetGrid(anchorCol + dc, anchorRow + dr, combatId);
        }

        /// <summary>유닛의 전체 풋프린트 그리드 해제</summary>
        public void ClearGridMulti(int anchorCol, int anchorRow, byte sizeW, byte sizeH)
        {
            for (int dc = 0; dc < sizeW; dc++)
                for (int dr = 0; dr < sizeH; dr++)
                    ClearGrid(anchorCol + dc, anchorRow + dr);
        }

        /// <summary>풋프린트 영역이 모두 비어있는지 (자기 자신 제외)</summary>
        public bool IsFootprintClear(int anchorCol, int anchorRow, byte sizeW, byte sizeH, int selfCombatId)
        {
            for (int dc = 0; dc < sizeW; dc++)
                for (int dr = 0; dr < sizeH; dr++)
                {
                    int c = anchorCol + dc, r = anchorRow + dr;
                    if (!BoardHelper.IsValidCombatPosition(c, r)) return false;
                    int occupant = GetUnitAtGrid(c, r);
                    if (occupant != CombatUnit.InvalidId && occupant != selfCombatId) return false;
                }
            return true;
        }
    }
}
