using System.Collections.Generic;
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
    /// 스킬 인스턴스 (값 타입). 모든 스킬의 공통 상태 + Custom union 필드를 flat으로 보유.
    /// 행위는 SkillDispatcher에서 SkillType 기반 static dispatch.
    /// </summary>
    public struct SimSkillInstance
    {
        // ── Dispatch key ──
        public SkillImplType Type;
        public bool IsInitialized;

        // ── 공통 (현 SimSkillBase 필드) ──
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
        public SkillExecutionType ExecutionType;
        public bool FaceTarget;
        public bool HasProjectile;
        public int[] SkillHitFrames;
        public int SkillClipFrames;

        // ── Generic 런타임 상태 ──
        public SkillRecipe Recipe;
        public int[] ParamValues;
        public int StartDelay;
        public int TickTimer;
        public int TickInterval;
        public int RemainingTicks;
        public int TickCount;
        public int WorldTickRate;
        public int CachedTargetId;
        public bool KnockbackHitWall;
        public int ProjectileArrivalTimer;
        public int CurrentPower;
        public int BounceCount;
        public int DecayPercent;
        public int CurrentHitFrameIndex;
        public int HitFrameTimer;
        public bool HasMultiHitFrames;
        public int PostCompleteTimer;
        public bool CompleteFired;
        public int DelayTimer;

        // ── 히트 추적 ──
        public int[] HitIds;
        public int HitIdCount;

        // ── Custom: Rukida ──
        public int FoxFireIncrease;
        public int AtkSpeedRatePercent;

        // ── Custom: April ──
        public int Rate1, Rate2, Rate3;
        public int DirCol, DirRow;
        public bool Started;
        public int ClipEndTimer;
        public int HitIndex;

        // ── Custom: Enki ──
        public int HotDuration, HotInterval;
        public int PhaseTimer;
        public int ChannelFramesRemaining;
        public bool Fired, Channeling;
        public int CachedCasterCombatId, CachedAttack;
        public int StartRow, CenterCol, HalfWidth, WaveDirRow;

        // ── Custom: Adria ──
        public int DefScaleValue, StunDurationFrames;
        public int CurrentPhase;
        public bool Done;
        public long HitMask;

        // ── 읽기 전용 프로퍼티 ──
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
            IsInitialized = true;
            DelayTimer = -1;
            HitIds = new int[8];
        }

        public void Reset()
        {
            StartDelay = 0; TickTimer = 0; TickInterval = 0;
            RemainingTicks = 0; TickCount = 0;
            CachedTargetId = CombatUnit.InvalidId;
            KnockbackHitWall = false; ProjectileArrivalTimer = 0;
            CurrentPower = 0; BounceCount = 0; DecayPercent = 0;
            HitIdCount = 0; CurrentHitFrameIndex = 0; HitFrameTimer = 0;
            HasMultiHitFrames = false; PostCompleteTimer = 0; CompleteFired = false;
            DelayTimer = -1;
            if (HitIds != null)
                for (int i = 0; i < HitIds.Length; i++) HitIds[i] = CombatUnit.InvalidId;
            Started = false; ClipEndTimer = 0; HitIndex = 0; DirCol = 0; DirRow = 0;
            Channeling = false; Fired = false; PhaseTimer = 0; ChannelFramesRemaining = 0;
            CurrentPhase = 0; Done = false; HitMask = 0;
        }
    }
}
