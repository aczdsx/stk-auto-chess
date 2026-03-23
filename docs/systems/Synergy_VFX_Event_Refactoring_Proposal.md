# Synergy VFX Event Refactoring Proposal

> **상태: 구현 완료** (2026-03-23)
> 아래 제안 내용은 모두 코드에 반영됨. 변경 파일: `SimulationEvents.cs`, `SynergySystem.cs`, `AutoChessViewBridge.cs`, `InGameMain_New.cs`

## 1. 문제 배경

### 증상
게임 진입 시 또는 관전 전환 시, **모든 활성 시너지의 달성 VFX가 한꺼번에 발동**되는 버그 발생.

### 원인
현재 시너지 VFX 발동 판단은 **View 레이어(`AutoChessViewBridge`)가 자체 관리하는 `_prevSynergyTiers` 스냅샷**과 현재 시너지 상태의 diff로 이루어진다. 이 스냅샷의 초기화가 누락되거나 타이밍이 어긋나면, 모든 시너지가 "새로 달성된 것"으로 판정되어 VFX가 일괄 발동한다.

구체적으로:
- `_prevSynergyTiers`는 `byte[]` 배열로 `Initialize()` 시 0으로 초기화됨 (`AutoChessViewBridge.cs:68-69`)
- `InGameMain_New.StartAutoChess()`에서 `RefreshSynergySnapshot()` 호출로 현재 상태를 반영 (`InGameMain_New.cs:70`)
- 그러나 **관전 전환, 재접속, 특정 초기화 경로**에서 `RefreshSynergySnapshot()` 호출이 누락될 경우, 0 상태의 스냅샷과 현재 활성 시너지가 diff되어 전부 신규 달성으로 판정

### 근본적 구조 문제
View가 "이전 상태"를 자체 관리하여 diff하는 방식은:
- 초기화 시점에 대한 **암묵적 의존**이 존재
- 새로운 진입 경로가 추가될 때마다 `RefreshSynergySnapshot()` 호출을 빠뜨릴 위험
- Simulation과 View 사이에 **이중 스냅샷** 관리 (`world.PrevSynergyTiers` vs `_prevSynergyTiers`)

---

## 2. 현재 구조 다이어그램

```
Board Change (Place / Withdraw / Swap)
    |
    v
CommandProcessor.OnBoardChanged()                    [Simulation Layer]
    |-- SynergySystem.Recalculate()                  world.Synergies[playerIndex] 갱신
    |-- SynergySystem.SyncPrepBehaviors()            world.PrevSynergyTiers와 diff → PrepBehavior 생성/제거/변경
    |       └── world.PrevSynergyTiers 갱신
    └-- EventQueue.PushSynergyUpdated(playerIndex)   ← 단순 "변경됨" 알림 (상세 정보 없음)
            |
            v
AutoChessViewBridge.HandleTick()                     [View Layer]
    case SynergyUpdated:
        |-- HandleSynergyVfx(world, playerIndex)
        |       |-- for each trait: compare _prevSynergyTiers[traitId] vs current tier
        |       |-- if newTier > oldTier → SpawnSynergyAchieveVfx()
        |       └-- _prevSynergyTiers[traitId] = newTier  (View 자체 스냅샷 갱신)
        └-- OnSynergyUpdated() → UI 갱신
```

**문제 핵심**: `SynergyUpdated` 이벤트는 "시너지가 변경되었다"는 사실만 전달하고, **무엇이 어떻게 변했는지**는 View가 스스로 판단해야 함. 이를 위해 View가 별도의 `_prevSynergyTiers` 스냅샷을 유지하며, 이 스냅샷의 동기화 책임이 외부 호출자(`InGameMain_New`)에게 분산됨.

---

## 3. 제안 구조 — Simulation 이벤트 직접 발행

### 핵심 아이디어
Simulation이 시너지 티어 변경을 감지하는 시점에 **`SynergyTierChanged(traitId, oldTier, newTier)` 이벤트를 직접 발행**하여, View가 스냅샷을 관리할 필요를 제거한다.

### 제안 구조 다이어그램

```
Board Change (Place / Withdraw / Swap)
    |
    v
CommandProcessor.OnBoardChanged()                         [Simulation Layer]
    |-- SynergySystem.Recalculate()                       world.Synergies[playerIndex] 갱신
    |-- SynergySystem.SyncPrepBehaviors()
    |       |-- oldTier vs newTier 비교
    |       |-- PrepBehavior 생성/제거/변경
    |       |-- ★ EventQueue.PushSynergyTierChanged(playerIndex, traitId, oldTier, newTier)
    |       └── world.PrevSynergyTiers 갱신
    └-- EventQueue.PushSynergyUpdated(playerIndex)        UI 갱신용 (기존 유지)
            |
            v
AutoChessViewBridge.HandleTick()                          [View Layer]
    case SynergyTierChanged:
        |-- if newTier > oldTier → SpawnSynergyAchieveVfx()  (이벤트 데이터 직접 사용)
        └-- (스냅샷 관리 불필요)
    case SynergyUpdated:
        └-- OnSynergyUpdated() → UI 갱신
```

