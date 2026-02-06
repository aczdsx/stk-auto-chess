# 샵 & 챔피언 풀 시스템 설계

> 공유 챔피언 풀, 샵 메커니즘, 리롤, 구매/판매를 정의한다.
>
> **GameMode 적용**: `EnableShop=true`인 모드에서만 활성화 (PvECampaign, Competitive).
> - Competitive: `SharedPool=true` — 4인이 하나의 챔피언 풀 공유
> - PvECampaign: `SharedPool=false` — 솔로 전용 풀 (풀 고갈이 완화됨)
> - ClassicBattle: 상점/풀 비활성화 — `UnitSourceType.OwnedCharacters` 사용

---

## 1. 챔피언 풀 (Champion Pool)

### 1.1 공유 풀 개념

```
4인 플레이어가 하나의 챔피언 풀을 공유.

풀 동작:
  - 게임 시작 시 등급별 정해진 수량으로 풀 초기화
  - 상점에 챔피언이 등장 → 풀에서 해당 챔피언 1개 차감 (예약)
  - 상점 리프레시/리롤 시 → 미구매 슬롯의 챔피언을 풀에 반환
  - 플레이어가 챔피언 판매 → 풀에 해당 챔피언 1개 반환
  - 플레이어 탈락 → 보유 챔피언 + 상점 미구매 챔피언 전부 풀에 반환

핵심 원칙:
  **"상점에 표시된 챔피언은 이미 풀에서 빠진 상태"**
  → A 플레이어 상점에 떠 있는 챔피언은 B 플레이어 상점에 뜰 확률 감소
  → 서로의 상점이 간접적으로 경쟁하는 구조

전략적 의미:
  - 다른 플레이어가 상점에 보유 or 구매한 챔피언은 출현 확률 감소
  - 3★ 달성이 어려워짐 → 견제/경쟁 요소
  - 누구도 사지 않는 챔피언은 출현 확률 증가
  - 상점을 잠금(Lock)하면 해당 챔피언이 계속 풀에서 빠져있는 효과
```

### 1.2 등급별 풀 크기

| 등급 | 챔피언당 풀 수량 | 비고 |
|------|---------------|------|
| 1코스트 | 22개 | 흔함, 초반 핵심 |
| 2코스트 | 18개 | |
| 3코스트 | 16개 | |
| 4코스트 | 12개 | |
| 5코스트 | 8개 | 희귀, 후반 핵심 |

> 4인 게임이므로 TFT(8인) 대비 풀 크기 축소.
> 예: 1코스트 챔피언 A가 22개 → 4인이 나눠 가질 수 있는 한계.
> 3★ 달성에 9개 필요 → 22개 중 9개 = 전략적 선택.

### 1.3 풀 고갈 메커니즘

```
풀에서 특정 챔피언이 0개가 되면:
  → 해당 챔피언은 더 이상 샵에 등장하지 않음
  → 리롤해도 나오지 않음
  → 다른 플레이어가 판매하거나 상점 리프레시로 반환하면 다시 등장 가능
  → 다른 플레이어의 상점에 떠 있는 것도 차감된 상태이므로
    그 플레이어가 리롤/새 라운드 진입 시 반환되어야 가용

3★ 합성 시:
  → 9개(1★×3 → 2★, 2★×3 → 3★) 소모
  → 3★ 유닛 판매 시 9개 모두 풀에 반환
```

---

## 2. 샵 메커니즘

### 2.1 샵 슬롯

```
┌──────┬──────┬──────┬──────┬──────┐
│ Slot │ Slot │ Slot │ Slot │ Slot │
│  0   │  1   │  2   │  3   │  4   │
│ 챔피언A│ 챔피언B│ 챔피언C│ 챔피언D│ 챔피언E│
│ 2골드 │ 3골드 │ 1골드 │ 4골드 │ 2골드 │
└──────┴──────┴──────┴──────┴──────┘
    [리롤 2골드]          [XP 구매 4골드]
```

- 매 Preparation 시작 시 자동 리프레시 (무료)
- 5개 슬롯에 랜덤 챔피언 표시
- 플레이어 레벨에 따른 등급 출현율 적용 (04_EconomySystem.md 참조)

