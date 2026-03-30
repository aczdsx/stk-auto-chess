using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>스킬 구현 타입 (static dispatch key)</summary>
    public enum SkillImplType : byte
    {
        None = 0,
        Generic,
        Rukida,
        April,
        Enki,
        Adria,
    }

    /// <summary>스킬 초기화 파라미터</summary>
    public struct SkillParams
    {
        public int SkillId;
        public int PowerPercent;
        public DamageType DamageType;
        public int CastFrames;
        public SkillTargetType TargetType;

        // CC
        public CrowdControlType CCType;
        public int CCDurationFrames;

        // 버프/디버프
        public StatModType BuffStat;
        public int BuffValue;
        public int BuffDurationFrames;

        // 보조 배율
        public int SecondaryPowerPercent;

        // 멀티타겟 / 다단히트
        public int TargetCount;
        public int HitCount;

        // SKL 클립 타이밍 (프레임 단위, SkillFactory.BuildParams에서 변환)
        /// <summary>SKL 클립 Execute 이벤트 타이밍 (프레임)</summary>
        public int[] SkillHitFrames;
        /// <summary>SKL 클립 전체 길이 (프레임)</summary>
        public int SkillClipFrames;

        /// <summary>GameWorld.TickRate — 모드별 상이하므로 초→프레임 변환 시 사용</summary>
        public int WorldTickRate;

        /// <summary>스킬 쿨타임(초) — specList[0] COOL base_rate</summary>
        public float CooldownSeconds;

        /// <summary>스킬 시전 시 타겟 방향으로 전환할지 여부 (false면 직전 방향 유지)</summary>
        public bool FaceTarget;
    }

    /// <summary>
    /// 스킬 설정 (초기화 후 읽기전용).
    /// 참조 타입(Recipe, ParamValues, SkillHitFrames)은 여기에 격리.
    /// </summary>
    public struct SkillConfig
    {
        // ── Dispatch ──
        public SkillImplType Type;
        public bool IsInitialized;

        // ── 공통 ──
        public int SkillId;
        public int PowerPercent;
        public DamageType DamageType;
        public int CastFrames;
        public SkillTargetType TargetType;
        public CrowdControlType CCType;
        public int CCDurationFrames;
        public StatModType BuffStat;
        public int BuffValue;
        public int BuffDurationFrames;
        public int SecondaryPowerPercent;
        public int TargetCount;
        public int HitCount;
        public bool FaceTarget;
        public SkillExecutionType ExecutionType;
        public bool HasProjectile;
        public int SkillClipFrames;
        public int WorldTickRate;

        // ── 참조 타입 (Config에 격리) ──
        public SkillRecipe Recipe;
        public int[] ParamValues;
        public int[] SkillHitFrames;

        // ── 읽기전용 프로퍼티 ──
        public readonly bool IsChanneling => ExecutionType != SkillExecutionType.Instant;
        public readonly int FirstEffectFrame => SkillHitFrames != null && SkillHitFrames.Length > 0
            ? SkillHitFrames[0] : 0;

        public readonly int GetCastFrames()
        {
            if (ExecutionType == SkillExecutionType.DelayedApply ||
                ExecutionType == SkillExecutionType.Channeling)
                return 0;
            if (CastFrames > 0) return CastFrames;
            if (SkillHitFrames != null && SkillHitFrames.Length > 0) return SkillHitFrames[0];
            return 0;
        }

        public readonly int GetActionLockFrames()
        {
            if (SkillClipFrames > 0) return SkillClipFrames;
            int cf = GetCastFrames();
            if (cf > 0) return cf;
            return FirstEffectFrame > 0 ? FirstEffectFrame : 1;
        }

        public void InitializeBase(SkillParams p)
        {
            SkillId = p.SkillId;
            PowerPercent = p.PowerPercent;
            DamageType = p.DamageType;
            CastFrames = p.CastFrames;
            CCType = p.CCType;
            CCDurationFrames = p.CCDurationFrames;
            BuffStat = p.BuffStat;
            BuffValue = p.BuffValue;
            BuffDurationFrames = p.BuffDurationFrames;
            SecondaryPowerPercent = p.SecondaryPowerPercent;
            TargetType = p.TargetType;
            TargetCount = p.TargetCount <= 0 ? 1 : p.TargetCount;
            HitCount = p.HitCount <= 0 ? 1 : p.HitCount;
            SkillHitFrames = p.SkillHitFrames;
            SkillClipFrames = p.SkillClipFrames;
            FaceTarget = p.FaceTarget;
            WorldTickRate = p.WorldTickRate;
            IsInitialized = true;
        }

        // ── Custom Config (타입별, flat — 총 9개 int로 union 불필요) ──
        // Rukida
        public int FoxFireIncrease;
        public int AtkSpeedRatePercent;
        // April
        public int Rate1, Rate2, Rate3;
        // Enki
        public int HotDuration, HotInterval;
        // Adria
        public int DefScaleValue, StunDurationFrames;
    }

    // ── 타입별 State structs ──

    public struct AprilState
    {
        public int DirCol, DirRow;
        public byte Started;  // bool 대신 byte (Explicit Layout 호환)
        public int ClipEndTimer;
        public int HitIndex;
    }

    public struct EnkiState
    {
        public int PhaseTimer;
        public int ChannelFramesRemaining;
        public byte Fired, Channeling;  // bool 대신 byte
        public int CachedCasterCombatId, CachedAttack;
        public int StartRow, CenterCol, HalfWidth, WaveDirRow;
    }

    public struct AdriaState
    {
        public int CurrentPhase;
        public byte Done;  // bool 대신 byte
        public long HitMask;
    }

    public struct DashState
    {
        public byte DashTilesRemaining;
        public sbyte DashDirCol, DashDirRow;
        public int DashHitDamage;
        public short DashStunFrames;
        public int DashFramesPerTile;
        public byte DashHitFrameIndex;  // 현재 대쉬가 시작된 hitframe 인덱스
    }

    [System.Runtime.InteropServices.StructLayout(
        System.Runtime.InteropServices.LayoutKind.Explicit)]
    public struct SkillCustomState
    {
        [System.Runtime.InteropServices.FieldOffset(0)] public AprilState April;
        [System.Runtime.InteropServices.FieldOffset(0)] public EnkiState Enki;
        [System.Runtime.InteropServices.FieldOffset(0)] public AdriaState Adria;
        [System.Runtime.InteropServices.FieldOffset(0)] public DashState Dash;
    }

    /// <summary>
    /// 스킬 런타임 상태 (매 시전마다 Reset).
    /// 참조 타입 없음 — HitIds는 inline 필드 8개.
    /// </summary>
    public struct SkillState
    {
        // ── 공통 타이머/런타임 ──
        public int StartDelay;
        public int TickTimer;
        public int TickInterval;
        public int RemainingTicks;
        public int TickCount;
        public int CachedTargetId;
        public byte KnockbackHitWall;  // bool → byte
        public int ProjectileArrivalTimer;
        public int CurrentPower;
        public int BounceCount;
        public int DecayPercent;
        public int CurrentHitFrameIndex;
        public int HitFrameTimer;
        public byte HasMultiHitFrames;  // bool → byte
        public int PostCompleteTimer;
        public byte CompleteFired;  // bool → byte
        public int DelayTimer;

        // ── 위치 저장 ──
        public byte SavedGridCol;
        public byte SavedGridRow;

        // ── 히트 추적 (고정 크기, 힙 할당 없음) ──
        public const int MaxHitIds = 8;
        public int HitId0, HitId1, HitId2, HitId3, HitId4, HitId5, HitId6, HitId7;
        public int HitIdCount;

        // ── 타입별 런타임 (union) ──
        public SkillCustomState Custom;

        public void Reset()
        {
            StartDelay = 0; TickTimer = 0; TickInterval = 0;
            RemainingTicks = 0; TickCount = 0;
            CachedTargetId = CombatUnit.InvalidId;
            KnockbackHitWall = 0; ProjectileArrivalTimer = 0;
            CurrentPower = 0; BounceCount = 0; DecayPercent = 0;
            HitIdCount = 0; CurrentHitFrameIndex = 0; HitFrameTimer = 0;
            HasMultiHitFrames = 0; PostCompleteTimer = 0; CompleteFired = 0;
            DelayTimer = -1;
            HitId0 = HitId1 = HitId2 = HitId3 = CombatUnit.InvalidId;
            HitId4 = HitId5 = HitId6 = HitId7 = CombatUnit.InvalidId;
            Custom = default;
        }

        public readonly int GetHitId(int index)
        {
            switch (index)
            {
                case 0: return HitId0; case 1: return HitId1;
                case 2: return HitId2; case 3: return HitId3;
                case 4: return HitId4; case 5: return HitId5;
                case 6: return HitId6; case 7: return HitId7;
                default: return CombatUnit.InvalidId;
            }
        }

        public void SetHitId(int index, int value)
        {
            switch (index)
            {
                case 0: HitId0 = value; break; case 1: HitId1 = value; break;
                case 2: HitId2 = value; break; case 3: HitId3 = value; break;
                case 4: HitId4 = value; break; case 5: HitId5 = value; break;
                case 6: HitId6 = value; break; case 7: HitId7 = value; break;
            }
        }
    }
}