### 장점
- View가 **이전 상태를 기억할 필요 없음** → `_prevSynergyTiers` 제거
- 초기화 타이밍과 무관하게 **이벤트가 발생한 시점에만 VFX 발동**
- 새로운 진입 경로 추가 시 `RefreshSynergySnapshot()` 호출 누락 위험 제거
- Simulation의 `world.PrevSynergyTiers`와 View의 `_prevSynergyTiers`라는 **이중 스냅샷 해소**
- 단일 책임 원칙: 시너지 변경 감지는 Simulation만 담당

---

## 4. 수정 대상 파일 목록

### 4.1 `SimulationEvents.cs`
**경로**: `Assets/_Project/Scripts/InGame_New/Simulation/Data/SimulationEvents.cs`

| 항목 | 내용 |
|------|------|
| `SimEventType` enum | `SynergyTierChanged` 값 추가 (line ~42 부근) |
| `SimEvent` struct | 기존 필드 재활용: `Value0`=traitId, `Value1`=oldTier\|newTier 패킹 (하위8비트=oldTier, 상위8비트=newTier) |
| 새 메서드 | `PushSynergyTierChanged(byte playerIndex, int traitId, byte oldTier, byte newTier)` 추가 |

**구현 예시:**
```csharp
// SimEventType enum (line 42 부근, SynergyUpdated 아래)
SynergyTierChanged,

// PushSynergyTierChanged 팩토리 메서드
public void PushSynergyTierChanged(byte playerIndex, int traitId, byte oldTier, byte newTier)
{
    Push(new SimEvent
    {
        Type = SimEventType.SynergyTierChanged,
        PlayerIndex = playerIndex,
        Value0 = traitId,
        Value1 = oldTier | (newTier << 8),
    });
}
```

### 4.2 `SynergySystem.cs`
**경로**: `Assets/_Project/Scripts/InGame_New/Simulation/Synergy/SynergySystem.cs`

| 항목 | 내용 |
|------|------|
| `SyncPrepBehaviors()` (line ~574) | oldTier/newTier 비교 로직(line ~588-641)에서 티어가 변경된 경우 `EventQueue.PushSynergyTierChanged()` 호출 추가 |
| 발행 시점 | Activation (0→N), Deactivation (N→0), Tier Change (M→N) 모든 경우에 이벤트 발행 |

**구현 예시** (`SyncPrepBehaviors` line 591-641 내부, 각 분기에 추가):
```csharp
// 활성화 (line ~591)
if (oldTier == 0 && newTier > 0)
{
    world.EventQueue.PushSynergyTierChanged(playerIndex, traitId, oldTier, newTier);
    // ... 기존 PrepBehavior 생성 로직 ...
}
// 비활성화 (line ~615)
else if (oldTier > 0 && newTier == 0)
{
    world.EventQueue.PushSynergyTierChanged(playerIndex, traitId, oldTier, newTier);
    // ... 기존 PrepBehavior 제거 로직 ...
}
// 티어 변경 (line ~627)
else if (oldTier != newTier && newTier > 0)
{
    world.EventQueue.PushSynergyTierChanged(playerIndex, traitId, oldTier, newTier);
    // ... 기존 티어 변경 로직 ...
}
```

> **주의**: `SyncPrepBehaviors()`는 `!spec.HasBehavior`인 시너지를 `continue`로 건너뛰지만 (line 585), View의 `HandleSynergyVfx()`는 모든 유효 시너지를 순회합니다. 따라서 두 가지 접근 중 하나를 선택:
>
> **방법 A** (권장): `SyncPrepBehaviors()` 내부에서 이벤트 발행 코드는 `HasBehavior` 필터 **앞**에 배치. 즉 순회 구조를 `if (!spec.IsValid) continue;` → 이벤트 발행 diff → `if (!spec.HasBehavior) continue;` → PrepBehavior 로직 순서로 변경.
>
> **방법 B**: `CommandProcessor.OnBoardChanged()`에서 `SyncPrepBehaviors()` 호출 후 별도의 `EmitSynergyTierChangedEvents()` 헬퍼를 호출하여 전체 시너지를 diff. 단, 이중 diff가 발생하므로 비효율.

### 4.3 `CommandProcessor.cs`
**경로**: `Assets/_Project/Scripts/InGame_New/Simulation/Core/CommandProcessor.cs`

