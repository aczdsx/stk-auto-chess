# 빅마우스 3페이즈 틱 기반 대쉬 시스템

**Goal:** 빅마우스 스킬(240107002)의 돌진을 틱 기반 3페이즈(Rush→Overshoot→Return)로 구현. 이동 중 타일 단위 데미지+스턴, 타이밍/Ease는 레시피에서 설정.

**원본 참고:** `EffectCodeSkill240107002.cs`

---

## 아키텍처

```
레시피 (SkillFactory.Monster.cs)
  → DashForward(distance, power, cc, rushMs, rushEase, ...)
  → SkillAction에 타이밍/Ease/데미지/CC 정보 저장

SimSkillGeneric.ExecuteActionWithSpecialHandling
  → DashForward 감지 → 방향/거리/데미지 계산
  → DashSystem.StartDash(ref action, ...) 호출

DashSystem (순수 이동 로직, VFX 무관심)
  → StartDash: 레시피 ms → 프레임 변환, 첫 타일 이동
  → ProcessTick: 타일별 히트 + 페이즈 전환

SimSkillGeneric.OnChannelTick
  → DashPhase 변화 감지 → DispatchActionsForHitFrame
  → Execute2 → Vfx(복귀 포탈) 발동

CombatAISystem
  → MoveTimer 카운트다운, DashPhase != None이면 이동 완료 스킵

UnitViewManager
  → DashEase 기반 Ease 보간, Overshoot 오프셋
```

---

## 데이터 구조

### CombatUnit (2바이트만)
```csharp
public MoveEaseType DashEase;   // 현재 페이즈 Ease
public DashPhase DashPhase;     // None/Rush/Overshoot/Return
```

### SimSkillInstance (런타임 상태)
```csharp
// 런타임
public byte DashTilesRemaining;
public sbyte DashDirCol, DashDirRow;
public int DashHitDamage;
public short DashStunFrames;
// 레시피에서 변환된 프레임 값
public int DashFramesPerTile;
public byte DashOvershootFrames, DashReturnFrames;
// 레시피 Ease
public MoveEaseType DashOvershootEase, DashReturnEase;
```

### SkillAction (레시피 정의)
```csharp
public short DashRushMs, DashOvershootMs, DashReturnMs;
public MoveEaseType DashRushEase, DashOvershootEase, DashReturnEase;
```

### Enums
```csharp
public enum DashPhase : byte { None, Rush, Overshoot, Return }
public enum MoveEaseType : byte { None, OutQuad, Linear, OutExpo, InExpo }
```

---

## 레시피 예시 (빅마우스)

```csharp
Skill(240107002, E.Channeling, T.NearestEnemy)
    .On(Evt.Execute1)
        .Do(Vfx(0, V.AtTargetWithDir))
        .Do(Vfx(1, V.AtCasterWithDir))
        .Do(DashForward(3,
            power: Spec(1, 200f),
            cc: CrowdControlType.Stun,
            ccDuration: Spec(2, P.Frames, 3f),
            rushMs: 500, rushEase: MoveEaseType.OutQuad,
            overshootMs: 300, overshootEase: MoveEaseType.Linear,
            returnMs: 100, returnEase: MoveEaseType.InExpo))
    .On(Evt.Execute2)  // 오버슈트 진입 시 DashSystem이 페이즈 전환 → 자동 디스패치
        .Do(Vfx(0, V.AtCasterToSaved, vfxDirOffset: 18))
    .Register();
```

---

## 변경 파일

| 파일 | 역할 |
|------|------|
| `Enums.cs` | DashPhase enum, MoveEaseType enum |
| `Components.cs` | CombatUnit에 DashEase + DashPhase (2바이트) |
| `SimSkillBase.cs` | SimSkillInstance 대쉬 런타임 필드 + Reset() |
| `SkillRecipe.cs` | SkillAction에 DashRush/Overshoot/ReturnMs/Ease, VfxDirOffset |
| `SkillFactory.cs` | DashForward 팩토리 (타이밍/Ease 파라미터), Vfx에 vfxDirOffset |
| `SkillFactory.Monster.cs` | 빅마우스 레시피 |
| `DashSystem.cs` (신규) | 3페이즈 이동 시스템 (순수 로직, VFX 없음) |
| `SimSkillGeneric.cs` | ExecuteActionWithSpecialHandling DashForward 특수 처리 + 채널링 틱 페이즈 디스패치 |
| `CombatAISystem.cs` | DashPhase != None이면 이동 완료 스킵 |
| `ActionExecutor.cs` | DashForward case break, SpawnVfx에 VfxDirOffset 전달 |
| `UnitViewManager.cs` | ApplyEase + Overshoot 오프셋 보간 |
| `BoardWorldHelper.cs` | GridDirToWorld 유틸 |
| `SimulationEvents.cs` | SimEvent.VfxDirOffset, PushSkillPhaseVfx에 vfxDirOffset 파라미터 |
| `CombatViewManager.cs` | OnSkillPhaseVfx에서 VfxDirOffset 적용 |
| `AutoChessViewBridge.cs` | VfxDirOffset 전달 |
