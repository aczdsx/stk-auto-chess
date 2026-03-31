namespace CookApps.AutoChess
{
    /// <summary>
    /// 직업 패시브 SOA 인라인 로직.
    /// 기존 CombatTraitBase virtual 디스패치를 대체하여 각 시스템에서 직접 호출.
    /// Quantum ECS 포팅 시 각 메서드가 개별 System으로 1:1 매핑됨.
    /// </summary>
    public static class JobPassiveLogic
    {
        // ===== 콜백 포인트별 집합 메서드 =====

        /// <summary>전투 시작 시 1회 호출 (Guardian 쉴드 초기화, Striker CC면역 즉시 부여)</summary>
        public static void OnCombatStart(CombatMatchState state, int unitIndex)
        {
            GuardianOnCombatStart(state, unitIndex);
            StrikerOnCombatStart(state, unitIndex);
        }

        /// <summary>매 틱 호출 (Guardian 쿨타임→쉴드충전, Striker 쿨타임→CC면역)</summary>
        public static void OnTick(CombatMatchState state, int unitIndex)
        {
            GuardianOnTick(state, unitIndex);
            StrikerOnTick(state, unitIndex);
        }

        /// <summary>기본 공격 전 (Ghost 확정크리 설정, Sharpshooter 관통 설정)</summary>
        public static void OnPreAttack(CombatMatchState state, int attackerIndex)
        {
            GhostOnPreAttack(state, attackerIndex);
            SharpshooterOnPreAttack(state, attackerIndex);
        }

        /// <summary>기본 공격 후 (Ghost 크리 복원, Sharpshooter 관통 복원, Esper 폭발)</summary>
        public static void OnPostAttack(CombatMatchState state, int attackerIndex, ref CombatUnit target)
        {
            GhostOnPostAttack(state, attackerIndex);
            SharpshooterOnPostAttack(state, attackerIndex);
            EsperOnPostAttack(state, attackerIndex, ref target);
        }

        /// <summary>적 처치 시 (스킬 킬 마나 리셋)</summary>
        public static void OnKill(CombatMatchState state, int killerIndex, ref CombatUnit victim)
        {
            SkillKillManaOnKill(state, killerIndex, ref victim);
        }

        /// <summary>들어오는 데미지 보정 (Guardian 쉴드 차단)</summary>
        public static int ModifyIncomingDamage(CombatMatchState state, int targetIndex, int damage, bool isBasicAttack)
        {
            return GuardianModifyIncomingDamage(state, targetIndex, damage, isBasicAttack);
        }
        // ===== GUARDIAN: 쿨타임마다 일반공격 N회 무시 베리어 =====

        public static void GuardianOnCombatStart(CombatMatchState state, int unitIndex)
        {
            ref var g = ref state.GuardianPassives[unitIndex];
            if (!g.Active) return;

            g.ShieldCharges = g.MaxCharges;
            g.Timer = 0;
            state.EventQueue?.PushStatusEffectAdded(
                state.Units[unitIndex].CombatId, CombatVfxType.BasicAttackShield, g.ShieldCharges);
        }

        public static void GuardianOnTick(CombatMatchState state, int unitIndex)
        {
            ref var g = ref state.GuardianPassives[unitIndex];
            if (!g.Active) return;

            g.Timer++;
            if (g.Timer >= g.CooldownFrames)
            {
                g.ShieldCharges = g.MaxCharges;
                g.Timer = 0;
                state.EventQueue?.PushStatusEffectAdded(
                    state.Units[unitIndex].CombatId, CombatVfxType.BasicAttackShield, g.ShieldCharges);
            }
        }

        /// <summary>Guardian 쉴드로 일반공격 차단. 차단 시 0 반환.</summary>
        public static int GuardianModifyIncomingDamage(
            CombatMatchState state, int targetIndex, int damage, bool isBasicAttack)
        {
            ref var g = ref state.GuardianPassives[targetIndex];
            if (!g.Active || !isBasicAttack || g.ShieldCharges <= 0)
                return damage;

            g.ShieldCharges--;

            if (g.ShieldCharges <= 0)
                state.EventQueue?.PushStatusEffectRemoved(
                    state.Units[targetIndex].CombatId, CombatVfxType.BasicAttackShield);
            else
                state.EventQueue?.PushStatusEffectAdded(
                    state.Units[targetIndex].CombatId, CombatVfxType.BasicAttackShield, g.ShieldCharges);

            return 0;
        }

        // ===== STRIKER: 쿨타임마다 CC 면역 1회 부여 =====

        public static void StrikerOnCombatStart(CombatMatchState state, int unitIndex)
        {
            ref var s = ref state.StrikerPassives[unitIndex];
            if (!s.Active) return;

            state.Units[unitIndex].CCImmuneCharges = 1;
            s.Timer = 0;
            state.EventQueue?.PushStatusEffectAdded(
                state.Units[unitIndex].CombatId, CombatVfxType.JobStriker, -1);
        }

        public static void StrikerOnTick(CombatMatchState state, int unitIndex)
        {
            ref var s = ref state.StrikerPassives[unitIndex];
            if (!s.Active) return;
            if (state.Units[unitIndex].CCImmuneCharges > 0) return;

            s.Timer++;
            if (s.Timer >= s.CooldownFrames)
            {
                state.Units[unitIndex].CCImmuneCharges = 1;
                s.Timer = 0;
                state.EventQueue?.PushStatusEffectAdded(
                    state.Units[unitIndex].CombatId, CombatVfxType.JobStriker, -1);
            }
        }

        // ===== GHOST: N타마다 확정 크리티컬 =====

        public static void GhostOnPreAttack(CombatMatchState state, int attackerIndex)
        {
            ref var g = ref state.GhostPassives[attackerIndex];
            if (!g.Active) return;

            g.CritOverrideActive = false;
            if (g.NextCrit)
            {
                g.SavedCritRate = state.Units[attackerIndex].CritRate;
                state.Units[attackerIndex].CritRate = 100;
                g.CritOverrideActive = true;
                g.NextCrit = false;
            }
        }

        public static void GhostOnPostAttack(CombatMatchState state, int attackerIndex)
        {
            ref var g = ref state.GhostPassives[attackerIndex];
            if (!g.Active) return;

            if (g.CritOverrideActive)
            {
                state.Units[attackerIndex].CritRate = g.SavedCritRate;
                g.CritOverrideActive = false;
            }

            g.Stack++;
            if (g.Stack >= g.MaxStack)
            {
                g.NextCrit = true;
                g.Stack = 0;
            }
        }

        // ===== SHARPSHOOTER: 확률적 방어 완전 관통 =====

        public static void SharpshooterOnPreAttack(CombatMatchState state, int attackerIndex)
        {
            ref var ss = ref state.SharpshooterPassives[attackerIndex];
            if (!ss.Active || ss.ChancePercent <= 0) return;

            ss.PierceActive = false;
            if (state.Rng.Chance(ss.ChancePercent))
            {
                ss.SavedAtkPierce = state.Units[attackerIndex].AtkPierce;
                ss.SavedResPierce = state.Units[attackerIndex].ResPierce;
                state.Units[attackerIndex].AtkPierce = 100;
                state.Units[attackerIndex].ResPierce = 100;
                ss.PierceActive = true;
                state.Units[attackerIndex].ProjectileVfxOverride = ProjectileVfxId.SharpshooterAD;
            }
        }

        public static void SharpshooterOnPostAttack(CombatMatchState state, int attackerIndex)
        {
            ref var ss = ref state.SharpshooterPassives[attackerIndex];
            if (!ss.Active || !ss.PierceActive) return;

            state.Units[attackerIndex].AtkPierce = ss.SavedAtkPierce;
            state.Units[attackerIndex].ResPierce = ss.SavedResPierce;
            ss.PierceActive = false;
        }

        // ===== ESPER: 확률적 주변 3×3 폭발 =====

        public static void EsperOnPostAttack(
            CombatMatchState state, int attackerIndex, ref CombatUnit target)
        {
            ref var e = ref state.EsperPassives[attackerIndex];
            if (!e.Active || e.ChancePercent <= 0) return;
            if (!target.IsAlive) return;

            if (state.Rng.Chance(e.ChancePercent))
            {
                int dmgPct = e.DamagePercent > 255 ? 255 : e.DamagePercent;
                JobPassiveSystem.ProcessEsperExplosion(
                    state, ref state.Units[attackerIndex], ref target, dmgPct);
            }
        }

        // ===== ORACLE: 평타로 아군 힐 =====

        public const int OracleHealTargetHPThreshold = 50; // HP비율 이 값 미만인 아군만 힐 대상
        public const int OracleHealRangeBonus = 0;         // 힐 타겟 탐색 시 사거리 보정

        public static bool IsOracleHealer(CombatMatchState state, int unitIndex)
        {
            return state.OraclePassives[unitIndex].Active;
        }

        /// <summary>힐량: Attack * HealPercent / 100, 양쪽 HealPower 적용</summary>
        public static int OracleCalculateHealAmount(
            CombatMatchState state, int healerIndex, ref CombatUnit target)
        {
            ref var o = ref state.OraclePassives[healerIndex];
            if (!o.Active) return 0;

            ref var healer = ref state.Units[healerIndex];
            int amount = healer.Attack * o.HealPercent / 100;
            amount = amount * (100 + healer.HealPower) / 100;
            amount = amount * (100 + target.HealPower) / 100;
            if (amount < 1) amount = 1;
            return amount;
        }

        // ===== SKILL KILL MANA: 스킬 킬 시 마나 즉시 충전 =====

        public static void SkillKillManaOnKill(
            CombatMatchState state, int killerIndex, ref CombatUnit victim)
        {
            ref var skm = ref state.SkillKillManaPassives[killerIndex];
            if (!skm.Active) return;

            ref var killer = ref state.Units[killerIndex];
            if (killer.MaxMana <= 0 || !killer.IsAlive) return;

            if (StatusEffectSystem.CountMarkers(state, killerIndex, skm.MarkerType) > 0)
            {
                killer.CurrentMana = killer.MaxMana;
                StatusEffectSystem.RemoveOldestMarker(state, killerIndex, skm.MarkerType);
            }
        }
    }
}
