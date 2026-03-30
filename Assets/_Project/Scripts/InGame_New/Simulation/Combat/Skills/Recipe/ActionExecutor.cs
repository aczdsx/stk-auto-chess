namespace CookApps.AutoChess
{
    /// <summary>
    /// Recipe 액션 실행기. SkillAction의 Effect 타입에 따라 기존 Helper를 호출.
    /// GC-free: lambda/delegate 없이 직접 for-loop로 순회.
    /// Quantum 호환: switch 기반 디스패치, struct context 전달.
    /// </summary>
    public static partial class ActionExecutor
    {
        // LowestHpAllies용 pre-allocated 버퍼 (최대 8명)
        private static readonly int[] LowestHpBuffer = new int[8];

        /// <summary>단일 액션 실행</summary>
        public static void Execute(ref SkillAction action, SkillExecuteContext ctx)
        {
            // VFX 스폰
            if (action.VfxIndex >= 0 && action.VfxAt != SkillVfxPlacement.None)
                SpawnVfx(ref action, ctx);

            // 효과 실행
            if (action.Effect == SkillEffectType.None)
                return;

            switch (action.Effect)
            {
                case SkillEffectType.Damage:       ExecuteDamage(ref action, ctx); break;
                case SkillEffectType.Heal:          ExecuteHeal(ref action, ctx); break;
                case SkillEffectType.ApplyCC:       ExecuteCC(ref action, ctx); break;
                case SkillEffectType.Knockback:     ExecuteKnockback(ref action, ctx); break;
                case SkillEffectType.ApplyBuff:     ExecuteBuff(ref action, ctx); break;
                case SkillEffectType.ApplyDebuff:   ExecuteDebuff(ref action, ctx); break;
                case SkillEffectType.Shield:        ExecuteShield(ref action, ctx); break;
                case SkillEffectType.RemoveDebuffs: ExecuteRemoveDebuffs(ref action, ctx); break;
                case SkillEffectType.AddMarker:     ExecuteAddMarker(ref action, ctx); break;
                case SkillEffectType.SpawnProjectile: ExecuteSpawnProjectile(ref action, ctx); break;
                case SkillEffectType.MultiHit:      ExecuteMultiHit(ref action, ctx); break;
                case SkillEffectType.ModifyStat:    ExecuteModifyStat(ref action, ctx); break;
                case SkillEffectType.SpawnLinearProjectile: ExecuteSpawnLinearProjectile(ref action, ctx); break;
                case SkillEffectType.DamageKnockbackInArea: ExecuteDamageKnockbackInArea(ref action, ctx); break;
                case SkillEffectType.SequentialLineDamage: ExecuteSequentialLineDamage(ref action, ctx); break;
                case SkillEffectType.Teleport: ExecuteTeleport(ref action, ctx); break;
                case SkillEffectType.Retarget: ExecuteRetarget(ref action, ctx); break;
                case SkillEffectType.ApplyStatusEffect: ExecuteApplyStatusEffect(ref action, ctx); break;
                case SkillEffectType.TileEffect: ExecuteTileEffect(ref action, ctx); break;
                case SkillEffectType.TeleportReturn: ExecuteTeleportReturn(ref action, ctx); break;
                case SkillEffectType.Dash: ExecuteDash(ref action, ctx); break;
                case SkillEffectType.DashReturn: ExecuteDashReturn(ref action, ctx); break;
                case SkillEffectType.DashForward: break;
                case SkillEffectType.RemoveVfx:
                    ctx.State.EventQueue?.PushSkillVfxRemove(ctx.CasterCombatId, ctx.SkillSpecId, (byte)action.VfxIndex);
                    break;
                case SkillEffectType.PlaySound:
                    ctx.State.EventQueue?.PushPlaySound(action.SoundId);
                    break;
            }
        }

        private static void GetAreaCenter(SkillExecuteContext ctx, out int col, out int row)
        {
            int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
            if (idx >= 0)
            {
                col = ctx.State.Units[idx].GridCol;
                row = ctx.State.Units[idx].GridRow;
            }
            else
            {
                ref var caster = ref ctx.GetCaster();
                col = caster.GridCol;
                row = caster.GridRow;
            }
        }
    }

    /// <summary>액션 실행에 필요한 컨텍스트 (GC-free struct)</summary>
    public struct SkillExecuteContext
    {
        public CombatMatchState State;
        public int CasterCombatId;
        public int TargetCombatId;
        public DamageType DamageType;
        public int SkillSpecId;
        public byte CasterTeam;
        public int WorldTickRate;
        public DeterministicRNG Rng;

        /// <summary>ParamSlots로 추출된 밸런스 수치 배열</summary>
        public int[] ParamValues;
        /// <summary>base PowerPercent (ParamIndex == -1일 때 사용)</summary>
        public int BasePowerPercent;
        /// <summary>현재 채널링 틱 번호 (SequentialLineDamage 등에서 step으로 사용)</summary>
        public int TickCount;

        // ── 체이닝 ──
        /// <summary>감쇠 적용된 현재 파워 (베인 바운스)</summary>
        public int CurrentPower;
        /// <summary>현재 바운스/히트 횟수</summary>
        public int BounceCount;
        /// <summary>히트한 타겟 ID 배열 참조 (GC-free: SimSkillGeneric의 고정 배열)</summary>
        public int[] HitIds;
        /// <summary>HitIds 유효 개수</summary>
        public int HitIdCount;

        /// <summary>스킬 시전 시작 위치 (TeleportReturn/AtCasterToSaved용)</summary>
        public byte SavedGridCol;
        public byte SavedGridRow;

        public ref CombatUnit GetCaster()
        {
            int idx = State.FindUnitIndex(CasterCombatId);
            if (idx < 0) idx = 0; // 캐스터 사망/제거 시 안전 폴백 (스킬 효과는 무효화됨)
            return ref State.Units[idx];
        }

        public bool IsCasterAlive()
        {
            int idx = State.FindUnitIndex(CasterCombatId);
            return idx >= 0 && State.Units[idx].CurrentHP > 0;
        }

        /// <summary>ParamSlots 참조 (AtkPercent 변환용)</summary>
        public ParamSlot[] ParamSlots;

        public int GetParamValue(int paramIndex)
        {
            if (paramIndex < 0) return BasePowerPercent;
            if (ParamValues != null && paramIndex < ParamValues.Length)
            {
                int val = ParamValues[paramIndex];
                // AtkPercent: 비율값을 공격력 기반 절대값으로 변환
                if (ParamSlots != null && paramIndex < ParamSlots.Length
                    && ParamSlots[paramIndex].ValueType == ParamValueType.AtkPercent)
                {
                    val = GetCaster().Attack * val / 100;
                }
                return val;
            }
            return BasePowerPercent;
        }
    }
}
