# VFX 재사용 시스템 설계

## Context

현재 VFX 시스템은 `Instantiate()` → `Destroy()` 패턴에 전면 의존. 두 가지 문제:

1. **페이즈 전환 시 VFX 소실**: Preparation → Combat 전환 시 `Deactivate()` → `ClearPersistentVfx()` + `ReleaseCharacterVisual()`로 VFX GO가 파괴되고, 전투 뷰에서 동일 VFX가 복원되지 않음
2. **전투 중 VFX 낭비**: 피격/스킬/투사체 VFX가 매번 `Instantiate` → `Destroy(go, 3f)`로 처리되어 GC 압력 발생

**목표**:
- **Phase A**: 페이즈 전환 시 VFX GO를 파괴하지 않고 임시 홀더에 보관(parking) → 새 전투 UnitView에 reparenting하여 재사용
- **Phase B** (후속): 전투 중 fire-and-forget VFX + 투사체 VFX에 ObjectPool 적용

## 핵심 흐름

```
[Before]  Destroy VFX → Instantiate new VFX (낭비)
[After]   Detach VFX → Park in holder → Reparent to new UnitView (재사용)
```

### 대상 VFX 2종

| 종류 | 저장소 | 생성 방식 | 현재 문제 |
|------|--------|-----------|----------|
| **Persistent VFX** (루키다 등) | `UnitView._persistentVfx` | `Instantiate(prefab, parentTransform)` | `ClearPersistentVfx()`에서 `Destroy()` |
| **Addressable Supernova VFX** | `AutoChessViewBridge._supernovaTargetVfx` | `Addressables.InstantiateAsync(ref, parentTransform)` | 부모(캐릭터 프리팹) 파괴 시 함께 소멸 |

---

## Step 1: UnitView — Position 추적 + Detach/Adopt

**파일**: `Assets/_Project/Scripts/InGame_New/View/Unit/UnitView.cs`

### 1-1. SkillPosition 추적용 딕셔너리 추가

`_persistentVfx` 옆에:
```csharp
private readonly Dictionary<int, SkillPosition> _persistentVfxPositions = new();
```

### 1-2. UpdatePersistentVfx에서 Position 기록

기존 `_persistentVfx[skillSpecId] = vfxGo;` 바로 다음에:
```csharp
_persistentVfxPositions[skillSpecId] = position;
```

### 1-3. DetachPersistentVfx — 파괴 대신 분리

```csharp
public List<(int skillSpecId, GameObject go, SkillPosition position)> DetachPersistentVfx()
{
    if (_persistentVfx.Count == 0) return null;
    var result = new List<(int, GameObject, SkillPosition)>();
    foreach (var kvp in _persistentVfx)
    {
        if (kvp.Value == null) continue;
        _persistentVfxPositions.TryGetValue(kvp.Key, out var pos);
        result.Add((kvp.Key, kvp.Value, pos));
    }
    _persistentVfx.Clear();
    _persistentVfxPositions.Clear();
    return result;
}
```

### 1-4. AdoptPersistentVfx — 외부 VFX를 입양

```csharp
public void AdoptPersistentVfx(int skillSpecId, GameObject vfxGo, SkillPosition position)
{
    if (vfxGo == null) return;
    var targetTransform = GetSkillPositionTransform(position);
    vfxGo.transform.SetParent(targetTransform);
    vfxGo.transform.localPosition = Vector3.zero;
    vfxGo.transform.localRotation = Quaternion.identity;
    _persistentVfx[skillSpecId] = vfxGo;
    _persistentVfxPositions[skillSpecId] = position;
}
```

### 1-5. DeactivateWithParking — parking 전용 비활성화

`Deactivate()`와 동일하되 `ClearPersistentVfx()` 대신 `DetachPersistentVfx()` 사용:
```csharp
public List<(int skillSpecId, GameObject go, SkillPosition position)> DeactivateWithParking()
{
    _isActive = false;
    ReleaseHpBar();
    var detached = DetachPersistentVfx();
    ReleaseCharacterVisual();
    gameObject.SetActive(false);
    return detached;
}
```

> **중요**: DetachPersistentVfx → ReleaseCharacterVisual 순서. Detach가 먼저여야 VFX가 캐릭터 프리팹 해제 전에 분리됨.

### 1-6. ClearPersistentVfx — Position 딕셔너리 동기화

```csharp
private void ClearPersistentVfx()
{
    foreach (var kvp in _persistentVfx)
    {
        if (kvp.Value != null) Destroy(kvp.Value);
    }
    _persistentVfx.Clear();
    _persistentVfxPositions.Clear();  // 추가
}
```

---

