# 아이템 시스템 설계

> 기본 아이템, 조합, 장착/해제, 효과, 드롭 로직을 정의한다.
>
> **GameMode 적용**: `EnableItems=true`인 모드에서 활성화.
> - ClassicBattle: 보유 캐릭터에 기장착된 아이템만 사용 (드롭/조합 없음)
> - PvECampaign: PvE 라운드에서 아이템 드롭, 조합 가능
> - Competitive: PvE 드롭 + 공유 드래프트(캐러셀)에서 획득, 조합 가능

---

## 1. 아이템 구조 개요

```
아이템 2단계 구조:

1. 기본 아이템 (Base Item): 8종
   - 크립 라운드, 캐러셀에서 획득
   - 단독으로 유닛에 장착 가능 (약한 효과)

2. 완성 아이템 (Combined Item): 기본 아이템 2개 조합
   - 8×8 = 64개 조합 가능 (대칭 제거 시 36개)
   - 고유하고 강력한 효과

장착 규칙:
  - 유닛당 최대 3개 아이템 장착
  - 같은 완성 아이템 중복 장착 제한 (유니크 아이템)
  - 기본 아이템은 중복 장착 가능
```

---

## 2. 기본 아이템 (8종)

```
| ID | 이름 | 스탯 보너스 |
|----|------|------------|
| 1 | 검 (Sword) | 공격력 +10 |
| 2 | 활 (Bow) | 공격속도 +15% |
| 3 | 지팡이 (Rod) | 스킬 위력 +15% |
| 4 | 눈물 (Tear) | 시작 마나 +15 |
| 5 | 갑옷 (Vest) | 방어력 +20 |
| 6 | 망토 (Cloak) | 마법저항 +20 |
| 7 | 벨트 (Belt) | HP +200 |
| 8 | 장갑 (Gloves) | 크리티컬 확률 +20% |

기본 아이템도 장착 시 해당 스탯 보너스 적용.
조합하면 기본 스탯 + 완성 아이템 고유 효과.
```

---

## 3. 아이템 조합

### 3.1 조합 규칙

```
기본 아이템 2개를 같은 유닛에 장착하면 자동 조합:

조합 발생 시점:
  1. 유닛이 이미 기본 아이템 1개 보유 + 기본 아이템 1개 장착 → 조합
  2. 기본 아이템 2개를 동시에 장착 → 조합 (드래그 시)

조합 결과:
  - 기본A + 기본B → 완성 아이템(A,B)
  - 조합표(ItemRecipeAsset)에서 결과 결정
  - 동일 기본 아이템끼리도 조합 가능 (검+검 → 특정 완성 아이템)

유닛이 기본 아이템 1개를 가진 상태에서 완성 아이템을 장착하는 경우:
  → 완성 아이템은 그대로 장착 (기본 아이템과 조합 안 됨)
  → 완성 아이템 + 기본 아이템 공존 가능

유닛에 완성 아이템이 이미 있을 때 기본 아이템 장착:
  → 기본 아이템 단독으로 슬롯 차지
  → 다른 기본 아이템이 또 오면 그때 조합
```

### 3.2 조합표 예시 (일부)

```
| 재료1 | 재료2 | 완성 아이템 | 효과 |
|-------|-------|------------|------|
| 검 | 검 | 분노의 검 | 공격력 +20, 기본 공격 시 추가 물리 데미지 |
| 검 | 활 | 질풍검 | 공격속도 +30%, 공격력 +10 |
| 검 | 지팡이 | 마검 | 공격력 +10, 기본 공격에 추가 마법 데미지 |
| 활 | 활 | 속사포 | 공격속도 +40%, 3회 공격마다 추가 타격 |
| 지팡이 | 지팡이 | 대마법사 지팡이 | 스킬 위력 +30% |
| 눈물 | 눈물 | 마나의 대양 | 시작 마나 +30, 마나 회복 증가 |
| 갑옷 | 갑옷 | 가시 갑옷 | 방어력 +40, 피격 시 반사 데미지 |
| 갑옷 | 망토 | 수호자의 맹세 | 방어력 +20, 마저 +20, HP 15% 이하 시 보호막 |
| 벨트 | 벨트 | 거인의 허리띠 | HP +400, CC 면역 1회 |
| 장갑 | 장갑 | 도적의 장갑 | 아이템 대신 완성 아이템 2개 랜덤 장착 |
| 검 | 갑옷 | 흡혈 검 | 공격력 +10, 생명력 흡수 15% |
| 지팡이 | 망토 | 이온 점화기 | 스킬 위력 +15%, 마저 +20, 적 회복 방해 |
| ... | ... | ... | ... |

총 36종 (8C2 + 8 = 28 + 8 = 36)
```