### 2.2 리롤

```
비용: 2골드
동작:
  1. 현재 샵의 미구매 챔피언 → 풀에 반환 (등장 시 차감된 것을 되돌림)
  2. 풀에서 새로운 5개 챔피언 추출 (추출 즉시 풀에서 차감)
  3. 등급 확률 테이블에 따라 결정론적 추출 (Quantum RNG)
```

### 2.3 구매

```
BuyUnitCommand(shopSlotIndex):
  1. 해당 슬롯의 챔피언 확인
  2. 골드 충분한지 체크
  3. 벤치에 빈 자리 있는지 체크
  4. 골드 차감
  5. 챔피언 엔티티 생성 → 벤치에 배치
  6. 풀 차감 불필요 (상점 등장 시 이미 차감됨)
  7. 자동 합성 체크 (동일 챔피언 3개 → 별 승급)
  8. 샵 슬롯을 IsPurchased로 마킹 (반환 방지)

실패 조건:
  - 골드 부족 → 무시
  - 벤치 + 보드 모두 꽉 참 → 무시
```

### 2.4 판매

```
SellUnitCommand(unitEntity):
  1. 해당 유닛의 등급과 별 확인
  2. 판매 골드 계산
  3. 골드 지급
  4. 유닛 엔티티 제거
  5. 풀에 챔피언 반환

판매 가격:
  1★: 구매가와 동일 (1/2/3/4/5 코스트)
  2★: 구매가 × 3
  3★: 구매가 × 9

예시:
  3코스트 1★ 판매 → 3골드
  3코스트 2★ 판매 → 9골드
  3코스트 3★ 판매 → 27골드
```

### 2.5 샵 잠금

```
LockShopCommand:
  - 현재 샵 내용을 다음 라운드까지 유지
  - 자동 리프레시 스킵
  - 원하는 챔피언이 있지만 골드가 부족할 때 사용
  - 잠금 해제: 다시 LockShopCommand 또는 리롤
```

---

## 3. 챔피언 추출 알고리즘

### 3.1 샵 리프레시 과정

```
RefreshShop(player):
  for each slot (0~4):
    1. 등급 결정
       rarity = RollRarity(player.Level, rng)
       // 레벨별 확률 테이블에서 결정론적 추출

    2. 해당 등급의 풀에서 챔피언 선택
       candidates = GetAvailableChampions(rarity, pool)
       // 풀에 1개 이상 남은 챔피언만 후보

       if candidates.empty:
         // 해당 등급이 완전 소진 → 다른 등급으로 대체
         rarity = FindAlternativeRarity(rng)
         candidates = GetAvailableChampions(rarity, pool)

    3. 후보 중 랜덤 선택 (가중치 균등)
       champion = candidates[rng.Next(0, candidates.Count)]

    4. 풀에서 즉시 차감 (상점 등장 = 풀에서 제거)
       pool[champion] -= 1
       // 이 시점부터 다른 플레이어의 상점에 등장 확률 감소

    5. 슬롯에 배치
       shop.slots[slot] = champion
```

### 3.2 풀 반환 타이밍

```
챔피언이 풀에서 차감되는 시점:
  1. 상점에 등장 시 → 즉시 차감 (예약)
  2. (구매 시에는 이미 차감된 상태 → 추가 차감 없음)

챔피언이 풀에 반환되는 시점:
  1. 리롤 시 → 미구매 슬롯의 챔피언 반환
  2. 라운드 시작 자동 리프레시 → 미구매 슬롯 반환
  3. 판매 시 → 즉시 반환
  4. 플레이어 탈락 시 → 보유 유닛 + 상점 미구매 챔피언 전부 반환
  5. 3★ 합성 시 → 반환 없음 (유닛으로 존재)
  6. 3★ 판매 시 → 9개 반환
```

---

## 4. Quantum 컴포넌트 설계

### 4.1 챔피언 풀

```qtn
// 전역 상태 (Global)
global {
    // ... 기존 필드 ...

    // 챔피언 풀: championId → 남은 수량
    // Quantum dictionary 사용
}

component ChampionPool {
    dictionary<Int32, Int32> Stock;  // championSpecId → remaining count
}
```

