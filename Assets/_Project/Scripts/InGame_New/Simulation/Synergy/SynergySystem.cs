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

                for (int e = 0; e < tierData.Effects.Length; e++)
                {
                    ref var effect = ref tierData.Effects[e];
                    ApplySingleEffect(matchState, ref effect, traitId, teamIndex);
                }
            }
        }

        private static void ApplySingleEffect(CombatMatchState state, ref SynergyEffect effect,
            int traitId, byte teamIndex)
        {
            int traitBit = 1 << traitId;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;

                // 대상 필터링
                switch (effect.Target)
                {
                    case SynergyTarget.TraitUnits:
                        if (unit.TeamIndex != teamIndex) continue;
                        // 해당 특성 비트가 있는 유닛만
                        // TraitFlags는 원본 UnitData에서 복사해야 하므로
                        // CombatUnit에 TraitFlags 필드가 필요 → 일단 SourceEntityId로 조회는 비용이 크므로
                        // CombatUnit에도 TraitFlags를 저장하도록 함
                        if ((unit.TraitFlags & traitBit) == 0) continue;
                        break;
                    case SynergyTarget.AllAllies:
                        if (unit.TeamIndex != teamIndex) continue;
                        break;
                    case SynergyTarget.AllEnemies:
                        if (unit.TeamIndex == teamIndex) continue;
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

        // ── 준비 페이즈 시너지 행동 동기화 ──

        /// <summary>
        /// 보드 변경 시 호출. 시너지 티어 diff를 계산하여 prep behavior 생성/소멸/변경.
        /// Recalculate() 이후에 호출해야 함.
        /// </summary>
        public static void SyncPrepBehaviors(GameWorld world, byte playerIndex)
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
                        b.OnActivate(world);
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
    }
}