---

## 4. 아이템 장착/해제

### 4.1 Command 정의

```csharp
// 아이템 장착
public class EquipItemCommand : DeterministicCommand
{
    public EntityRef ItemEntity;
    public EntityRef TargetUnit;

    public override void Serialize(BitStream stream)
    {
        stream.Serialize(ref ItemEntity);
        stream.Serialize(ref TargetUnit);
    }
}

// 아이템 해제 (Preparation 페이즈에서만)
public class UnequipItemCommand : DeterministicCommand
{
    public EntityRef UnitEntity;
    public Byte ItemSlotIndex;   // 0, 1, 2

    public override void Serialize(BitStream stream)
    {
        stream.Serialize(ref UnitEntity);
        stream.Serialize(ref ItemSlotIndex);
    }
}
```

### 4.2 장착 규칙

```
Preparation 페이즈:
  - 장착: 가능
  - 해제: 가능 (아이템이 인벤토리로 복귀)

Combat 페이즈:
  - 장착: 가능 (인벤토리에서 보드 유닛에)
  - 해제: 불가

공통 규칙:
  - 유닛 아이템 슬롯 3개 중 빈 슬롯에 장착
  - 슬롯 3개 모두 찬 경우 → 장착 불가
  - 유니크 아이템 중복 체크 → 같은 완성 아이템 이미 있으면 불가
  - 자기 소유 유닛에만 장착 가능
```

### 4.3 아이템 인벤토리

```
플레이어별 아이템 보관소:
  - 보유할 수 있는 미장착 아이템 수: 최대 10개
  - 인벤토리 가득 참 + 아이템 획득 → 자동으로 가장 오래된 아이템 위에 겹침
    → 또는 크립 드롭 시 직접 선택

인벤토리에 있는 아이템:
  - 유닛에 장착 대기
  - 판매 불가 (아이템은 판매할 수 없음)
```

---

## 5. Quantum 컴포넌트 설계

### 5.1 아이템 컴포넌트

```qtn
component ItemData {
    Int32 ItemSpecId;
    Boolean IsBaseItem;      // true: 기본 아이템, false: 완성 아이템
    player_ref Owner;
    ItemLocation Location;

    // 장착 정보
    EntityRef EquippedUnit;  // 장착된 유닛 (None이면 인벤토리)
    Byte SlotIndex;          // 유닛 내 슬롯 (0~2)
}

enum ItemLocation {
    Inventory,
    Equipped
}
```

### 5.2 플레이어 아이템 인벤토리

```qtn
component PlayerItemInventory {
    array<EntityRef>[10] Items;  // 미장착 아이템 (최대 10개)
}
```

### 5.3 아이템 스펙 (Asset)

```csharp
public class ItemSpecAsset : AssetObject
{
    public int ItemId;
    public string Name;
    public bool IsBaseItem;
    public Sprite Icon;

    // 기본 아이템이면: 조합 재료 ID (자기 자신)
    // 완성 아이템이면: 조합 재료 2개
    public int RecipeItem1;
    public int RecipeItem2;

    // 스탯 보너스
    public FP BonusAttack;
    public FP BonusAttackSpeedPercent;
    public FP BonusSpellPowerPercent;
    public FP BonusMana;
    public FP BonusArmor;
    public FP BonusMagicResist;
    public FP BonusHP;
    public FP BonusCritChance;

    // 특수 효과
    public ItemEffectType SpecialEffect;
    public FP EffectValue1;
    public FP EffectValue2;

    // 유니크 여부
    public bool IsUnique;
}

public enum ItemEffectType
{
    None,
    LifeSteal,              // 기본 공격 흡혈
    SpellVamp,              // 스킬 흡혈
    ReflectDamage,          // 반사 데미지
    OnHitMagicDamage,       // 기본 공격 시 추가 마법 데미지
    ShieldOnLowHP,          // HP 낮을 때 보호막
    ManaRefund,             // 스킬 사용 후 마나 일부 환원
    BurnOnHit,              // 기본 공격 시 화상 (지속 데미지)
    AntiHeal,               // 적 회복 감소
    ExtraAttack,            // N회 공격마다 추가 타격
    CCImmunity,             // CC 면역 (1회 또는 일정 시간)
    DodgeChance,            // 회피 확률
    Cleave,                 // 범위 공격 (기본 공격이 주변에도 데미지)
}
```