### 4.2 플레이어 샵

```qtn
component PlayerShop {
    array<ShopSlot>[5] Slots;
    Boolean IsLocked;
}

struct ShopSlot {
    Int32 ChampionSpecId;    // 0이면 빈 슬롯
    Int32 Cost;              // 코스트 (1~5)
    Boolean IsPurchased;     // 구매 완료 여부
}
```

### 4.3 챔피언 스펙 (Asset)

```csharp
public class ChampionSpecAsset : AssetObject
{
    public int ChampionId;
    public string Name;
    public int Cost;              // 1~5 코스트
    public int PoolCount;         // 등급별 풀 크기 (22/18/16/12/8)

    // 기본 스탯
    public FP BaseHP;
    public FP BaseAttack;
    public FP BaseArmor;
    public FP BaseMagicResist;
    public FP AttackSpeed;
    public FP AttackRange;        // 타일 단위
    public FP MoveSpeed;

    // 스킬
    public AssetRef<SkillSpecAsset> Skill;
    public int ManaCost;
    public int StartingMana;

    // 시너지
    public int[] TraitIds;        // 기원 + 직업 (2~3개)

    // 별 승급 시 스탯 배율
    public FP Star2Multiplier;    // 1.8x
    public FP Star3Multiplier;    // 3.2x
}
```

---

## 5. ShopSystem 설계

```csharp
public unsafe class ShopSystem : SystemMainThread,
    ISignalOnPhaseStarted
{
    public void OnPhaseStarted(Frame f, GamePhase phase)
    {
        if (phase != GamePhase.Preparation) return;

        // 모든 생존 플레이어의 샵 리프레시
        var players = f.Filter<PlayerShop, PlayerEconomy>();
        while (players.Next(out var entity, out var shop, out var economy))
        {
            if (shop->IsLocked)
            {
                shop->IsLocked = false; // 잠금은 1회만 유지
                continue;
            }
            RefreshShop(f, entity, shop, economy);
        }
    }

    public override void Update(Frame f)
    {
        // Command 처리
        for (int p = 0; p < f.PlayerCount; p++)
        {
            var player = (PlayerRef)p;

            foreach (var cmd in f.GetPlayerCommands<BuyUnitCommand>(player))
                ProcessBuy(f, player, cmd);

            foreach (var cmd in f.GetPlayerCommands<SellUnitCommand>(player))
                ProcessSell(f, player, cmd);

            foreach (var cmd in f.GetPlayerCommands<RerollShopCommand>(player))
                ProcessReroll(f, player);

            foreach (var cmd in f.GetPlayerCommands<LockShopCommand>(player))
                ProcessLock(f, player);
        }
    }
}
```

---

## 6. View 이벤트

```qtn
event ShopRefreshed {
    player_ref Player;
    // View에서 Frame의 PlayerShop 컴포넌트를 직접 읽어서 갱신
}

event UnitPurchased {
    player_ref Player;
    Int32 ChampionSpecId;
    Int32 ShopSlot;
    Int32 RemainingGold;
}

event UnitSold {
    player_ref Player;
    entity_ref Unit;
    Int32 SellPrice;
    Int32 RemainingGold;
}

event ShopLocked {
    player_ref Player;
    Boolean IsLocked;
}
```

---

## 7. 4인 게임 풀 밸런스

### 챔피언 수 설계 가이드

```
등급별 챔피언 종류 수 (예시):

1코스트: 10종 × 22개 = 220개 (풀 총량)
2코스트: 8종 × 18개 = 144개
3코스트: 8종 × 16개 = 128개
4코스트: 5종 × 12개 = 60개
5코스트: 4종 × 8개 = 32개

4인 게임에서:
  - 1코스트 A를 혼자 9개(3★) 모으기: 9/22 = 41% 사용
  - 4인 중 2명이 같은 5코스트 경쟁: 8개를 둘이 나눔 → 3★ 불가능
```

### 풀 크기 조정 원칙

```
8인 게임 대비:
  - 풀 총량을 약 50~60% 수준으로 축소
  - 챔피언 종류 수는 유사하게 유지 (시너지 다양성)
  - 5코스트 풀은 더 타이트하게 (경쟁 유도)
```
