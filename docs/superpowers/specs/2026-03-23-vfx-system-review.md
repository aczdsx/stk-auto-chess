# VFX 시스템 코드 리뷰 보고서

> 2026-03-23 | Phase A (Reparenting) + Phase B (Pooling) 전체 점검

---

## Critical (즉시 수정 필요)

### 1. ReturnVfxAfterDelay — 전투 종료 후 double-return / destroyed GO push

**파일:** `CombatViewManager.cs` — `ReturnVfxAfterDelay()`

**문제:** `destroyCancellationToken`은 MonoBehaviour 파괴 시에만 취소됨. 전투 종료(`OnCombatEnd`)는 파괴가 아니므로 3초 딜레이가 전투 종료 후에도 계속 실행됨.

**시나리오:**
1. 전투 종료 → `ClearAllProjectiles()` 호출 → GO가 pool 반환 또는 Destroy됨
2. 3초 후 `ReturnVfxAfterDelay`가 같은 GO를 또 `_vfxPool.Return` → 스택에 2중 push
3. 다음 전투에서 `Get` 2회가 같은 인스턴스 반환 → 상태 오염

**수정안:**
```csharp
private CancellationTokenSource _combatCts;

public void OnCombatStart()
{
    _combatCts?.Dispose();
    _combatCts = new CancellationTokenSource();
    _isCombatActive = true;
}

public void OnCombatEnd()
{
    _combatCts?.Cancel();
    _combatCts?.Dispose();
    _combatCts = null;
    // ... 기존 코드
}

private async UniTaskVoid ReturnVfxAfterDelay(GameObject go, GameObject prefab, float delay)
{
    var ct = _combatCts?.Token ?? destroyCancellationToken;
    var canceled = await UniTask.Delay((int)(delay * 1000), cancellationToken: ct)
        .SuppressCancellationThrow();
    if (canceled || go == null) return;
    _vfxPool.Return(go, prefab);
}
```

### 2. VfxPool.Get — null GO 처리 dead code + while 루프 필요

**파일:** `VfxPool.cs` L26-32

**문제:** `if (go == null) go = null;`은 no-op. 스택에 파괴된 GO가 여러 개 있으면 1개만 버리고 나머지는 다음 Get에서 반복.

**수정안:**
```csharp
if (_pools.TryGetValue(key, out var stack))
{
    while (stack.Count > 0)
    {
        go = stack.Pop();
        if (go != null) break;
        go = null;
    }
}
```

---

## High (빠른 수정 필요)

### 3. ScheduleVfxReparent — destroyCancellationToken 미전달

**파일:** `UnitViewManager.cs` — `ScheduleVfxReparent()`

**문제:** async void에 취소 토큰 없음. UnitViewManager 파괴 후에도 실행되어 이미 Destroy된 GO를 AdoptPersistentVfx에 전달할 수 있음.

**수정안:**
```csharp
private async void ScheduleVfxReparent(int entityId, UnitView view)
{
    await UniTask.WaitUntil(
        () => view == null || view.IsReady,
        cancellationToken: destroyCancellationToken).SuppressCancellationThrow();
    if (this == null || view == null) return;
    // ...
}
```

### 4. HandleCombatViewCreated — destroyCancellationToken 미전달

**파일:** `AutoChessViewBridge.cs` — `HandleCombatViewCreated()`

**문제:** 동일 이슈. 씬 언로드/전투 종료 시 MissingReferenceException 가능.

**수정안:** `UniTask.WaitUntil`에 `cancellationToken: destroyCancellationToken` 추가.

### 5. OnProjectileExpired Movement==null 경로 — pool 대신 Destroy 사용

**파일:** `CombatViewManager.cs` L546-558

**문제:** Movement가 null인 투사체의 VFX가 `Object.Destroy`로 처리되어 풀로 반환되지 않음. `ReleaseVfx(ap)`를 호출하면 `SourcePrefab`이 있는 경우 풀로 반환됨.

**수정안:** 해당 블록을 `ReleaseProjectile(ap)` 호출로 통일.

### 6. Followable VFX 부모 스케일 간섭

**파일:** `VfxPool.cs` L40-48 / `CombatViewManager.cs` L616-624

**문제:** `VfxPool.Get`에서 `position` 설정 후 `SetParent(parent)` 호출 시 non-uniform scale 부모 아래에서 좌표/스케일 오염 가능. 기존 코드는 `Instantiate` 후 `SetParent`를 별도로 했음.

**수정안:** `VfxPool.Get`에서 Followable의 경우 `SetParent` 후 `localPosition = Vector3.zero`, `localRotation = Quaternion.identity`로 설정하는 옵션 추가, 또는 `SpawnSkillVfxCore`에서 부모를 null로 Get한 뒤 수동 SetParent.

---

## Medium (개선 권장)

### 7. ParkSynergyTargetVfx 후 reparent 시 파티클 미재생

**파일:** `AutoChessViewBridge.cs` — `HandleCombatViewCreated()` L1061-1069

**문제:** parking holder가 `SetActive(false)`이므로 파티클이 정지됨. reparent 후 활성 부모로 이동해도 파티클이 자동 재생되지 않음.

**수정안:** reparent 후 파티클 재생 코드 추가:
```csharp
var particles = go.GetComponentsInChildren<ParticleSystem>(true);
foreach (var ps in particles) { ps.Clear(); ps.Play(); }
```

### 8. VfxPool — GetComponentsInChildren 매번 GC 할당

**파일:** `VfxPool.cs` L88-113

**문제:** `ReplayParticles`, `StopParticles`, `ClearTrails`가 매 호출마다 배열 할당. 전투 중 고주파 호출 시 GC 압력.

**수정안:**
- `ActiveProjectile`의 `Particles`/`Trails` 캐시를 `Return` 시에도 전달하는 오버로드 추가
- 또는 풀 엔트리에 ParticleSystem[]/TrailRenderer[] 캐시를 함께 저장

### 9. OnCombatEnd에서 _vfxPool.Clear() 미호출

**파일:** `CombatViewManager.cs` L120-129

**문제:** 전투 간 풀 GO가 계속 누적. 장시간 플레이 시 메모리 증가.

**수정안:** `OnCombatEnd`에서 `_dyingProjectiles` 정리 + 선택적으로 `_vfxPool.Clear()` 호출. 또는 풀 크기 상한을 설정.

### 10. localScale 리셋 후 부모 스케일 간섭

**파일:** `VfxPool.cs` L41

**문제:** `go.transform.localScale = prefab.transform.localScale`로 리셋하지만, 부모 SetParent 후 lossyScale이 변경될 수 있음.

---

## Low (개선 가능)

### 11. Dead code: `if (go == null) go = null;`

**파일:** `VfxPool.cs` L30-31

### 12. DetachPersistentVfx null 반환 → 호출자 null 체크 중복

**파일:** `UnitView.cs` L521, `UnitViewManager.cs` L280

빈 리스트 대신 null 반환하여 호출자에서 `null && Count > 0` 이중 체크 필요.

---

## 요약

| 심각도 | 건수 | 즉시 수정 필요 |
|--------|------|---------------|
| Critical | 2 | O |
| High | 4 | O |
| Medium | 4 | 권장 |
| Low | 2 | 선택 |

**가장 즉각적인 수정:**
1. `ReturnVfxAfterDelay` 전투 종료 취소 토큰 (Critical #1)
2. `OnProjectileExpired` Movement==null 경로에서 `ReleaseProjectile` 사용 (High #5)
3. `ScheduleVfxReparent` + `HandleCombatViewCreated`에 `destroyCancellationToken` 추가 (High #3, #4)