## Step 2: UnitViewManager — Parking 홀더 + Reparent 스케줄링

**파일**: `Assets/_Project/Scripts/InGame_New/View/Unit/UnitViewManager.cs`

### 2-1. Parking 인프라

```csharp
private Transform _vfxParkingHolder;
private readonly Dictionary<int, List<(int skillSpecId, GameObject go, SkillPosition position)>>
    _parkedPersistentVfx = new();

public Transform VfxParkingHolder => _vfxParkingHolder;
```

### 2-2. Initialize에서 parking holder 생성

기존 풀 생성 코드 바로 위에:
```csharp
_vfxParkingHolder = new GameObject("VfxParkingHolder").transform;
_vfxParkingHolder.SetParent(transform);
_vfxParkingHolder.gameObject.SetActive(false);  // 비활성화 → 파티클 시스템 자동 중단
```

### 2-3. OnCombatStart — Destroy 대신 Park

```csharp
public void OnCombatStart()
{
    foreach (var kvp in _boardUnitViews)
    {
        var view = kvp.Value;
        int entityId = view.EntityId;
        var detached = view.DeactivateWithParking();
        if (detached != null && detached.Count > 0)
        {
            foreach (var (_, go, _) in detached)
            {
                if (go != null) go.transform.SetParent(_vfxParkingHolder);
            }
            _parkedPersistentVfx[entityId] = detached;
        }
    }
    _boardUnitViews.Clear();
}
```

> `DeactivateWithParking()`은 항상 비활성화를 수행하고 VFX 리스트만 반환. null/빈 리스트이면 parking할 VFX가 없을 뿐, 뷰는 이미 비활성화됨.

### 2-4. GetOrCreateCombatView에서 reparent 스케줄링

기존 `OnCombatViewCreated?.Invoke(...)` 바로 다음에:
```csharp
if (_parkedPersistentVfx.ContainsKey(sourceEntityId))
    ScheduleVfxReparent(sourceEntityId, view);
```

### 2-5. ScheduleVfxReparent

```csharp
private async void ScheduleVfxReparent(int entityId, UnitView view)
{
    await UniTask.WaitUntil(() => view == null || view.IsReady);
    if (view == null) return;

    if (_parkedPersistentVfx.TryGetValue(entityId, out var list))
    {
        foreach (var (skillSpecId, go, position) in list)
            view.AdoptPersistentVfx(skillSpecId, go, position);
        _parkedPersistentVfx.Remove(entityId);
    }
}
```

> `IsReady` 대기: 캐릭터 프리팹 로드 완료 후 `SkillTopFXTransform` 등이 존재해야 reparent 가능.
> Reparent 시 비활성 홀더 → 활성 부모로 이동하므로 파티클 시스템이 자동 재개됨.

### 2-6. OnCombatEnd — 잔여 parked VFX 정리

```csharp
public void OnCombatEnd()
{
    foreach (var view in _combatUnitViews.Values)
        ReturnToPool(view);
    _combatUnitViews.Clear();
    ClearParkedVfx();
}

private void ClearParkedVfx()
{
    foreach (var list in _parkedPersistentVfx.Values)
    {
        foreach (var (_, go, _) in list)
        {
            if (go != null) Object.Destroy(go);
        }
    }
    _parkedPersistentVfx.Clear();
}
```

### 2-7. using 추가

```csharp
using Cysharp.Threading.Tasks;
using CookApps.AutoBattler;  // SkillPosition
```

---

## Step 3: AutoChessViewBridge — Addressable VFX Detach/Reparent

**파일**: `Assets/_Project/Scripts/InGame_New/View/AutoChessViewBridge.cs`

### 3-1. DetachSupernovaTargetVfx

`OnCombatStart()` 전에 슈퍼노바 VFX GO를 parking holder로 이동 (handle은 유지):
```csharp
private void DetachSupernovaTargetVfx(Transform parkingHolder)
{
    foreach (var kv in _supernovaTargetVfx)
    {
        if (!kv.Value.handle.IsValid()) continue;
        var go = kv.Value.handle.Result;
        if (go != null) go.transform.SetParent(parkingHolder);
    }
}
```

> Addressable handle은 그대로 유지. GO만 reparent. `ReleaseInstance`는 호출하지 않음.

### 3-2. HandlePhaseChanged — Combat 케이스 수정

```csharp
case GamePhase.Combat:
    DetachSupernovaTargetVfx(_unitViewManager.VfxParkingHolder);  // 추가
    _unitViewManager.OnCombatStart();
    _combatViewManager.OnCombatStart();
    // ... 나머지 기존 코드 동일
```

### 3-3. HandleCombatViewCreated — VFX GO reparent

