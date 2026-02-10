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
            for (int slot = 0; slot < PlayerBoard.BoardSize; slot++)
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
                }
            }
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
            switch (effect.Type)
            {
                case SynergyEffectType.BonusArmor:
                    unit.Armor += effect.Value;
                    break;
                case SynergyEffectType.BonusMagicResist:
                    unit.MagicResist += effect.Value;
                    break;
                case SynergyEffectType.BonusAttack:
                    unit.Attack += effect.Value;
                    break;
                case SynergyEffectType.BonusAttackPercent:
                    unit.Attack += unit.Attack * effect.ValuePercent / 100;
                    break;
                case SynergyEffectType.BonusHP:
                    unit.MaxHP += effect.Value;
                    unit.CurrentHP += effect.Value;
                    break;
                case SynergyEffectType.BonusHPPercent:
                    int hpBonus = unit.MaxHP * effect.ValuePercent / 100;
                    unit.MaxHP += hpBonus;
                    unit.CurrentHP += hpBonus;
                    break;
                case SynergyEffectType.BonusAttackSpeed:
                    unit.AttackSpeed += effect.Value;
                    break;
                case SynergyEffectType.BonusAttackSpeedPercent:
                    unit.AttackSpeed += unit.AttackSpeed * effect.ValuePercent / 100;
                    break;
                case SynergyEffectType.BonusMana:
                    unit.MaxMana += effect.Value;
                    break;
                case SynergyEffectType.BonusCritChance:
                    unit.CritChance += effect.Value;
                    break;
                case SynergyEffectType.BonusCritMultiplier:
                    unit.CritMultiplier += effect.Value;
                    break;
                case SynergyEffectType.StartingMana:
                    unit.CurrentMana += effect.Value;
                    if (unit.CurrentMana > unit.MaxMana)
                        unit.CurrentMana = unit.MaxMana;
                    break;
                case SynergyEffectType.SpellDamagePercent:
                    // TODO: Phase 4 스킬 - 스킬 데미지 배율 적용
                    break;
                case SynergyEffectType.LifeSteal:
                    unit.LifeSteal += effect.Value;
                    break;
                case SynergyEffectType.DodgeChance:
                    unit.DodgeChance += effect.Value;
                    break;
                case SynergyEffectType.BacklineJump:
                    unit.HasBacklineJump = true;
                    break;
                case SynergyEffectType.ShieldOnCombatStart:
                    int shieldAmt = unit.MaxHP * effect.ValuePercent / 100;
                    StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.Shield, shieldAmt, -1);
                    break;
                case SynergyEffectType.DamageReduction:
                    unit.DamageReduction += effect.Value;
                    break;
                case SynergyEffectType.ReduceArmor:
                    // 디버프: 적군 방어력 감소 (퍼센트)
                    unit.Armor -= unit.Armor * effect.ValuePercent / 100;
                    if (unit.Armor < 0) unit.Armor = 0;
                    break;
                case SynergyEffectType.ReduceMagicResist:
                    unit.MagicResist -= unit.MagicResist * effect.ValuePercent / 100;
                    if (unit.MagicResist < 0) unit.MagicResist = 0;
                    break;
            }
        }
    }
}
