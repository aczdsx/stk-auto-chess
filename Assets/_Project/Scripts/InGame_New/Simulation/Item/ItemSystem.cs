namespace CookApps.AutoChess
{
    /// <summary>
    /// 아이템 시스템. 장착/해제/자동 조합/스탯 적용.
    /// 기본 아이템 2개 → 합성 아이템 자동 조합.
    /// </summary>
    public static class ItemSystem
    {
        // ── 아이템 장착 ──

        /// <summary>인벤토리 아이템을 유닛에 장착</summary>
        public static bool TryEquip(GameWorld world, byte playerIndex, int itemInstanceId, int targetEntityId)
        {
            if (!world.Config.EnableItems) return false;

            int itemIdx = FindItemIndex(world, itemInstanceId);
            if (itemIdx < 0) return false;

            ref var item = ref world.Items[itemIdx];
            if (item.OwnerIndex != playerIndex) return false;
            if (item.Location != ItemLocation.Inventory) return false;

            int unitIdx = world.FindUnitIndex(targetEntityId);
            if (unitIdx < 0) return false;

            ref var unit = ref world.Units[unitIdx];
            if (unit.OwnerIndex != playerIndex) return false;

            // 빈 슬롯 찾기
            int slot = unit.GetEmptyItemSlot();
            if (slot < 0) return false; // 슬롯 가득 참 (3개)

            // 유니크 체크
            if (world.ItemSpecs != null)
            {
                int specIdx = FindItemSpecIndex(world, item.ItemSpecId);
                if (specIdx >= 0 && world.ItemSpecs[specIdx].IsUnique)
                {
                    if (HasEquippedItem(world, ref unit, item.ItemSpecId))
                        return false;
                }
            }

            // 인벤토리에서 제거
            RemoveFromInventory(world, playerIndex, itemInstanceId);

            // 유닛에 장착
            item.Location = ItemLocation.Equipped;
            item.EquippedEntityId = targetEntityId;
            item.SlotIndex = (byte)slot;
            unit.SetItemSlot(slot, itemInstanceId);

            // 자동 조합 체크
            TryAutoCombine(world, playerIndex, ref unit, unitIdx);

            return true;
        }

        /// <summary>유닛에서 아이템 해제 (Preparation 페이즈만)</summary>
        public static bool TryUnequip(GameWorld world, byte playerIndex, int itemInstanceId)
        {
            if (!world.Config.EnableItems) return false;

            int itemIdx = FindItemIndex(world, itemInstanceId);
            if (itemIdx < 0) return false;

            ref var item = ref world.Items[itemIdx];
            if (item.OwnerIndex != playerIndex) return false;
            if (item.Location != ItemLocation.Equipped) return false;

            // 인벤토리 공간 체크
            if (world.ItemInventoryCount[playerIndex] >= GameWorld.MaxItemInventory) return false;

            int unitIdx = world.FindUnitIndex(item.EquippedEntityId);
            if (unitIdx >= 0)
            {
                ref var unit = ref world.Units[unitIdx];
                unit.SetItemSlot(item.SlotIndex, UnitData.InvalidId);
            }

            // 인벤토리로 이동
            item.Location = ItemLocation.Inventory;
            item.EquippedEntityId = ItemData.InvalidId;
            AddToInventory(world, playerIndex, itemInstanceId);

            return true;
        }

        // ── 자동 조합 ──

        /// <summary>기본 아이템 2개가 장착되면 합성 아이템으로 조합</summary>
        private static void TryAutoCombine(GameWorld world, byte playerIndex, ref UnitData unit, int unitIdx)
        {
            if (world.ItemSpecs == null) return;

            // 기본 아이템 2개 찾기
            int baseSlot1 = -1, baseSlot2 = -1;
            int baseSpecId1 = 0, baseSpecId2 = 0;

            for (int s = 0; s < UnitData.MaxItemSlots; s++)
            {
                int instId = unit.GetItemSlot(s);
                if (instId == UnitData.InvalidId) continue;

                int idx = FindItemIndex(world, instId);
                if (idx < 0) continue;

                int specIdx = FindItemSpecIndex(world, world.Items[idx].ItemSpecId);
                if (specIdx < 0) continue;

                if (!world.ItemSpecs[specIdx].IsBaseItem) continue;

                if (baseSlot1 < 0)
                {
                    baseSlot1 = s;
                    baseSpecId1 = world.Items[idx].ItemSpecId;
                }
                else if (baseSlot2 < 0)
                {
                    baseSlot2 = s;
                    baseSpecId2 = world.Items[idx].ItemSpecId;
                    break;
                }
            }

            if (baseSlot1 < 0 || baseSlot2 < 0) return;

            // 레시피 조회
            int resultSpecId = LookupRecipe(world, baseSpecId1, baseSpecId2);
            if (resultSpecId <= 0) return;

            // 기본 아이템 2개 제거
            int instId1 = unit.GetItemSlot(baseSlot1);
            int instId2 = unit.GetItemSlot(baseSlot2);

            DestroyItem(world, playerIndex, instId1);
            DestroyItem(world, playerIndex, instId2);

            unit.SetItemSlot(baseSlot1, UnitData.InvalidId);
            unit.SetItemSlot(baseSlot2, UnitData.InvalidId);

            // 합성 아이템 생성 + 장착
            int newInstId = CreateItemInstance(world, playerIndex, resultSpecId);
            if (newInstId == ItemData.InvalidId) return;

            int newIdx = FindItemIndex(world, newInstId);
            if (newIdx < 0) return;

            ref var newItem = ref world.Items[newIdx];
            newItem.Location = ItemLocation.Equipped;
            newItem.EquippedEntityId = unit.EntityId;
            newItem.SlotIndex = (byte)baseSlot1;
            unit.SetItemSlot(baseSlot1, newInstId);
        }

        /// <summary>레시피 조회: 기본아이템 2개 → 합성 아이템 ID</summary>
        private static int LookupRecipe(GameWorld world, int baseItem1, int baseItem2)
        {
            if (world.ItemSpecs == null) return 0;

            for (int i = 0; i < world.ItemSpecCount; i++)
            {
                ref var spec = ref world.ItemSpecs[i];
                if (spec.IsBaseItem) continue;

                // 양방향 매칭
                if ((spec.RecipeItem1 == baseItem1 && spec.RecipeItem2 == baseItem2) ||
                    (spec.RecipeItem1 == baseItem2 && spec.RecipeItem2 == baseItem1))
                {
                    return spec.ItemId;
                }
            }

            return 0; // 레시피 없음
        }

        // ── 전투 유닛에 아이템 스탯 적용 ──

        /// <summary>
        /// CombatUnit에 장착된 아이템의 스탯 보너스 적용.
        /// CombatSetupSystem에서 유닛 복제 후 호출.
        /// 순서: 기본 스탯 → 아이템 → 시너지
        /// </summary>
        public static void ApplyItemStats(GameWorld world, ref CombatUnit combatUnit, ref UnitData sourceUnit)
        {
            if (!world.Config.EnableItems) return;
            if (world.ItemSpecs == null) return;

            for (int s = 0; s < UnitData.MaxItemSlots; s++)
            {
                int instId = sourceUnit.GetItemSlot(s);
                if (instId == UnitData.InvalidId) continue;

                int itemIdx = FindItemIndex(world, instId);
                if (itemIdx < 0) continue;

                int specIdx = FindItemSpecIndex(world, world.Items[itemIdx].ItemSpecId);
                if (specIdx < 0) continue;

                ref var spec = ref world.ItemSpecs[specIdx];
                ApplySpecStats(ref combatUnit, ref spec);
            }
        }

        /// <summary>아이템 스펙의 스탯을 CombatUnit에 적용</summary>
        private static void ApplySpecStats(ref CombatUnit unit, ref ItemSpec spec)
        {
            unit.Attack += spec.BonusAttack;
            unit.Def += spec.BonusDef;
            unit.AdReduce += spec.BonusAdReduce;
            unit.ApReduce += spec.BonusApReduce;

            if (spec.BonusHP > 0)
            {
                unit.MaxHP += spec.BonusHP;
                unit.CurrentHP += spec.BonusHP;
            }

            if (spec.BonusAttackSpeedPercent > 0)
                unit.AttackSpeed += unit.AttackSpeed * spec.BonusAttackSpeedPercent / 100;

            if (spec.BonusMana > 0)
            {
                unit.CurrentMana += spec.BonusMana;
                if (unit.CurrentMana > unit.MaxMana)
                    unit.CurrentMana = unit.MaxMana;
            }

            if (spec.BonusCritChance > 0)
                unit.CritRate += spec.BonusCritChance;
        }

        /// <summary>
        /// CombatUnit에 아이템 특수 효과 등록.
        /// CombatItemEffects 구조체에 누적.
        /// </summary>
        public static CombatItemEffects BuildItemEffects(GameWorld world, ref UnitData sourceUnit)
        {
            var fx = new CombatItemEffects();
            if (!world.Config.EnableItems || world.ItemSpecs == null) return fx;

            for (int s = 0; s < UnitData.MaxItemSlots; s++)
            {
                int instId = sourceUnit.GetItemSlot(s);
                if (instId == UnitData.InvalidId) continue;

                int itemIdx = FindItemIndex(world, instId);
                if (itemIdx < 0) continue;

                int specIdx = FindItemSpecIndex(world, world.Items[itemIdx].ItemSpecId);
                if (specIdx < 0) continue;

                ref var spec = ref world.ItemSpecs[specIdx];
                if (spec.SpecialEffect == ItemEffectType.None) continue;

                switch (spec.SpecialEffect)
                {
                    case ItemEffectType.LifeSteal:
                        fx.LifeStealPercent += spec.EffectValue1;
                        break;
                    case ItemEffectType.SpellVamp:
                        fx.SpellVampPercent += spec.EffectValue1;
                        break;
                    case ItemEffectType.ReflectDamage:
                        fx.ReflectDamagePercent += spec.EffectValue1;
                        break;
                    case ItemEffectType.OnHitMagicDamage:
                        fx.OnHitMagicDamage += spec.EffectValue1;
                        break;
                    case ItemEffectType.BurnOnHit:
                        fx.BurnDamagePerTick += spec.EffectValue1;
                        break;
                    case ItemEffectType.AntiHeal:
                        fx.AntiHealPercent += spec.EffectValue1;
                        break;
                    case ItemEffectType.DodgeChance:
                        fx.DodgeChanceBonus += spec.EffectValue1;
                        break;
                    case ItemEffectType.Cleave:
                        fx.CleavePercent += spec.EffectValue1;
                        break;
                    case ItemEffectType.ExtraAttack:
                        fx.ExtraAttackInterval = spec.EffectValue1;
                        break;
                    case ItemEffectType.CCImmunity:
                        fx.HasCCImmunity = true;
                        break;
                    case ItemEffectType.ShieldOnLowHP:
                        fx.ShieldThresholdPercent = spec.EffectValue1;
                        fx.ShieldBonusAmount = spec.EffectValue2;
                        break;
                    case ItemEffectType.ManaRefund:
                        // TODO: 스킬 시스템 연동 시 처리
                        break;
                }
            }

            return fx;
        }

        // ── 아이템 인스턴스 관리 ──

        /// <summary>새 아이템 인스턴스 생성</summary>
        public static int CreateItemInstance(GameWorld world, byte playerIndex, int itemSpecId)
        {
            int slot = FindEmptyItemSlot(world);
            if (slot < 0) return ItemData.InvalidId;

            int instId = world.NextItemInstanceId++;
            ref var item = ref world.Items[slot];
            item.ItemInstanceId = instId;
            item.ItemSpecId = itemSpecId;
            item.OwnerIndex = playerIndex;
            item.Location = ItemLocation.Inventory;
            item.EquippedEntityId = ItemData.InvalidId;

            return instId;
        }

        /// <summary>아이템을 인벤토리에 추가</summary>
        public static bool AddToInventory(GameWorld world, byte playerIndex, int itemInstanceId)
        {
            if (world.ItemInventoryCount[playerIndex] >= GameWorld.MaxItemInventory) return false;

            int idx = world.ItemInventoryCount[playerIndex];
            world.ItemInventory[playerIndex][idx] = itemInstanceId;
            world.ItemInventoryCount[playerIndex]++;
            return true;
        }

        /// <summary>아이템 획득 (생성 + 인벤토리 추가)</summary>
        public static int AcquireItem(GameWorld world, byte playerIndex, int itemSpecId)
        {
            int instId = CreateItemInstance(world, playerIndex, itemSpecId);
            if (instId == ItemData.InvalidId) return ItemData.InvalidId;

            if (!AddToInventory(world, playerIndex, instId))
            {
                // 인벤토리 가득 참 - 가장 오래된 것 교체
                if (world.ItemInventoryCount[playerIndex] > 0)
                {
                    // 가장 오래된(0번) 아이템 제거
                    int oldInstId = world.ItemInventory[playerIndex][0];
                    DestroyItem(world, playerIndex, oldInstId);

                    // 시프트
                    for (int i = 0; i < world.ItemInventoryCount[playerIndex] - 1; i++)
                        world.ItemInventory[playerIndex][i] = world.ItemInventory[playerIndex][i + 1];
                    world.ItemInventoryCount[playerIndex]--;

                    AddToInventory(world, playerIndex, instId);
                }
            }

            return instId;
        }

        // ── 유틸리티 ──

        private static void RemoveFromInventory(GameWorld world, byte playerIndex, int itemInstanceId)
        {
            int count = world.ItemInventoryCount[playerIndex];
            for (int i = 0; i < count; i++)
            {
                if (world.ItemInventory[playerIndex][i] == itemInstanceId)
                {
                    // 시프트
                    for (int j = i; j < count - 1; j++)
                        world.ItemInventory[playerIndex][j] = world.ItemInventory[playerIndex][j + 1];
                    world.ItemInventory[playerIndex][count - 1] = ItemData.InvalidId;
                    world.ItemInventoryCount[playerIndex]--;
                    return;
                }
            }
        }

        private static void DestroyItem(GameWorld world, byte playerIndex, int itemInstanceId)
        {
            int idx = FindItemIndex(world, itemInstanceId);
            if (idx >= 0)
            {
                world.Items[idx] = ItemData.CreateEmpty();
            }
            RemoveFromInventory(world, playerIndex, itemInstanceId);
        }

        private static bool HasEquippedItem(GameWorld world, ref UnitData unit, int itemSpecId)
        {
            for (int s = 0; s < UnitData.MaxItemSlots; s++)
            {
                int instId = unit.GetItemSlot(s);
                if (instId == UnitData.InvalidId) continue;

                int idx = FindItemIndex(world, instId);
                if (idx >= 0 && world.Items[idx].ItemSpecId == itemSpecId)
                    return true;
            }
            return false;
        }

        public static int FindItemIndex(GameWorld world, int itemInstanceId)
        {
            if (itemInstanceId == ItemData.InvalidId) return -1;
            for (int i = 0; i < GameWorld.MaxItems; i++)
            {
                if (world.Items[i].ItemInstanceId == itemInstanceId)
                    return i;
            }
            return -1;
        }

        public static int FindItemSpecIndex(GameWorld world, int itemSpecId)
        {
            for (int i = 0; i < world.ItemSpecCount; i++)
            {
                if (world.ItemSpecs[i].ItemId == itemSpecId)
                    return i;
            }
            return -1;
        }

        /// <summary>보드 유닛의 장착 아이템으로부터 HP 보너스 합산</summary>
        public static int CalcItemBonusHP(GameWorld world, ref UnitData unit)
        {
            if (!world.Config.EnableItems || world.ItemSpecs == null) return 0;

            int bonus = 0;
            for (int s = 0; s < UnitData.MaxItemSlots; s++)
            {
                int instId = unit.GetItemSlot(s);
                if (instId == UnitData.InvalidId) continue;

                int itemIdx = FindItemIndex(world, instId);
                if (itemIdx < 0) continue;

                int specIdx = FindItemSpecIndex(world, world.Items[itemIdx].ItemSpecId);
                if (specIdx < 0) continue;

                bonus += world.ItemSpecs[specIdx].BonusHP;
            }
            return bonus;
        }

        private static int FindEmptyItemSlot(GameWorld world)
        {
            for (int i = 0; i < GameWorld.MaxItems; i++)
            {
                if (!world.Items[i].IsValid)
                    return i;
            }
            return -1;
        }
    }
}