### 5.4 조합표 (Asset)

```csharp
public class ItemRecipeAsset : AssetObject
{
    public ItemRecipeEntry[] Recipes;
}

public struct ItemRecipeEntry
{
    public int BaseItem1Id;
    public int BaseItem2Id;
    public int ResultItemId;
}
```

---

## 6. ItemSystem 설계

### 6.1 장착 처리

```csharp
public unsafe class ItemSystem : SystemMainThread
{
    public override void Update(Frame f)
    {
        // Preparation & Combat 모두에서 장착 가능
        var phase = f.Global->CurrentPhase;
        if (phase != GamePhase.Preparation && phase != GamePhase.Combat) return;

        for (int p = 0; p < f.PlayerCount; p++)
        {
            var player = (PlayerRef)p;

            foreach (var cmd in f.GetPlayerCommands<EquipItemCommand>(player))
                ProcessEquip(f, player, cmd, phase);

            // 해제는 Preparation에서만
            if (phase == GamePhase.Preparation)
            {
                foreach (var cmd in f.GetPlayerCommands<UnequipItemCommand>(player))
                    ProcessUnequip(f, player, cmd);
            }
        }
    }

    private void ProcessEquip(Frame f, PlayerRef player,
                               EquipItemCommand cmd, GamePhase phase)
    {
        if (!f.Exists(cmd.ItemEntity)) return;
        if (!f.Exists(cmd.TargetUnit)) return;

        var item = f.Get<ItemData>(cmd.ItemEntity);
        if (item->Owner != player) return;
        if (item->Location != ItemLocation.Inventory) return;

        var unit = f.Get<UnitData>(cmd.TargetUnit);
        if (unit->Owner != player) return;

        // 전투 중에는 보드 유닛에만 장착 가능
        if (phase == GamePhase.Combat && unit->Location != UnitLocation.Board)
            return;

        // 빈 슬롯 찾기
        int emptySlot = -1;
        for (int i = 0; i < 3; i++)
        {
            if (unit->Items[i] == EntityRef.None)
            {
                emptySlot = i;
                break;
            }
        }
        if (emptySlot == -1) return; // 슬롯 가득 참

        // 유니크 중복 체크
        var itemSpec = f.FindAsset<ItemSpecAsset>(item->ItemSpecId);
        if (itemSpec.IsUnique)
        {
            for (int i = 0; i < 3; i++)
            {
                if (unit->Items[i] == EntityRef.None) continue;
                var existing = f.Get<ItemData>(unit->Items[i]);
                if (existing->ItemSpecId == item->ItemSpecId) return; // 중복
            }
        }

        // 인벤토리에서 제거
        RemoveFromInventory(f, player, cmd.ItemEntity);

        // 유닛에 장착
        unit->Items[emptySlot] = cmd.ItemEntity;
        item->Location = ItemLocation.Equipped;
        item->EquippedUnit = cmd.TargetUnit;
        item->SlotIndex = (byte)emptySlot;

        // 조합 체크
        TryCombineItems(f, player, cmd.TargetUnit, unit);

        f.Events.ItemEquipped(player, cmd.ItemEntity, cmd.TargetUnit, (byte)emptySlot);
    }
}
```

### 6.2 자동 조합

```csharp
private void TryCombineItems(Frame f, PlayerRef player,
                              EntityRef unitEntity, UnitData* unit)
{
    // 기본 아이템 2개 이상인지 체크
    var baseItems = new List<(int slot, EntityRef entity, int specId)>();

    for (int i = 0; i < 3; i++)
    {
        if (unit->Items[i] == EntityRef.None) continue;
        var item = f.Get<ItemData>(unit->Items[i]);
        var spec = f.FindAsset<ItemSpecAsset>(item->ItemSpecId);
        if (spec.IsBaseItem)
        {
            baseItems.Add((i, unit->Items[i], item->ItemSpecId));
        }
    }

    if (baseItems.Count < 2) return;

    // 첫 2개 기본 아이템으로 조합
    var a = baseItems[0];
    var b = baseItems[1];

    int resultSpecId = LookupRecipe(f, a.specId, b.specId);
    if (resultSpecId == 0) return; // 조합 불가 (있을 수 없지만 안전 체크)

    // 소재 아이템 제거
    f.Destroy(a.entity);
    f.Destroy(b.entity);

    // 완성 아이템 생성
    var resultEntity = f.Create();
    var resultItem = f.Add<ItemData>(resultEntity);
    resultItem->ItemSpecId = resultSpecId;
    resultItem->IsBaseItem = false;
    resultItem->Owner = player;
    resultItem->Location = ItemLocation.Equipped;
    resultItem->EquippedUnit = unitEntity;
    resultItem->SlotIndex = (byte)a.slot;

    // 슬롯 정리
    unit->Items[a.slot] = resultEntity;
    unit->Items[b.slot] = EntityRef.None;

    // 나머지 기본 아이템이 또 있으면 재귀 체크
    // (3개 기본 아이템 상황: 2개 조합 후 1개 남음)

    f.Events.ItemCombined(player, resultEntity, resultSpecId, unitEntity);
}
```

