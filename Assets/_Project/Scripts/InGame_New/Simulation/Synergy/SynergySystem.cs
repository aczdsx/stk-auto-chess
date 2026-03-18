using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 시너지 시스템. 보드 유닛의 특성을 집계하여 활성 시너지 결정.
    /// 전투 시작 시 CombatUnit에 시너지 효과를 적용.
    /// </summary>
    public static class SynergySystem
    {
        // ── 시너지 재계산 ──

        /// <summary>
        /// 플레이어의 시너지 상태 재계산.
        /// 보드 변경 시 (배치/회수/교환) + 전투 시작 시 호출.
        /// </summary>
        public static void Recalculate(GameWorld world, byte playerIndex)
        {
            if (!world.Config.EnableSynergy) return;
            if (world.SynergySpecs == null || world.SynergySpecCount == 0) return;

            var synergy = world.Synergies[playerIndex];
            synergy.Clear();

            // 보드 유닛의 고유 챔피언 특성 집계 (같은 챔피언은 1회만)
            int countedChampionFlags = 0; // 이미 집계한 ChampionSpecId 비트마스크 (간이)
            var countedIds = new int[8];  // 최대 8유닛
            int countedCount = 0;

            var boardSlots = world.BoardSlots[playerIndex];
            for (int slot = 0; slot < world.BoardSize; slot++)
            {
                int entityId = boardSlots[slot];
                if (entityId == UnitData.InvalidId) continue;

                int unitIdx = world.FindUnitIndex(entityId);
                if (unitIdx < 0) continue;

                ref var unit = ref world.Units[unitIdx];
                if (!unit.IsValid) continue;

                // 중복 챔피언 체크
                bool alreadyCounted = false;
                for (int c = 0; c < countedCount; c++)
                {
                    if (countedIds[c] == unit.ChampionSpecId)
                    {
                        alreadyCounted = true;
                        break;
                    }
                }
                if (alreadyCounted) continue;

                if (countedCount < 8)
                    countedIds[countedCount++] = unit.ChampionSpecId;

                // 특성 비트 스캔
                int flags = unit.TraitFlags;
                for (int bit = 0; bit < PlayerSynergy.MaxTraits && flags != 0; bit++)
                {
                    if ((flags & 1) != 0)
                    {
                        byte cur = synergy.GetTraitCount(bit);
                        synergy.SetTraitCount(bit, (byte)(cur + 1));
                    }
                    flags >>= 1;
                }
            }

            // 각 특성의 활성 티어 결정
            for (int t = 0; t < world.SynergySpecCount; t++)
            {
                ref var spec = ref world.SynergySpecs[t];
                if (!spec.IsValid) continue;

                int traitId = spec.TraitId;
                if (traitId < 0 || traitId >= PlayerSynergy.MaxTraits) continue;

                byte unitCount = synergy.GetTraitCount(traitId);
                if (unitCount == 0) continue;

                // 가장 높은 충족 티어 결정 (높은 것부터 검사)
                byte bestTier = 0;
                for (int tier = spec.Tiers.Length - 1; tier >= 0; tier--)
                {
                    if (unitCount >= spec.Tiers[tier].RequiredCount)
                    {
                        bestTier = (byte)(tier + 1);
                        break;
                    }
                }

                if (bestTier > 0)
                {
                    synergy.SetTraitTier(traitId, bestTier);
                    synergy.ActiveSynergyCount++;
                    LogSynergyRecalc((SynergyType)traitId, playerIndex, bestTier, unitCount, spec);
                }
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void LogSynergyRecalc(SynergyType type, byte playerIndex, byte tier, byte unitCount,
            in SynergySpec spec)
        {
            int tierIndex = tier - 1;
            if (tierIndex < 0 || tierIndex >= spec.Tiers.Length) return;

            ref var tierData = ref spec.Tiers[tierIndex];
            var sb = new System.Text.StringBuilder();
            sb.Append($"<color=green>[Synergy] P{playerIndex} {type} ACTIVE tier={tier} units={unitCount}");

            if (tierData.Effects != null)
            {
                sb.Append(" | ");
                for (int e = 0; e < tierData.Effects.Length; e++)
                {
                    ref var eff = ref tierData.Effects[e];
                    if (e > 0) sb.Append(", ");
                    if (eff.Value != 0)
                        sb.Append($"{eff.Type}({eff.Target})+{eff.Value}");
                    else if (eff.ValuePercent != 0)
                        sb.Append($"{eff.Type}({eff.Target})+{eff.ValuePercent}%");
                    else
                        sb.Append($"{eff.Type}({eff.Target})");
                }
            }

            sb.Append("</color>");
            UnityEngine.Debug.Log(sb.ToString());
        }

        // ── 전투 시작 시 시너지 효과 적용 ──

        /// <summary>
        /// CombatUnit들에 시너지 효과 적용.
        /// 아이템 스탯 적용 후, 전투 첫 프레임 전에 호출.
        /// </summary>
        public static void ApplyEffects(GameWorld world, CombatMatchState matchState, byte playerIndex, byte teamIndex)
        {
            if (!world.Config.EnableSynergy) return;
            if (world.SynergySpecs == null) return;

            var synergy = world.Synergies[playerIndex];

            for (int t = 0; t < world.SynergySpecCount; t++)
            {
                ref var spec = ref world.SynergySpecs[t];
                if (!spec.IsValid) continue;

                int traitId = spec.TraitId;
                byte tier = synergy.GetTraitTier(traitId);
                if (tier == 0) continue;

                int tierIndex = tier - 1;
                if (tierIndex >= spec.Tiers.Length) continue;

                ref var tierData = ref spec.Tiers[tierIndex];
                if (tierData.Effects == null) continue;

                // PrepTarget 효과가 있고 아직 대상 미지정이면 자동 배정
                bool hasPrepTarget = false;
                for (int e = 0; e < tierData.Effects.Length; e++)
                {
                    if (tierData.Effects[e].Target == SynergyTarget.PrepTarget)
                    {
                        hasPrepTarget = true;
                        break;
                    }
                }

                if (hasPrepTarget)
                {
                    int prepIdx = FindPrepBehavior(world, playerIndex, traitId);
                    if (prepIdx >= 0)
                    {
                        var prep = world.PrepBehaviors[playerIndex][prepIdx];
                        if (prep.PrepTargetEntityId == -1)
                        {
                            // SUPERNOVA TraitFlag를 가진 보드 유닛 중 랜덤 1명 선택
                            AutoAssignPrepTarget(world, playerIndex, traitId, prep);

                            // 자동 배정 성공 시 구체 제거 + 타겟 부여 이벤트
                            if (prep.PrepTargetEntityId >= 0 && prep is SynergyPrepSupernova sn)
                            {
                                if (sn.ObjectCol >= 0)
                                {
                                    world.EventQueue.PushSupernovaObjectEvent(
                                        playerIndex, traitId, SupernovaSubType.Remove,
                                        (byte)sn.ObjectCol, (byte)sn.ObjectRow);
                                    sn.ObjectCol = -1;
                                    sn.ObjectRow = -1;
                                }
                                world.EventQueue.PushSupernovaObjectEvent(
                                    playerIndex, traitId, SupernovaSubType.TargetAssigned,
                                    0, 0, prep.PrepTargetEntityId);
                                world.EventQueue.PushSynergyUpdated(playerIndex);
                            }
                        }
                    }
                }

                for (int e = 0; e < tierData.Effects.Length; e++)
                {
                    ref var effect = ref tierData.Effects[e];
                    ApplySingleEffect(world, matchState, ref effect, traitId, teamIndex, playerIndex);
                }
            }
        }

        /// <summary>PrepTarget 미지정 시 보드 위 해당 시너지 유닛 중 랜덤 1명 자동 선택</summary>
        private static void AutoAssignPrepTarget(GameWorld world, byte playerIndex, int traitId,
            SynergyPrepBehaviorBase prep)
        {
            int traitBit = 1 << traitId;
            var boardSlots = world.BoardSlots[playerIndex];

            // 후보 수집 (스택 할당)
            var candidates = new int[8];
            int candidateCount = 0;

            for (int slot = 0; slot < world.BoardSize; slot++)
            {
                int entityId = boardSlots[slot];
                if (entityId == UnitData.InvalidId) continue;

                int unitIdx = world.FindUnitIndex(entityId);
                if (unitIdx < 0) continue;

                ref var unit = ref world.Units[unitIdx];
                if (!unit.IsValid) continue;
                if ((unit.TraitFlags & traitBit) == 0) continue;

                if (candidateCount < candidates.Length)
                    candidates[candidateCount++] = entityId;
            }

            if (candidateCount > 0)
            {
                int pick = world.RNG.Range(0, candidateCount);
                prep.PrepTargetEntityId = candidates[pick];
            }
        }

        private static void ApplySingleEffect(GameWorld world, CombatMatchState state,
            ref SynergyEffect effect, int traitId, byte teamIndex, byte playerIndex)
        {
            int traitBit = 1 << traitId;

            // PrepTarget인 경우 대상 EntityId 조회
            int prepTargetEntityId = -1;
            if (effect.Target == SynergyTarget.PrepTarget)
            {
                int prepIdx = FindPrepBehavior(world, playerIndex, traitId);
                if (prepIdx >= 0)
                    prepTargetEntityId = world.PrepBehaviors[playerIndex][prepIdx].PrepTargetEntityId;
                if (prepTargetEntityId == -1) return; // 대상 없으면 스킵
            }

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;

                // 대상 필터링
                switch (effect.Target)
                {
                    case SynergyTarget.TraitUnits:
                        if (unit.TeamIndex != teamIndex) continue;
                        if ((unit.TraitFlags & traitBit) == 0) continue;
                        break;
                    case SynergyTarget.AllAllies:
                        if (unit.TeamIndex != teamIndex) continue;
                        break;
                    case SynergyTarget.AllEnemies:
                        if (unit.TeamIndex == teamIndex) continue;
                        break;
                    case SynergyTarget.PrepTarget:
                        if (unit.TeamIndex != teamIndex) continue;
                        if (unit.SourceEntityId != prepTargetEntityId) continue;
                        break;
                }

                ApplyStatEffect(state, i, ref unit, ref effect);
            }
        }

        private static void ApplyStatEffect(CombatMatchState state, int unitIndex,
            ref CombatUnit unit, ref SynergyEffect effect)
        {
            int before;
            switch (effect.Type)
            {
                case SynergyEffectType.BonusDef:
                    before = unit.Def; unit.Def += effect.Value;
                    LogStat(ref unit, "Def", before, unit.Def);
                    break;
                case SynergyEffectType.BonusAdReduce:
                    before = unit.AdReduce; unit.AdReduce += effect.Value;
                    LogStat(ref unit, "AdReduce", before, unit.AdReduce);
                    break;
                case SynergyEffectType.BonusApReduce:
                    before = unit.ApReduce; unit.ApReduce += effect.Value;
                    LogStat(ref unit, "ApReduce", before, unit.ApReduce);
                    break;
                case SynergyEffectType.BonusAttack:
                    before = unit.Attack; unit.Attack += effect.Value;
                    LogStat(ref unit, "Attack", before, unit.Attack);
                    break;
                case SynergyEffectType.BonusAttackPercent:
                    before = unit.Attack; unit.Attack += unit.BaseAttack * effect.ValuePercent / 100;
                    LogStat(ref unit, $"Attack(+{effect.ValuePercent}%)", before, unit.Attack);
                    break;
                case SynergyEffectType.BonusHP:
                    before = unit.MaxHP; unit.MaxHP += effect.Value; unit.CurrentHP += effect.Value;
                    LogStat(ref unit, "HP", before, unit.MaxHP);
                    break;
                case SynergyEffectType.BonusHPPercent:
                    before = unit.MaxHP;
                    int hpBonus = unit.BaseMaxHP * effect.ValuePercent / 100;
                    unit.MaxHP += hpBonus; unit.CurrentHP += hpBonus;
                    LogStat(ref unit, $"HP(+{effect.ValuePercent}%)", before, unit.MaxHP);
                    break;
                case SynergyEffectType.BonusAttackSpeed:
                    before = unit.AttackSpeed; unit.AttackSpeed += effect.Value;
                    LogStat(ref unit, "AtkSpd", before, unit.AttackSpeed);
                    break;
                case SynergyEffectType.BonusAttackSpeedPercent:
                    before = unit.AttackSpeed; unit.AttackSpeed += unit.BaseAttackSpeed * effect.ValuePercent / 100;
                    LogStat(ref unit, $"AtkSpd(+{effect.ValuePercent}%)", before, unit.AttackSpeed);
                    break;
                case SynergyEffectType.BonusDefPercent:
                    before = unit.Def; unit.Def += unit.BaseDef * effect.ValuePercent / 100;
                    LogStat(ref unit, $"Def(+{effect.ValuePercent}%)", before, unit.Def);
                    break;
                case SynergyEffectType.BonusAdReducePercent:
                    before = unit.AdReduce; unit.AdReduce += unit.BaseAdReduce * effect.ValuePercent / 100;
                    LogStat(ref unit, $"AdReduce(+{effect.ValuePercent}%)", before, unit.AdReduce);
                    break;
                case SynergyEffectType.BonusApReducePercent:
                    before = unit.ApReduce; unit.ApReduce += unit.BaseApReduce * effect.ValuePercent / 100;
                    LogStat(ref unit, $"ApReduce(+{effect.ValuePercent}%)", before, unit.ApReduce);
                    break;
                case SynergyEffectType.BonusMana:
                    before = unit.MaxMana; unit.MaxMana += effect.Value;
                    LogStat(ref unit, "Mana", before, unit.MaxMana);
                    break;
                case SynergyEffectType.BonusCritChance:
                    before = unit.CritRate; unit.CritRate += effect.Value;
                    LogStat(ref unit, "CritRate", before, unit.CritRate);
                    break;
                case SynergyEffectType.BonusCritMultiplier:
                    before = unit.CritPower; unit.CritPower += effect.Value;
                    LogStat(ref unit, "CritPower", before, unit.CritPower);
                    break;
                case SynergyEffectType.StartingMana:
                    before = unit.CurrentMana; unit.CurrentMana += effect.Value;
                    if (unit.CurrentMana > unit.MaxMana) unit.CurrentMana = unit.MaxMana;
                    LogStat(ref unit, "StartMana", before, unit.CurrentMana);
                    break;
                case SynergyEffectType.SpellDamagePercent:
                    break;
                case SynergyEffectType.LifeSteal:
                    before = unit.LifeSteal; unit.LifeSteal += effect.Value;
                    LogStat(ref unit, "LifeSteal", before, unit.LifeSteal);
                    break;
                case SynergyEffectType.DodgeChance:
                    before = unit.DodgeChance; unit.DodgeChance += effect.Value;
                    LogStat(ref unit, "Dodge", before, unit.DodgeChance);
                    break;
                case SynergyEffectType.BonusMoveSpeedPercent:
                    before = unit.MoveSpeed; unit.MoveSpeed += unit.MoveSpeed * effect.ValuePercent / 100;
                    LogStat(ref unit, $"MoveSpd(+{effect.ValuePercent}%)", before, unit.MoveSpeed);
                    break;
                case SynergyEffectType.BonusPiercePercent:
                    int atkPBefore = unit.AtkPierce; int resPBefore = unit.ResPierce;
                    unit.AtkPierce += effect.ValuePercent;
                    unit.ResPierce += effect.ValuePercent;
                    LogStat(ref unit, $"AtkPierce(+{effect.ValuePercent}%)", atkPBefore, unit.AtkPierce);
                    LogStat(ref unit, $"ResPierce(+{effect.ValuePercent}%)", resPBefore, unit.ResPierce);
                    break;
                case SynergyEffectType.BacklineJump:
                    unit.HasBacklineJump = true;
                    LogStat(ref unit, "BacklineJump", 0, 1);
                    break;
                case SynergyEffectType.ShieldOnCombatStart:
                    int shieldAmt = unit.MaxHP * effect.ValuePercent / 100;
                    StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.Shield, shieldAmt, -1);
                    LogStat(ref unit, "Shield", 0, shieldAmt);
                    break;
                case SynergyEffectType.ReduceDef:
                    before = unit.Def; unit.Def -= unit.Def * effect.ValuePercent / 100;
                    if (unit.Def < 0) unit.Def = 0;
                    LogStat(ref unit, $"Def(-{effect.ValuePercent}%)", before, unit.Def);
                    break;
                case SynergyEffectType.ReduceAdReduce:
                    before = unit.AdReduce; unit.AdReduce -= unit.AdReduce * effect.ValuePercent / 100;
                    if (unit.AdReduce < 0) unit.AdReduce = 0;
                    LogStat(ref unit, $"AdReduce(-{effect.ValuePercent}%)", before, unit.AdReduce);
                    break;
                case SynergyEffectType.ReduceApReduce:
                    before = unit.ApReduce; unit.ApReduce -= unit.ApReduce * effect.ValuePercent / 100;
                    if (unit.ApReduce < 0) unit.ApReduce = 0;
                    LogStat(ref unit, $"ApReduce(-{effect.ValuePercent}%)", before, unit.ApReduce);
                    break;
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void LogStat(ref CombatUnit unit, string stat, int before, int after)
        {
            if (before == after) return;
            var charName = GetCharacterName(unit.ChampionSpecId);
            UnityEngine.Debug.Log(
                $"<color=cyan>[Synergy] {charName}(id:{unit.ChampionSpecId}) {stat}: {before} → {after}</color>");
        }

        private static string GetCharacterName(int championSpecId)
        {
#if UNITY_EDITOR
            var info = CookApps.AutoBattler.SpecDataManager.Instance?.GetCharacterData(championSpecId);
            if (info != null && !string.IsNullOrEmpty(info.name_token))
                return info.name_token;
#endif
            return $"#{championSpecId}";
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void LogPrepSync(string action, SynergyType type, byte playerIndex,
            byte oldTier, byte newTier, byte unitCount)
        {
            UnityEngine.Debug.Log(
                $"<color=yellow>[SynergyPrep] P{playerIndex} {type} {action} " +
                $"| tier: {oldTier} → {newTier} | units: {unitCount}</color>");
        }

        // ── 스냅샷 캡처/복원 ──

        /// <summary>현재 활성 PrepBehavior 상태를 스냅샷 배열로 추출</summary>
        public static PrepBehaviorSnapshot[] CapturePrepSnapshots(GameWorld world, byte playerIndex)
        {
            int count = world.PrepBehaviorCounts[playerIndex];
            if (count == 0) return System.Array.Empty<PrepBehaviorSnapshot>();

            var snapshots = new PrepBehaviorSnapshot[count];
            for (int i = 0; i < count; i++)
                snapshots[i] = world.PrepBehaviors[playerIndex][i].CaptureSnapshot();
            return snapshots;
        }

        private static PrepBehaviorSnapshot? FindSnapshotByTraitId(PrepBehaviorSnapshot[] snapshots, int traitId)
        {
            if (snapshots == null) return null;
            for (int i = 0; i < snapshots.Length; i++)
            {
                if (snapshots[i].TraitId == traitId)
                    return snapshots[i];
            }
            return null;
        }

        /// <summary>복원된 PrepBehavior에 대해 View 동기화 이벤트 발행</summary>
        private static void EmitRestoreEvents(GameWorld world, byte playerIndex, SynergyPrepBehaviorBase b, PrepBehaviorSnapshot snap)
        {
            if (b is not SynergyPrepSupernova sn) return;

            if (sn.PrepTargetEntityId >= 0)
            {
                // 타겟 부여 상태 — 유닛이 아직 보드에 있는지 검증
                if (IsEntityOnBoard(world, playerIndex, sn.PrepTargetEntityId))
                {
                    world.EventQueue.PushSupernovaObjectEvent(
                        playerIndex, b.TraitId, SupernovaSubType.TargetAssigned,
                        0, 0, sn.PrepTargetEntityId);
                    world.EventQueue.PushSynergyUpdated(playerIndex);
                }
                else
                {
                    // 유닛이 사라짐 → 타겟 초기화 + 새로 배치
                    sn.PrepTargetEntityId = -1;
                    sn.ObjectCol = -1;
                    sn.ObjectRow = -1;
                    b.OnActivate(world); // 정상 활성화 (랜덤 배치)
                }
            }
            else if (sn.ObjectCol >= 0)
            {
                // 미부여 상태 — 이전 오브젝트 위치가 비어있으면 복원
                int slot = sn.ObjectRow * world.BoardWidth + sn.ObjectCol;
                if (slot >= 0 && slot < world.BoardSize
                    && world.BoardSlots[playerIndex][slot] == UnitData.InvalidId)
                {
                    world.EventQueue.PushSupernovaObjectEvent(
                        playerIndex, b.TraitId, SupernovaSubType.Spawn,
                        (byte)sn.ObjectCol, (byte)sn.ObjectRow);
                }
                else
                {
                    // 위치가 점유됨 → 새로 배치
                    sn.ObjectCol = -1;
                    sn.ObjectRow = -1;
                    b.OnActivate(world);
                }
            }
            else
            {
                // 스냅샷에 위치/타겟 없음 → 정상 활성화
                b.OnActivate(world);
            }
        }

        private static bool IsEntityOnBoard(GameWorld world, byte playerIndex, int entityId)
        {
            var boardSlots = world.BoardSlots[playerIndex];
            for (int slot = 0; slot < world.BoardSize; slot++)
            {
                if (boardSlots[slot] == entityId)
                    return true;
            }
            return false;
        }

        // ── 준비 페이즈 시너지 행동 동기화 ──

        /// <summary>
        /// 보드 변경 시 호출. 시너지 티어 diff를 계산하여 prep behavior 생성/소멸/변경.
        /// Recalculate() 이후에 호출해야 함.
        /// prevSnapshots가 주어지면, 동일 traitId의 이전 스냅샷에서 상태를 복원 (스테이지 전환용).
        /// </summary>
        public static void SyncPrepBehaviors(GameWorld world, byte playerIndex, PrepBehaviorSnapshot[] prevSnapshots = null)
        {
            if (!world.Config.EnableSynergy) return;
            if (world.SynergySpecs == null || world.SynergySpecCount == 0) return;

            var synergy = world.Synergies[playerIndex];
            var prevTiers = world.PrevSynergyTiers[playerIndex];

            for (int t = 0; t < world.SynergySpecCount; t++)
            {
                ref var spec = ref world.SynergySpecs[t];
                if (!spec.IsValid || !spec.HasBehavior) continue;

                int traitId = spec.TraitId;
                byte oldTier = prevTiers[traitId];
                byte newTier = synergy.GetTraitTier(traitId);

                if (oldTier == 0 && newTier > 0)
                {
                    // 활성화
                    LogPrepSync("ACTIVATED", (SynergyType)traitId, playerIndex, oldTier, newTier,
                        synergy.GetTraitCount(traitId));
                    var b = SynergyFactory.CreatePrep(
                        (SynergyType)traitId, newTier, traitId, playerIndex);
                    if (b != null)
                    {
                        AddPrepBehavior(world, playerIndex, b);

                        // 이전 스냅샷에서 같은 traitId가 있으면 상태 복원 (OnActivate 건너뜀)
                        var prevSnap = FindSnapshotByTraitId(prevSnapshots, traitId);
                        if (prevSnap != null)
                        {
                            b.RestoreFromSnapshot(prevSnap.Value);
                            EmitRestoreEvents(world, playerIndex, b, prevSnap.Value);
                        }
                        else
                        {
                            b.OnActivate(world); // 완전 새로운 활성화
                        }
                    }
                }
                else if (oldTier > 0 && newTier == 0)
                {
                    // 비활성화
                    LogPrepSync("DEACTIVATED", (SynergyType)traitId, playerIndex, oldTier, newTier,
                        synergy.GetTraitCount(traitId));
                    int idx = FindPrepBehavior(world, playerIndex, traitId);
                    if (idx >= 0)
                    {
                        world.PrepBehaviors[playerIndex][idx].OnDeactivate(world);
                        RemovePrepBehaviorAt(world, playerIndex, idx);
                    }
                }
                else if (oldTier != newTier && newTier > 0)
                {
                    // 티어 변경
                    LogPrepSync("TIER_CHANGED", (SynergyType)traitId, playerIndex, oldTier, newTier,
                        synergy.GetTraitCount(traitId));
                    int idx = FindPrepBehavior(world, playerIndex, traitId);
                    if (idx >= 0)
                    {
                        var b = world.PrepBehaviors[playerIndex][idx];
                        b.Tier = newTier;
                        b.OnTierChanged(world, oldTier, newTier);
                    }
                }

                prevTiers[traitId] = newTier;
            }

            // 모든 활성 행동에 보드 변경 알림
            for (int i = 0; i < world.PrepBehaviorCounts[playerIndex]; i++)
                world.PrepBehaviors[playerIndex][i].OnBoardChanged(world);
        }

        /// <summary>플레이어의 모든 prep behavior 해제 및 prev tier 초기화</summary>
        public static void ClearPrepBehaviors(GameWorld world, byte playerIndex)
        {
            for (int i = 0; i < world.PrepBehaviorCounts[playerIndex]; i++)
            {
                world.PrepBehaviors[playerIndex][i].OnDeactivate(world);
                world.PrepBehaviors[playerIndex][i] = null;
            }
            world.PrepBehaviorCounts[playerIndex] = 0;
            System.Array.Clear(world.PrevSynergyTiers[playerIndex], 0, PlayerSynergy.MaxTraits);
        }

        // ── Prep Behavior 배열 헬퍼 ──

        private static void AddPrepBehavior(GameWorld world, byte playerIndex, SynergyPrepBehaviorBase b)
        {
            int count = world.PrepBehaviorCounts[playerIndex];
            if (count >= GameWorld.MaxPrepBehaviors) return;
            world.PrepBehaviors[playerIndex][count] = b;
            world.PrepBehaviorCounts[playerIndex] = count + 1;
        }

        public static int FindPrepBehavior(GameWorld world, byte playerIndex, int traitId)
        {
            int count = world.PrepBehaviorCounts[playerIndex];
            for (int i = 0; i < count; i++)
            {
                if (world.PrepBehaviors[playerIndex][i].TraitId == traitId)
                    return i;
            }
            return -1;
        }

        private static void RemovePrepBehaviorAt(GameWorld world, byte playerIndex, int index)
        {
            int last = world.PrepBehaviorCounts[playerIndex] - 1;
            if (index < last)
                world.PrepBehaviors[playerIndex][index] = world.PrepBehaviors[playerIndex][last];
            world.PrepBehaviors[playerIndex][last] = null;
            world.PrepBehaviorCounts[playerIndex] = last;
        }

        /// <summary>보드 내 유닛 이동 시 PrepBehavior에만 알림 (시너지 재계산 불필요)</summary>
        public static void NotifyPrepBoardChanged(GameWorld world, byte playerIndex)
        {
            for (int i = 0; i < world.PrepBehaviorCounts[playerIndex]; i++)
                world.PrepBehaviors[playerIndex][i].OnBoardChanged(world);
        }

        // ── 전투 시작 시 시너지 행동 등록 (asterism) ──

        /// <summary>
        /// HasBehavior인 시너지의 CombatTraitBase를 생성하여 대상 유닛에 TraitSystem으로 부착.
        /// ApplyEffects() 이후 호출.
        /// </summary>
        public static void ApplyBehaviors(GameWorld world, CombatMatchState state,
            byte playerIndex, byte teamIndex)
        {
            if (!world.Config.EnableSynergy) return;
            if (world.SynergySpecs == null) return;

            var synergy = world.Synergies[playerIndex];

            for (int t = 0; t < world.SynergySpecCount; t++)
            {
                ref var spec = ref world.SynergySpecs[t];
                if (!spec.IsValid || !spec.HasBehavior) continue;

                int traitId = spec.TraitId;
                byte tier = synergy.GetTraitTier(traitId);
                if (tier == 0) continue;

                // prep 데이터 가져오기
                int prepTargetEntityId = -1;
                int prepParam0 = 0, prepParam1 = 0;
                int prepIdx = FindPrepBehavior(world, playerIndex, traitId);
                if (prepIdx >= 0)
                {
                    var prep = world.PrepBehaviors[playerIndex][prepIdx];
                    prepTargetEntityId = prep.PrepTargetEntityId;
                    prepParam0 = prep.PrepParam0;
                    prepParam1 = prep.PrepParam1;
                }

                // 대상 유닛에 trait 부착
                for (int u = 0; u < state.UnitCount; u++)
                {
                    ref var unit = ref state.Units[u];
                    if (unit.TeamIndex != teamIndex || !unit.IsAlive) continue;

                    // prepTargetEntityId가 지정되면 해당 유닛에만, 아니면 팀 전체에
                    if (prepTargetEntityId >= 0 && unit.SourceEntityId != prepTargetEntityId) continue;

                    var trait = SynergyFactory.CreateTrait((SynergyType)traitId, tier);
                    if (trait == null) continue;

                    trait.SynergyTraitId = traitId;
                    trait.PrepTargetEntityId = prepTargetEntityId;
                    trait.PrepParam0 = prepParam0;
                    trait.PrepParam1 = prepParam1;

                    TraitSystem.AddTrait(state, u, trait);
                }
            }
        }
        // ── DeckAdditionalData 연동 (저장/로드) ──

        /// <summary>현재 슈퍼노바 타겟의 ChampionSpecId 반환 (덱 저장용). 미부여 시 0.</summary>
        public static int GetSupernovaTargetSpecId(GameWorld world, byte playerIndex)
        {
            int prepIdx = FindPrepBehavior(world, playerIndex, (int)SynergyType.SUPERNOVA);
            if (prepIdx < 0) return 0;

            var prep = world.PrepBehaviors[playerIndex][prepIdx];
            if (prep.PrepTargetEntityId < 0) return 0;

            int unitIdx = world.FindUnitIndex(prep.PrepTargetEntityId);
            if (unitIdx < 0) return 0;

            return world.Units[unitIdx].ChampionSpecId;
        }

        /// <summary>저장된 슈퍼노바 타겟 specId로 PrepTarget 복원 (재접속용)</summary>
        public static void RestoreSupernovaTarget(GameWorld world, byte playerIndex, int supernovaCharacterId)
        {
            if (supernovaCharacterId == 0) return;

            int prepIdx = FindPrepBehavior(world, playerIndex, (int)SynergyType.SUPERNOVA);
            if (prepIdx < 0) return;

            var prep = world.PrepBehaviors[playerIndex][prepIdx];

            // specId로 보드 위 유닛 찾기
            var boardSlots = world.BoardSlots[playerIndex];
            for (int slot = 0; slot < world.BoardSize; slot++)
            {
                int entityId = boardSlots[slot];
                if (entityId == UnitData.InvalidId) continue;

                int unitIdx = world.FindUnitIndex(entityId);
                if (unitIdx < 0) continue;

                if (world.Units[unitIdx].ChampionSpecId != supernovaCharacterId) continue;

                // TraitFlag 검증
                int traitBit = 1 << prep.TraitId;
                if ((world.Units[unitIdx].TraitFlags & traitBit) == 0) continue;

                prep.PrepTargetEntityId = entityId;

                // 오브젝트 제거 (타겟 부여됨)
                if (prep is SynergyPrepSupernova sn && sn.ObjectCol >= 0)
                {
                    world.EventQueue.PushSupernovaObjectEvent(
                        playerIndex, prep.TraitId, SupernovaSubType.Remove,
                        (byte)sn.ObjectCol, (byte)sn.ObjectRow);
                    sn.ObjectCol = -1;
                    sn.ObjectRow = -1;
                }

                world.EventQueue.PushSupernovaObjectEvent(
                    playerIndex, prep.TraitId, SupernovaSubType.TargetAssigned,
                    0, 0, entityId);
                world.EventQueue.PushSynergyUpdated(playerIndex);
                break;
            }
        }

        // ── 프리뷰용 시너지 HP 보너스 ──

        /// <summary>보드 유닛에 적용될 시너지 HP 보너스 합산</summary>
        public static int CalcSynergyBonusHP(GameWorld world, byte playerIndex, ref UnitData unit, int entityId = -1)
        {
            if (!world.Config.EnableSynergy || world.SynergySpecs == null || world.SynergySpecCount == 0)
                return 0;

            int bonus = 0;
            var synergy = world.Synergies[playerIndex];

            for (int t = 0; t < world.SynergySpecCount; t++)
            {
                ref var spec = ref world.SynergySpecs[t];
                if (!spec.IsValid) continue;

                byte tier = synergy.GetTraitTier(spec.TraitId);
                if (tier == 0) continue;

                int tierIndex = tier - 1;
                if (tierIndex >= spec.Tiers.Length) continue;

                ref var tierData = ref spec.Tiers[tierIndex];
                if (tierData.Effects == null) continue;

                for (int e = 0; e < tierData.Effects.Length; e++)
                {
                    ref var effect = ref tierData.Effects[e];

                    switch (effect.Target)
                    {
                        case SynergyTarget.TraitUnits:
                            if ((unit.TraitFlags & (1 << spec.TraitId)) == 0) continue;
                            break;
                        case SynergyTarget.AllAllies:
                            break;
                        case SynergyTarget.PrepTarget:
                            // entityId가 주어지고 이 유닛이 PrepTarget이면 적용
                            if (entityId < 0) continue;
                            int prepIdx = FindPrepBehavior(world, playerIndex, spec.TraitId);
                            if (prepIdx < 0 || world.PrepBehaviors[playerIndex][prepIdx].PrepTargetEntityId != entityId)
                                continue;
                            break;
                        default:
                            continue;
                    }

                    switch (effect.Type)
                    {
                        case SynergyEffectType.BonusHP:
                            bonus += effect.Value;
                            break;
                        case SynergyEffectType.BonusHPPercent:
                            bonus += unit.MaxHP * effect.ValuePercent / 100;
                            break;
                    }
                }
            }

            return bonus;
        }
    }
}