기존 스케일만 복원하던 코드를 VFX GO reparent 포함으로 확장:
```csharp
private async void HandleCombatViewCreated(int entityId, UnitView view)
{
    _buffIconTracker?.RefreshIconsForUnit(view.CombatId);

    // 슈퍼노바 target VFX 검색
    float scale = 0f;
    AsyncOperationHandle<GameObject> handle = default;
    SkillPosition vfxPosition = SkillPosition.CUSTOM;

    foreach (var kv in _supernovaTargetVfx)
    {
        if (kv.Value.entityId != entityId) continue;
        scale = kv.Value.appliedScale;
        handle = kv.Value.handle;
        if (_synergyVfxConfig != null &&
            _synergyVfxConfig.TryGetTaggedVfx((SynergyType)kv.Key.traitId, SynergyVfxTag.TargetVfx, out var targetVfx))
            vfxPosition = targetVfx.Position;
        break;
    }

    // 슈퍼노바 VFX가 없으면 스킵
    if (!handle.IsValid()) return;

    await UniTask.WaitUntil(() => view == null || view.IsReady);
    if (view == null) return;

    // VFX GO를 새 전투 뷰에 reparent (Instantiate 없음)
    var go = handle.Result;
    if (go != null)
    {
        var parentTransform = vfxPosition != SkillPosition.CUSTOM
            ? view.GetSkillPositionTransform(vfxPosition)
            : view.transform;
        go.transform.SetParent(parentTransform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
    }

    if (scale > 0f) view.AddViewScale(scale, forceSet: true);
}
```

---

## IdleCombatViewBridge

**파일**: `Assets/_Project/Scripts/InGame_New/View/IdleCombatViewBridge.cs` (L57-61)

`HandleCombatStarted()`에서도 `_unitViewManager.OnCombatStart()`를 호출하므로, `OnCombatStart()` 변경이 자동 적용됨. 단, IdleCombat 경로에는 슈퍼노바 VFX가 없으므로 `DetachSupernovaTargetVfx` 호출은 불필요.

---

## 정리 타이밍

| 시점 | Persistent VFX | Addressable VFX (슈퍼노바) |
|------|---------------|--------------------------|
| 전투 시작 (`OnCombatStart`) | Detach → parking holder | Detach → parking holder |
| 전투 뷰 생성 | `ScheduleVfxReparent` → `AdoptPersistentVfx` | `HandleCombatViewCreated` → reparent |
| 전투 종료 (`OnCombatEnd`) | `ClearParkedVfx` (잔여 정리) | 기존 `RemoveTargetVfx`로 handle release |
| 유닛 판매/제거 | 기존 `Deactivate()` → `ClearPersistentVfx()` | 기존 `RemoveTargetVfx` |

---

## 수정 파일 요약

| 파일 | 변경 | 크기 |
|------|------|------|
| **UnitView.cs** | `_persistentVfxPositions`, `DetachPersistentVfx()`, `AdoptPersistentVfx()`, `DeactivateWithParking()`, `ClearPersistentVfx` 수정 | ~40줄 추가 |
| **UnitViewManager.cs** | parking holder, `_parkedPersistentVfx`, `OnCombatStart` parking, `ScheduleVfxReparent()`, `ClearParkedVfx()`, `OnCombatEnd` 수정, using 추가 | ~40줄 추가 |
| **AutoChessViewBridge.cs** | `DetachSupernovaTargetVfx()`, `HandlePhaseChanged` 수정, `HandleCombatViewCreated` reparent | ~40줄 수정 |

**신규 파일 없음.**

---

## 엣지 케이스

1. **IsReady 전 reparent 불가**: `WaitUntil(IsReady)` → SkillTopFXTransform 등 존재 보장
2. **전투 중 사망으로 풀 반환**: `Deactivate()` → `ClearPersistentVfx()` → 일반 파괴 경로 (정상)
3. **parking holder 비활성화**: `SetActive(false)`로 파킹된 VFX의 파티클 시스템 자동 중단, reparent 후 활성 부모 아래서 자동 재개
4. **localPosition/Rotation 리셋**: reparent 시 `Vector3.zero`/`Quaternion.identity` — `Instantiate(prefab, parent)` 동작과 동일
5. **Addressable handle 유효성**: reparent은 handle과 무관. `SetParent()`만 호출하므로 handle은 그대로 유효
6. **parked VFX 미소비**: 전투 뷰가 생성되지 않은 유닛(사망 등)의 VFX는 `ClearParkedVfx()`에서 정리

## Phase A 검증 방법