| 항목 | 내용 |
|------|------|
| `OnBoardChanged()` (line ~107) | `PushSynergyUpdated()` 호출은 **유지** (UI 갱신 용도). 별도 변경 불필요 — 이벤트는 `SyncPrepBehaviors()` 내부에서 발행 |

> Note: `PushSynergyUpdated`는 UI 패널 갱신 트리거로 계속 사용하되, VFX 판단은 `SynergyTierChanged`로 분리.

### 4.4 `AutoChessViewBridge.cs`
**경로**: `Assets/_Project/Scripts/InGame_New/View/AutoChessViewBridge.cs`

| 항목 | 내용 |
|------|------|
| `_prevSynergyTiers` 필드 (line 28) | **제거** |
| `RefreshSynergySnapshot()` (line 452) | **제거** |
| `HandleSynergyVfx()` (line 462) | **제거** — diff 로직 전체 불필요 |
| `HandleTick()` (line ~386) | `SynergyUpdated` case에서 `HandleSynergyVfx()` 호출 제거 |
| 새 case 추가 | `case SimEventType.SynergyTierChanged:` → 이벤트의 `NewTier > OldTier`이면 `SpawnSynergyAchieveVfx()` 호출 |
| `SpawnSynergyAchieveVfx()` (line 487) | 유지 (traitId를 이벤트에서 직접 수신) |
| `Initialize()` (line 68-69) | `_prevSynergyTiers` 초기화 및 `RefreshSynergySnapshot()` 호출 제거 |
| `SetSpectateBoard()` (line 691) | `RefreshSynergySnapshot()` 호출 **제거** — 관전 전환 시에도 이벤트 기반으로 동작 |

**구현 예시** (HandleTick 내 새 case):
```csharp
case SimEventType.SynergyTierChanged:
{
    if (evt.PlayerIndex != _localPlayerIndex) break;
    byte oldTier = (byte)(evt.Value1 & 0xFF);
    byte newTier = (byte)((evt.Value1 >> 8) & 0xFF);
    if (newTier > oldTier)
        SpawnSynergyAchieveVfx(world, evt.PlayerIndex, evt.Value0); // Value0 = traitId
    break;
}
```

### 4.5 `InGameMain_New.cs`
**경로**: `Assets/_Project/Scripts/UI/InGame/InGameMain_New.cs`

| 항목 | 내용 |
|------|------|
| `StartAutoChess()` (line 70) | `_viewRoot.ViewBridge.RefreshSynergySnapshot()` 호출 **제거** |

---

## 5. 구현 단계 요약

| 단계 | 작업 | 비고 |
|------|------|------|
| **1** | `SimulationEvents.cs`에 `SynergyTierChanged` 이벤트 타입 및 `PushSynergyTierChanged()` 메서드 추가 | 기존 이벤트와 병존 |
| **2** | `SynergySystem.SyncPrepBehaviors()`에서 tier 변경 감지 시 이벤트 발행 | 기존 diff 로직 활용, 이벤트 Push만 추가 |
| **3** | `AutoChessViewBridge.HandleTick()`에 `SynergyTierChanged` case 추가, VFX 트리거 연결 | `SpawnSynergyAchieveVfx()` 재사용 |
| **4** | `AutoChessViewBridge`에서 `_prevSynergyTiers`, `RefreshSynergySnapshot()`, `HandleSynergyVfx()` 제거 | View 스냅샷 완전 제거 |
| **5** | `InGameMain_New.StartAutoChess()`에서 `RefreshSynergySnapshot()` 호출 제거 | 외부 동기화 의존 제거 |
| **6** | 테스트: 게임 진입, 유닛 배치/철수, 관전 전환 시 VFX 정상 동작 확인 | 기존 버그 재현 시나리오 포함 |

---

## 6. 기대 효과

| 항목 | 개선 내용 |
|------|-----------|
| **버그 근본 해결** | 초기화 타이밍 누락으로 인한 VFX 일괄 발동 버그가 구조적으로 불가능해짐 |
| **이중 스냅샷 해소** | `world.PrevSynergyTiers` (Simulation) + `_prevSynergyTiers` (View) → Simulation 단일 관리 |
| **유지보수성 향상** | 새로운 진입 경로 추가 시 View 스냅샷 동기화를 신경 쓸 필요 없음 |
| **관심사 분리** | "무엇이 변했는가" 판단은 Simulation, "어떻게 보여줄 것인가"는 View — 역할 명확화 |
| **확장성** | `SynergyTierChanged` 이벤트를 다른 시스템(사운드, 로그, 통계)에서도 구독 가능 |
| **코드 감소** | View에서 ~40줄의 diff/스냅샷 로직 제거, 외부 동기화 호출 제거 |