---

## 7. 전투 중 아이템 효과 적용

### 7.1 스탯 보너스 적용

```csharp
// CombatUnit 생성 시 아이템 스탯 반영
private void ApplyItemStats(Frame f, CombatUnit* combat, UnitData* source)
{
    for (int i = 0; i < 3; i++)
    {
        if (source->Items[i] == EntityRef.None) continue;
        var item = f.Get<ItemData>(source->Items[i]);
        var spec = f.FindAsset<ItemSpecAsset>(item->ItemSpecId);

        combat->Attack += spec.BonusAttack;
        combat->AttackSpeed += combat->AttackSpeed * spec.BonusAttackSpeedPercent / 100;
        combat->Armor += spec.BonusArmor;
        combat->MagicResist += spec.BonusMagicResist;
        combat->MaxHP += spec.BonusHP;
        combat->CurrentHP += spec.BonusHP;
        combat->CurrentMana += spec.BonusMana;
        // CritChance, SpellPower는 별도 컴포넌트/플래그로 관리
    }
}
```

### 7.2 특수 효과 처리

```csharp
// 전투 중 아이템 특수 효과는 CombatUnit에 플래그로 기록
component CombatItemEffects {
    FP LifeStealPercent;
    FP SpellVampPercent;
    FP ReflectDamagePercent;
    FP OnHitMagicDamage;
    FP BurnDamagePerSecond;
    FP AntiHealPercent;
    FP DodgeChance;
    FP CleavePercent;
    Int32 ExtraAttackInterval;    // N회마다 추가 타격
    Int32 AttackCounter;          // 현재 공격 카운트
    Boolean HasCCImmunity;
    Boolean CCImmunityUsed;
    FP ShieldThresholdPercent;
    FP ShieldAmount;
    Boolean ShieldTriggered;
}
```

### 7.3 데미지 계산 시 아이템 효과 참조

```
공격 시:
  1. 기본 데미지 계산
  2. OnHitMagicDamage → 추가 마법 데미지
  3. Cleave → 주변 적에게 비율 데미지
  4. BurnOnHit → 화상 디버프 부착
  5. ExtraAttack → 카운터 체크, 추가 타격
  6. LifeSteal → 물리 데미지의 N% 회복

피격 시:
  1. DodgeChance → 회피 체크
  2. ReflectDamage → 공격자에게 반사
  3. ShieldOnLowHP → HP 임계점 체크, 보호막 부여
  4. CCImmunity → CC 효과 무효화 (1회)

스킬 사용 시:
  5. SpellPower → 스킬 위력 증폭
  6. SpellVamp → 스킬 데미지의 N% 회복
  7. ManaRefund → 마나 일부 환원
  8. AntiHeal → 타겟 회복 감소 디버프
```

---

## 8. 아이템 획득 경로

### 8.1 크립 라운드 (PvE)

```
크립 전투 승리 시 아이템 드롭:

드롭 수량:
  - 1-1: 기본 아이템 1개
  - 1-2: 기본 아이템 1개
  - 1-3: 기본 아이템 1~2개
  - 3-1 (중간 크립): 기본 아이템 2개 + 골드
  - 이후 PvE: 기본 아이템 2~3개

드롭 풀:
  - 8종 기본 아이템 중 균등 확률 (Quantum RNG)
  - 완성 아이템은 직접 드롭하지 않음

크립 전투 패배 시:
  - 드롭 없음 or 1개 감소
```

### 8.2 캐러셀 (SharedDraft)

```
중앙에 챔피언+아이템 조합 배치:
  - 각 챔피언이 기본 아이템 1개 장착
  - 챔피언 선택 시 아이템도 함께 획득
  - HP가 낮은 플레이어부터 선택 (약자 보호)

4인 게임:
  - 캐러셀 등장 수: 6~8개 (4명이 선택해도 2~4개 남음)
  - 순위별 1개씩 선택
```

