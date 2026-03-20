namespace CookApps.AutoChess
{
    /// <summary>CombatVfxType + StatModType 패킹/언패킹 헬퍼 (하위8비트=VfxType, 상위8비트=StatModType)</summary>
    public static class SimEventHelper
    {
        public static int EncodeVfxStat(CombatVfxType vfx, StatModType stat = default)
            => (int)vfx | ((int)stat << 8);
        public static CombatVfxType DecodeVfxType(int packed) => (CombatVfxType)(packed & 0xFF);
        public static StatModType DecodeStatType(int packed) => (StatModType)((packed >> 8) & 0xFF);
    }

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
        SkillAreaEffect,
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
        // Phase별 스킬 VFX
        SkillPhaseVfx,
        SkillRectAreaEffect,
        // 전투 피드백
        UnitMissed,
        UnitHealed,
        // 전투 VFX (상태효과/CC)
        StatusEffectAdded,
        StatusEffectRemoved,
        CCAdded,
        CCRemoved,
        ManaFull,
        // 버프 아이콘 (SkillMarker)
        SkillMarkerAdded,    // Value0=markerId(skillSpecId), Value1=totalFrames
        SkillMarkerRemoved,  // Value0=markerId(skillSpecId), Value1=remainingCount
        // 슈퍼노바 오브젝트
        SupernovaObjectEvent,  // Value0=traitId, Value1=subType, Col/Row, EntityId(TargetAssigned시)
        // 카메라 연출
        CameraShake,  // Value0=duration(ms), Value1=magnitude(x100)
    }

    /// <summary>슈퍼노바 오브젝트 이벤트 서브타입</summary>
    public static class SupernovaSubType
    {
        public const byte Spawn = 0;
        public const byte Remove = 1;
        public const byte Move = 2;
        public const byte TierChanged = 3;
        public const byte TargetAssigned = 4;
        public const byte TargetRemoved = 5;
        public const byte InvalidDrop = 6;  // trait 불일치 유닛이 구체 위치에 배치됨
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
        public bool Flag1;           // hasProjectile (UnitCastSkill) 등

        // 페이즈
        public GamePhase Phase;
        public GamePhase PrevPhase;

        // 투사체
        public ProjectileType ProjType;
        public byte DirCol;
        public byte DirRow;
        public int Radius;

        // 투사체 VFX
        public sbyte SkillVfxIndex;     // 투사체 VFX 인덱스
        public sbyte ArrivalVfxIndex;   // 도착 시 스폰할 VFX 인덱스 (-1이면 없음)
        public short MoveInterval;      // 타일 이동 간격 (프레임)
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

        public void PushUnitAttacked(int attackerId, int targetId, int damage, bool isCrit, bool isProjectile, bool isPreTimed = false)
        {
            // Value1 비트 패킹: bit0 = isProjectile, bit1 = isPreTimed (시뮬레이션 타이밍 완료)
            int flags = (isProjectile ? 1 : 0) | (isPreTimed ? 2 : 0);
            Push(new SimEvent
            {
                Type = SimEventType.UnitAttacked,
                EntityId = attackerId,
                TargetEntityId = targetId,
                Value0 = damage,
                Flag0 = isCrit,
                Value1 = flags,
            });
        }

        public void PushUnitDamaged(int targetId, int sourceId, int damage, DamageType damageType, bool isCrit = false)
        {
            Push(new SimEvent
            {
                Type = SimEventType.UnitDamaged,
                EntityId = targetId,
                TargetEntityId = sourceId,
                Value0 = damage,
                Value1 = (int)damageType,
                Flag0 = isCrit,
            });
        }

        public void PushUnitDied(int entityId, int killerId, int combatId = -1)
        {
            Push(new SimEvent
            {
                Type = SimEventType.UnitDied,
                EntityId = entityId,
                TargetEntityId = killerId,
                Value0 = combatId,
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

        public void PushUnitCastSkill(int casterId, int targetId, int skillSpecId, bool skipVfx = false, bool hasProjectile = false)
        {
            Push(new SimEvent
            {
                Type = SimEventType.UnitCastSkill,
                EntityId = casterId,
                TargetEntityId = targetId,
                Value0 = skillSpecId,
                Flag0 = skipVfx,
                Flag1 = hasProjectile,
            });
        }

        public void PushProjectileSpawned(int sourceId, int targetId, ProjectileType projType,
            byte col, byte row, sbyte dirCol = 0, sbyte dirRow = 0, int projectileId = 0, int skillSpecId = 0,
            sbyte skillVfxIndex = -1, int moveInterval = 0, bool useBezier = false, sbyte arrivalVfxIndex = -1)
        {
            Push(new SimEvent
            {
                Type = SimEventType.ProjectileSpawned,
                EntityId = sourceId,
                TargetEntityId = targetId,
                ProjType = projType,
                Col = col,
                Row = row,
                DirCol = (byte)dirCol,
                DirRow = (byte)dirRow,
                Value0 = projectileId,
                Value1 = skillSpecId,
                SkillVfxIndex = skillVfxIndex,
                ArrivalVfxIndex = arrivalVfxIndex,
                MoveInterval = (short)moveInterval,
                Flag0 = useBezier,
            });
        }

        public void PushProjectileMoved(int projectileId, int sourceId, byte col, byte row,
            sbyte dirCol = 0, sbyte dirRow = 0, int width = 1)
        {
            Push(new SimEvent
            {
                Type = SimEventType.ProjectileMoved,
                EntityId = sourceId,
                Value0 = projectileId,
                Col = col,
                Row = row,
                DirCol = (byte)dirCol,
                DirRow = (byte)dirRow,
                Radius = width,
            });
        }

        public void PushProjectileExpired(int projectileId, int sourceId)
        {
            Push(new SimEvent
            {
                Type = SimEventType.ProjectileExpired,
                EntityId = sourceId,
                Value0 = projectileId,
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

        /// <summary>스킬 범위 타일 이펙트 (채널링 틱 등). isRow=true이면 행 단위(col±radius), false이면 맨해튼 거리 기반. isBox=true이면 체비셰프(네모) 범위.</summary>
        public void PushSkillAreaEffect(int casterId, byte col, byte row, int radius, bool isRow = false, bool isBox = false)
        {
            Push(new SimEvent
            {
                Type = SimEventType.SkillAreaEffect,
                EntityId = casterId,
                Col = col,
                Row = row,
                Radius = radius,
                Flag0 = isRow,
                Value1 = isBox ? 1 : 0,
            });
        }

        /// <summary>
        /// Phase별 스킬 VFX 이벤트 발행.
        /// vfxIndex = skill_vfxs 배열 인덱스.
        /// dirCol/dirRow: VFX 방향(0이면 방향 없음).
        /// targetId: VFX를 타겟 유닛 위치에 스폰 (0이면 무시).
        /// col/row: VFX를 그리드 좌표에 스폰 (Flag0=true일 때 사용).
        /// </summary>
        public void PushSkillPhaseVfx(int casterId, int skillSpecId, byte vfxIndex,
            sbyte dirCol = 0, sbyte dirRow = 0, int targetId = 0,
            byte col = 0, byte row = 0, bool useGridPos = false)
        {
            Push(new SimEvent
            {
                Type = SimEventType.SkillPhaseVfx,
                EntityId = casterId,
                TargetEntityId = targetId,
                Value0 = skillSpecId,
                Value1 = vfxIndex,
                DirCol = (byte)dirCol,
                DirRow = (byte)dirRow,
                Col = col,
                Row = row,
                Flag0 = useGridPos,
            });
        }

        /// <summary>ㄷ자형 범위 타일 이펙트. 타겟 방향 기준 2×3.</summary>
        public void PushSkillRectAreaEffect(int casterId, byte col, byte row, sbyte dirCol, sbyte dirRow)
        {
            Push(new SimEvent
            {
                Type = SimEventType.SkillRectAreaEffect,
                EntityId = casterId,
                Col = col,
                Row = row,
                DirCol = (byte)dirCol,
                DirRow = (byte)dirRow,
            });
        }

        public void PushUnitMissed(int attackerId, int targetId)
        {
            Push(new SimEvent
            {
                Type = SimEventType.UnitMissed,
                EntityId = attackerId,
                TargetEntityId = targetId,
            });
        }

        public void PushUnitHealed(int targetId, int amount)
        {
            Push(new SimEvent
            {
                Type = SimEventType.UnitHealed,
                EntityId = targetId,
                Value0 = amount,
            });
        }

        public void PushManaFull(int entityId, int skillSpecId)
        {
            Push(new SimEvent
            {
                Type = SimEventType.ManaFull,
                EntityId = entityId,
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

        public void PushStatusEffectAdded(int combatId, CombatVfxType vfxType, int totalFrames = 0, StatModType statType = default)
        {
            Push(new SimEvent
            {
                Type = SimEventType.StatusEffectAdded,
                EntityId = combatId,
                Value0 = SimEventHelper.EncodeVfxStat(vfxType, statType),
                Value1 = totalFrames,
            });
        }

        public void PushStatusEffectRemoved(int combatId, CombatVfxType vfxType, StatModType statType = default)
        {
            Push(new SimEvent
            {
                Type = SimEventType.StatusEffectRemoved,
                EntityId = combatId,
                Value0 = SimEventHelper.EncodeVfxStat(vfxType, statType),
            });
        }

        public void PushCCAdded(int combatId, CombatVfxType vfxType, int totalFrames = 0)
        {
            Push(new SimEvent
            {
                Type = SimEventType.CCAdded,
                EntityId = combatId,
                Value0 = (int)vfxType,
                Value1 = totalFrames,
            });
        }

        public void PushCCRemoved(int combatId, CombatVfxType vfxType)
        {
            Push(new SimEvent
            {
                Type = SimEventType.CCRemoved,
                EntityId = combatId,
                Value0 = (int)vfxType,
            });
        }

        public void PushSkillMarkerAdded(int combatId, int markerId, int totalFrames)
        {
            Push(new SimEvent
            {
                Type = SimEventType.SkillMarkerAdded,
                EntityId = combatId,
                Value0 = markerId,
                Value1 = totalFrames,
            });
        }

        public void PushSkillMarkerRemoved(int combatId, int markerId, int remainingCount)
        {
            Push(new SimEvent
            {
                Type = SimEventType.SkillMarkerRemoved,
                EntityId = combatId,
                Value0 = markerId,
                Value1 = remainingCount,
            });
        }

        /// <summary>카메라 쉐이킹 이벤트. duration=밀리초, magnitude=x100 정수(0.15f → 15).</summary>
        public void PushCameraShake(int durationMs, int magnitudeX100)
        {
            Push(new SimEvent
            {
                Type = SimEventType.CameraShake,
                Value0 = durationMs,
                Value1 = magnitudeX100,
            });
        }

        public void PushSupernovaObjectEvent(byte playerIndex, int traitId, byte subType,
            byte col, byte row, int entityId = 0)
        {
            Push(new SimEvent
            {
                Type = SimEventType.SupernovaObjectEvent,
                PlayerIndex = playerIndex,
                Value0 = traitId,
                Value1 = subType,
                Col = col,
                Row = row,
                EntityId = entityId,
            });
        }
    }
}