1. 준비 페이즈에서 PersistentVfx 있는 유닛(루키다 등) 배치
2. 전투 시작 → VFX가 전투 뷰에서 보이는지 확인 (**새로 생성되지 않고 기존 GO가 reparent**)
3. 전투 종료 → parked VFX가 정리되는지 확인
4. 슈퍼노바 스케일 + VFX가 전투 뷰에서 유지되는지 확인
5. 유닛 판매 시 기존 Deactivate 경로가 정상 작동하는지 확인
6. 연속 라운드 반복 시 메모리 누수 없는지 확인

---

# Phase B: 전투 중 VFX 풀링 (후속 작업)

## 현황 분석

현재 전투 중 VFX는 모두 `Instantiate()` → `Destroy(go, 3f)` 패턴. 전투가 격렬해지면 GC 압력이 높아짐.

### VFX 종류별 생성/파괴 방식

| 종류 | 생성 | 파괴 | 빈도 | 풀링 효과 |
|------|------|------|------|----------|
| **피격 VFX** (`_hitVfxPrefab`) | `Instantiate` (CombatViewManager L218, L695) | `Destroy(go, 3f)` | 피격당 1회 | **높음** — 동일 프리팹 반복 |
| **스킬 VFX** (`SpawnSkillVfxCore`) | `Instantiate` (L608-619) | `Destroy(go, 3f)` | 스킬당 1-2회 | 중간 — 프리팹 다양 |
| **투사체 VFX** (`CreateVfx`) | `Instantiate` (L947-963) | `Object.Destroy` 즉시 (L972-983) | 투사체당 1회 | **높음** — 고주파 |
| **상태이상 OneShot** | `Addressables.InstantiateAsync` (CombatVfxManager L116-145) | 3초 후 `ReleaseInstance` | 버프 적용당 1회 | 중간 |
| **상태이상 Loop** | `Addressables.InstantiateAsync` (CombatVfxManager L149-188) | 효과 해제 시 `ReleaseInstance` | 버프 해제당 1회 | 낮음 — 장시간 유지 |

### 기존 풀링 패턴 (참고용)

| 풀 | 파일 | 방식 |
|----|------|------|
| `InGameVfxMovementPool` | `InGame/VFX/InGameVfxMovement/InGameVfxMovementPool.cs` | `LinkedPool<T>` 제네릭 |
| `TileEffectManager` | `InGame_New/View/Board/Effect/TileEffectManager.cs` | `ObjectPool<GO>` + Addressables |

## 풀링 대상 (우선순위순)

### B-1. Fire-and-forget VFX 풀 (우선순위: 높음)

**대상**: `SpawnSkillVfxCore()`의 `Destroy(go, FireAndForgetLifetime)` → 풀 반환으로 교체

**방식**: prefab 키 기반 `Dictionary<GameObject, ObjectPool<GameObject>>` 풀
- `Get(prefab)`: 풀에서 꺼내거나 Instantiate → `SetActive(true)` + ParticleSystem replay
- `Return(go)`: `SetActive(false)` → 풀에 반환 (3초 타이머 대신)

**영향 범위**:
- `CombatViewManager.SpawnSkillVfxCore()` (L608-619)
- `CombatViewManager.SpawnFireAndForgetVfx()` (L677-682)
- 피격 VFX: `OnUnitDamaged()` L218, `ExecuteMeleeHit()` L695

### B-2. 투사체 VFX 풀 (우선순위: 높음)

**대상**: `CreateVfx()` L947 → `Object.Destroy()` L972 → 풀 반환

**방식**: 투사체 타입별 풀 (prefab 키)
- `Get`: InGameVfx.Clear() + 재초기화
- `Return`: ParticleSystem Stop + Trail Clear + `SetActive(false)`

**주의**: 트레일 페이드아웃 완료 후 반환해야 시각적 이상 없음 (기존 `_dyingProjectiles` 패턴 참고)

### B-3. Addressable OneShot VFX 캐싱 (우선순위: 중간)

**대상**: `CombatVfxManager.SpawnOneShotAsync()` L116-145

**방식**: `TileEffectManager`와 동일한 `ObjectPool<GO>` + Addressables 패턴
- 첫 로드: `Addressables.InstantiateAsync()`
- 이후: 풀에서 재사용

---

## Phase B 수정 파일 예상

| 파일 | 변경 |
|------|------|
| **VfxPool.cs** (신규) | prefab 키 기반 범용 VFX ObjectPool |
| **CombatViewManager.cs** | `SpawnSkillVfxCore`, `SpawnFireAndForgetVfx`, `CreateVfx`, `ReleaseVfx` → 풀 사용 |
| **CombatVfxManager.cs** | OneShot VFX → 풀 사용 |

> Phase B는 Phase A 완료 후 별도 작업으로 진행.
