namespace CookApps.AutoChess
{
    /// <summary>시뮬레이션 이벤트 타입</summary>
    public enum SimEventType : byte
    {
        // 유닛
        UnitSpawned,
        UnitMoved,
        UnitAttacked,
        UnitDamaged,
        UnitDied,
        UnitCastSkill,
        UnitCombined,
        // 투사체
        ProjectileSpawned,
        ProjectileHit,
        ProjectileMoved,
        ProjectileExploded,
        ProjectileExpired,
        // 게임 흐름
        PhaseChanged,
        CombatResult,
        PlayerEliminated,
        GameOver,
        // 경제/상점
        GoldChanged,
        LevelUp,
        ShopRefreshed,
        UnitPurchased,
        UnitSold,
        // 시너지/아이템
        SynergyUpdated,
        ItemEquipped,
        ItemUnequipped,
        ItemCombined,
    }

    /// <summary>
    /// 시뮬레이션 이벤트. 시뮬레이션에서 발생하여 View 레이어로 전달.
    /// 공용 필드로 모든 이벤트 타입을 커버 (유니온 대신 flat 구조체, GC 회피).
    /// </summary>
    public struct SimEvent
    {
        public SimEventType Type;

        // 공통
        public byte PlayerIndex;
        public int EntityId;         // 주체 유닛/아이템
        public int TargetEntityId;   // 대상 유닛

        // 위치
        public byte Col;
        public byte Row;

        // 수치
        public int Value0;           // 데미지, 골드, 레벨 등
        public int Value1;           // 추가 수치
        public bool Flag0;           // isCrit, isProjectile 등

        // 페이즈
        public GamePhase Phase;
        public GamePhase PrevPhase;

        // 투사체
        public ProjectileType ProjType;
        public byte DirCol;
        public byte DirRow;
        public int Radius;
    }

    /// <summary>
    /// 시뮬레이션 이벤트 큐. 틱마다 이벤트를 쌓고 View에서 소비.
    /// </summary>
    public class SimEventQueue
    {
        public const int MaxEvents = 128;

        public SimEvent[] Events;
        public int Count;

        public SimEventQueue()
        {
            Events = new SimEvent[MaxEvents];
            Count = 0;
        }

        public void Push(SimEvent evt)
        {
            if (Count >= MaxEvents) return; // 오버플로우 방지
            Events[Count++] = evt;
        }

        public void Clear()
        {
            Count = 0;
        }

        // ── 팩토리 헬퍼 ──

        public void PushUnitMoved(int entityId, byte col, byte row)
        {
            Push(new SimEvent
            {
                Type = SimEventType.UnitMoved,
                EntityId = entityId,
                Col = col,
                Row = row,
            });
        }

        public void PushUnitAttacked(int attackerId, int targetId, int damage, bool isCrit, bool isProjectile)
        {
            Push(new SimEvent
            {
                Type = SimEventType.UnitAttacked,
                EntityId = attackerId,
                TargetEntityId = targetId,
                Value0 = damage,
                Flag0 = isCrit,
            });
        }

        public void PushUnitDamaged(int targetId, int sourceId, int damage, DamageType damageType)
        {
            Push(new SimEvent
            {
                Type = SimEventType.UnitDamaged,
                EntityId = targetId,
                TargetEntityId = sourceId,
                Value0 = damage,
                Value1 = (int)damageType,
            });
        }

        public void PushUnitDied(int entityId, int killerId)
        {
            Push(new SimEvent
            {
                Type = SimEventType.UnitDied,
                EntityId = entityId,
                TargetEntityId = killerId,
            });
        }

        public void PushPhaseChanged(GamePhase prev, GamePhase current)
        {
            Push(new SimEvent
            {
                Type = SimEventType.PhaseChanged,
                PrevPhase = prev,
                Phase = current,
            });
        }

        public void PushCombatResult(byte matchIndex, byte winner, byte playerA, byte playerB)
        {
            Push(new SimEvent
            {
                Type = SimEventType.CombatResult,
                PlayerIndex = matchIndex,
                Value0 = winner,
                Value1 = (playerA << 8) | playerB,
            });
        }

        public void PushPlayerEliminated(byte playerIndex, byte rank)
        {
            Push(new SimEvent
            {
                Type = SimEventType.PlayerEliminated,
                PlayerIndex = playerIndex,
                Value0 = rank,
            });
        }

        public void PushGoldChanged(byte playerIndex, int totalGold, int delta)
        {
            Push(new SimEvent
            {
                Type = SimEventType.GoldChanged,
                PlayerIndex = playerIndex,
                Value0 = totalGold,
                Value1 = delta,
            });
        }

        public void PushLevelUp(byte playerIndex, byte newLevel)
        {
            Push(new SimEvent
            {
                Type = SimEventType.LevelUp,
                PlayerIndex = playerIndex,
                Value0 = newLevel,
            });
        }

        public void PushUnitCastSkill(int casterId, int targetId, int skillSpecId)
        {
            Push(new SimEvent
            {
                Type = SimEventType.UnitCastSkill,
                EntityId = casterId,
                TargetEntityId = targetId,
                Value0 = skillSpecId,
            });
        }

        public void PushProjectileExploded(byte col, byte row, int radius, int skillSpecId = 0)
        {
            Push(new SimEvent
            {
                Type = SimEventType.ProjectileExploded,
                Col = col,
                Row = row,
                Radius = radius,
                Value0 = skillSpecId,
            });
        }

        public void PushSynergyUpdated(byte playerIndex)
        {
            Push(new SimEvent
            {
                Type = SimEventType.SynergyUpdated,
                PlayerIndex = playerIndex,
            });
        }
    }
}