---

## 9. 전투 중 아이템 장착

### 9.1 전투 중 장착 흐름

```
전투 중에 인벤토리 아이템을 보드 유닛에 장착 가능:

흐름:
  1. 플레이어가 인벤토리에서 아이템 드래그
  2. 전투 중인 자기 유닛에 드롭
  3. EquipItemCommand 전송
  4. Quantum에서 처리:
     a. 유닛 슬롯 여유 확인
     b. UnitData에 아이템 장착 기록
     c. 현재 전투 중인 CombatUnit에도 즉시 반영
        → 해당 CombatUnit의 SourceUnit이 같은 유닛이면
        → CombatUnit의 스탯 재계산

특수 처리:
  - 전투 중 장착 시 CombatUnit의 스탯이 즉시 갱신됨
  - 보호막, CC면역 등 전투 시작 시 트리거 효과는 적용 안 됨
  - 스탯 보너스만 즉시 반영 (HP, 공격력, 방어력 등)
```

### 9.2 전투 중 장착 시 아이템 효과

```
즉시 적용:
  - 스탯 보너스 (Attack, Armor, MR, AttackSpeed 등)
  - HP 보너스 (MaxHP 증가, CurrentHP도 증가)
  - 지속 효과 (LifeSteal, OnHit 등)

적용 안 됨:
  - 전투 시작 시 1회 트리거 (StartingMana, ShieldOnCombatStart)
  - CC면역 이미 소진된 경우 리셋 안 됨

이유:
  - 전투 시작 시 1회 효과를 전투 중 장착으로 받을 수 있으면
    의도적으로 늦게 장착하는 최적 전략이 생겨 게임 복잡도만 올라감
```

---

## 10. View 이벤트

```qtn
event ItemEquipped {
    player_ref Player;
    entity_ref Item;
    entity_ref Unit;
    Byte SlotIndex;
}

event ItemUnequipped {
    player_ref Player;
    entity_ref Item;
    entity_ref Unit;
    Byte SlotIndex;
}

event ItemCombined {
    player_ref Player;
    entity_ref ResultItem;
    Int32 ResultItemSpecId;
    entity_ref Unit;
}

event ItemDropped {
    player_ref Player;
    Int32 ItemSpecId;
}

event ItemAcquired {
    player_ref Player;
    entity_ref Item;
    Int32 ItemSpecId;
}
```

---

## 11. UI 표현

### 11.1 아이템 인벤토리 UI

```
화면 하단 또는 벤치 옆에 아이템 인벤토리 표시:

┌─┬─┬─┬─┬─┬─┬─┬─┬─┬─┐
│🗡│🏹│ │ │ │ │ │ │ │ │  아이템 인벤토리 (10칸)
└─┴─┴─┴─┴─┴─┴─┴─┴─┴─┘

- 드래그하여 유닛에 장착
- 기본 아이템: 단일 아이콘
- 탭하면 조합 가능 목록 표시 (레시피 힌트)
```

### 11.2 유닛 아이템 표시

```
유닛 발 아래에 작은 아이템 아이콘 (최대 3개):

  [유닛 Spine]
  ◆ ◆ ◇       ← 완성 2개 + 빈 슬롯 1개

탭하면 아이템 상세 팝업:
  - 이름, 효과 설명
  - 조합 재료 표시 (완성 아이템인 경우)
  - 해제 버튼 (Preparation에서만)
```

### 11.3 조합 가이드

```
기본 아이템을 길게 누르면:
  - 해당 아이템으로 만들 수 있는 모든 완성 아이템 목록
  - 필요한 나머지 재료 표시
  - 보유 여부 하이라이트
```

---

## 12. 4인 게임 아이템 밸런스

```
TFT 대비 조정:

아이템 획득량:
  - PvE 라운드 수가 동일하므로 기본 획득량 유지
  - 캐러셀 주기 동일 유지
  - 4인 게임에서 아이템 총량은 TFT의 ~50%
  - 개인당 아이템 수는 비슷하거나 약간 많음

아이템 밸런스:
  - 유닛 수 최대 8개 × 아이템 3개 = 최대 24개 아이템
  - 실제로 후반에 보유하는 아이템: 6~10개
  - 완성 아이템 3~5개 + 기본 아이템 수개

조합 재료 분배:
  - 크립 드롭이 주 수입원
  - 캐러셀은 약자 보호 + 특정 아이템 노림
  - 균형 잡힌 아이템 분배가 게임 공정성의 핵심
```
