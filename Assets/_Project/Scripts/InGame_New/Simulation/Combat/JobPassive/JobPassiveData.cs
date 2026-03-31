namespace CookApps.AutoChess
{
    /// <summary>GUARDIAN: 쿨타임마다 일반공격 N회 무시 베리어</summary>
    public struct GuardianPassive
    {
        public bool Active;
        public int CooldownFrames;   // 쉴드 재충전 쿨타임 (프레임)
        public int MaxCharges;       // 최대 충전 횟수
        public int Timer;            // 현재 타이머
        public int ShieldCharges;    // 남은 쉴드 충전 횟수
    }

    /// <summary>GHOST: N타마다 확정 크리티컬</summary>
    public struct GhostPassive
    {
        public bool Active;
        public int MaxStack;            // 확정 크리 필요 스택
        public int Stack;               // 현재 공격 스택
        public bool NextCrit;           // 다음 공격 확정 크리 플래그
        public int SavedCritRate;       // 원본 크리율 백업
        public bool CritOverrideActive; // 크리 오버라이드 활성 여부
    }

    /// <summary>STRIKER: 쿨타임마다 CC 면역 1회 부여</summary>
    public struct StrikerPassive
    {
        public bool Active;
        public int CooldownFrames; // CC 면역 재충전 쿨타임 (프레임)
        public int Timer;          // 현재 타이머
    }

    /// <summary>SHARPSHOOTER: 확률적 방어 완전 관통</summary>
    public struct SharpshooterPassive
    {
        public bool Active;
        public int ChancePercent;  // 발동 확률 (정수 %)
        public int SavedAtkPierce; // 원본 AtkPierce 백업
        public int SavedResPierce; // 원본 ResPierce 백업
        public bool PierceActive;  // 현재 공격에서 관통 활성 여부
    }

    /// <summary>ESPER: 확률적 주변 3×3 폭발</summary>
    public struct EsperPassive
    {
        public bool Active;
        public int ChancePercent; // 폭발 발동 확률 (정수 %)
        public int DamagePercent; // 폭발 데미지 비율
    }

    /// <summary>ORACLE: 평타로 아군 힐</summary>
    public struct OraclePassive
    {
        public bool Active;
        public int HealPercent; // 회복 비율 (정수 %)
    }

    /// <summary>특정 캐릭터 전용: 스킬 킬 시 마나 즉시 충전</summary>
    public struct SkillKillManaData
    {
        public bool Active;
        public int MarkerType; // SkillMarkerType (int 변환)
    }
}
