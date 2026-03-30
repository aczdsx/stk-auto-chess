# JobPassive 시스템 ChampionSpec 분리 Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `JobPassiveParam0/1`을 `ChampionSpec`에서 제거하고, `ChampionPool`에 `PositionType → (param0, param1)` 룩업 테이블로 분리하여 직업군 패시브 데이터를 의미적으로 올바른 위치에 저장한다.

**Architecture:** `ChampionSpec`은 챔피언 개별 스탯만 보유. 직업군 공통 파라미터는 `ChampionPool.JobPassiveTable[]`에 `PositionType` 인덱스로 저장. `JobPassiveSystem`은 Pool 테이블에서 파라미터를 조회. `JobPassiveSystem.cs`는 기존 `Systems/` 폴더에서 `Traits/JobPassive/` 폴더로 이동하여 Trait 클래스들과 함께 응집.

**Tech Stack:** Unity C#, struct-based ECS-like simulation

---

## File Structure

| Action | Path | Responsibility |
|--------|------|---------------|
| Modify | `Simulation/Data/Components.cs` | `JobPassiveParams` struct 추가, `ChampionPool.JobPassiveTable` 추가, `ChampionSpec`에서 `JobPassiveParam0/1` 제거 |
| Move | `Simulation/Combat/Systems/JobPassiveSystem.cs` → `Simulation/Combat/Traits/JobPassive/JobPassiveSystem.cs` | 파일 이동 (Trait 클래스들과 응집) |
| Modify | `Simulation/Combat/Traits/JobPassive/JobPassiveSystem.cs` | Pool 테이블에서 파라미터 조회하도록 변경 |
| Modify | `Adapter/AutoChessSpecAdapter.cs` | `BuildJobPassiveTable()` 메서드 추가, `ChampionSpec` 생성에서 `JobPassiveParam0/1` 제거 |

**Base path:** `Assets/_Project/Scripts/InGame_New/`

---

## Chunk 1: 데이터 구조 변경 및 시스템 연결

### Task 1: Components.cs — JobPassiveParams 구조체 추가 및 ChampionPool 확장

**Files:**
- Modify: `Simulation/Data/Components.cs:246-248` (ChampionSpec에서 제거)
- Modify: `Simulation/Data/Components.cs:265-326` (ChampionPool에 테이블 추가)

- [ ] **Step 1: `JobPassiveParams` struct 추가**

`ChampionSpec` 위, 시너지 섹션 전에 추가:

```csharp
/// <summary>직업군 패시브 파라미터 (PositionType별 공통)</summary>
public struct JobPassiveParams
{
    public int Param0; // 주 계수 (확률%, 스택수, 쿨타임 등)
    public int Param1; // 부 계수 (데미지%, 충전 횟수 등)
}
```

- [ ] **Step 2: `ChampionSpec`에서 `JobPassiveParam0/1` 제거**

Components.cs:246-248의 다음 3줄 삭제:
```csharp
// 직업 패시브 파라미터 (SpecDataManager.GetJobPassiveList → SkillJob에서 추출)
public int JobPassiveParam0; // 주 계수 (확률%, 스택수, 쿨타임 프레임 등)
public int JobPassiveParam1; // 부 계수 (데미지%, 면역 지속 프레임 등)
```

- [ ] **Step 3: `ChampionPool`에 `JobPassiveTable` 필드 추가**

`ChampionPool` 클래스에 필드 추가:
```csharp
// 직업 패시브 파라미터 테이블 (인덱스 = CharacterPositionType)
public const int MaxPositionTypes = 16;
public JobPassiveParams[] JobPassiveTable;
```

- [ ] **Step 4: `ChampionPool.Create()`에서 테이블 초기화**

`Create()` 메서드의 pool 초기화에 추가:
```csharp
JobPassiveTable = new JobPassiveParams[MaxPositionTypes],
```

- [ ] **Step 5: 컴파일 확인**

이 시점에서 `JobPassiveParam0/1` 참조하는 곳(Adapter, JobPassiveSystem)에서 컴파일 에러 발생 예상. 다음 Task에서 수정.

---

### Task 2: AutoChessSpecAdapter — 테이블 빌드로 전환

**Files:**
- Modify: `Adapter/AutoChessSpecAdapter.cs:14-26` (InjectSpecs에서 테이블 주입)
- Modify: `Adapter/AutoChessSpecAdapter.cs:80-81` (ChampionSpec 생성에서 제거)
- Modify: `Adapter/AutoChessSpecAdapter.cs:134-152` (GetJobPassiveParam0/1 → BuildJobPassiveTable로 교체)

- [ ] **Step 1: ChampionSpec 생성에서 `JobPassiveParam0/1` 라인 제거**

AutoChessSpecAdapter.cs:80-81 삭제:
```csharp
                    JobPassiveParam0 = GetJobPassiveParam0(c.character_position_type),
                    JobPassiveParam1 = GetJobPassiveParam1(c.character_position_type),
```

- [ ] **Step 2: `GetJobPassiveParam0/1` 메서드 삭제**

AutoChessSpecAdapter.cs:134-152의 두 메서드와 주석 전부 삭제:
```csharp
// ── 직업 패시브 파라미터 추출 ──
/// <summary>직업 패시브 주 계수 ...</summary>
private static int GetJobPassiveParam0(...) { ... }
/// <summary>직업 패시브 부 계수 ...</summary>
private static int GetJobPassiveParam1(...) { ... }
```

- [ ] **Step 3: `BuildJobPassiveTable()` 메서드 추가**

같은 위치에 새 메서드:
```csharp
// ── 직업 패시브 테이블 ──

/// <summary>PositionType별 패시브 파라미터 테이블 구성</summary>
private static JobPassiveParams[] BuildJobPassiveTable()
{
    var table = new JobPassiveParams[ChampionPool.MaxPositionTypes];
    var specMgr = SpecDataManager.Instance;
    if (specMgr == null) return table;

    foreach (CharacterPositionType posType in System.Enum.GetValues(typeof(CharacterPositionType)))
    {
        if (posType == CharacterPositionType.NONE) continue;

        var passiveList = specMgr.GetJobPassiveList(posType);
        if (passiveList == null || passiveList.Count == 0) continue;

        var data = passiveList[0][0]; // grade 0
        table[(int)posType] = new JobPassiveParams
        {
            Param0 = (int)(data.passive_rate * 100),
            Param1 = (int)(data.passive_rate_2 * 100),
        };
    }

    return table;
}
```

- [ ] **Step 4: `InjectSpecs()`에서 테이블 주입**

`InjectSpecs()` 메서드에서 Pool 설정 후 테이블 연결:
```csharp
world.Pool.JobPassiveTable = BuildJobPassiveTable();
```

- [ ] **Step 5: 컴파일 확인**

Adapter 쪽 에러 해소. JobPassiveSystem 에러 남아있음.

---

### Task 3: JobPassiveSystem — Pool 테이블 조회로 전환 + 파일 이동

**Files:**
- Move: `Simulation/Combat/Systems/JobPassiveSystem.cs` → `Simulation/Combat/Traits/JobPassive/JobPassiveSystem.cs`
- Modify: 이동된 `JobPassiveSystem.cs:16-30` (SetupJobPassives 수정)

- [ ] **Step 1: `JobPassiveSystem.cs` 파일 이동**

```bash
git mv Assets/_Project/Scripts/InGame_New/Simulation/Combat/Systems/JobPassiveSystem.cs \
      Assets/_Project/Scripts/InGame_New/Simulation/Combat/Traits/JobPassive/JobPassiveSystem.cs
```

- [ ] **Step 2: `SetupJobPassives()` 수정 — Pool 테이블에서 조회**

기존 코드:
```csharp
public static void SetupJobPassives(CombatMatchState state, GameWorld world)
{
    if (world.Pool == null) return;

    for (int i = 0; i < state.UnitCount; i++)
    {
        ref var unit = ref state.Units[i];
        if (!unit.IsAlive) continue;

        var spec = FindSpec(world, unit.ChampionSpecId);
        if (spec.PositionType == 0) continue;

        AttachJobPassive(state, i, (CharacterPositionType)spec.PositionType,
            spec.JobPassiveParam0, spec.JobPassiveParam1, world.TickRate);
    }
}
```

변경:
```csharp
public static void SetupJobPassives(CombatMatchState state, GameWorld world)
{
    if (world.Pool?.JobPassiveTable == null) return;

    for (int i = 0; i < state.UnitCount; i++)
    {
        ref var unit = ref state.Units[i];
        if (!unit.IsAlive) continue;

        var spec = FindSpec(world, unit.ChampionSpecId);
        if (spec.PositionType == 0) continue;

        var jobParams = world.Pool.JobPassiveTable[spec.PositionType];
        AttachJobPassive(state, i, (CharacterPositionType)spec.PositionType,
            jobParams.Param0, jobParams.Param1, world.TickRate);
    }
}
```

- [ ] **Step 3: `FindSpec` 메서드가 더 이상 Pool 전체 순회 불필요한지 확인**

`FindSpec`은 여전히 `PositionType`을 가져오기 위해 필요하므로 유지.

- [ ] **Step 4: 전체 컴파일 확인**

모든 컴파일 에러 해소 확인.

- [ ] **Step 5: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs \
      Assets/_Project/Scripts/InGame_New/Adapter/AutoChessSpecAdapter.cs \
      Assets/_Project/Scripts/InGame_New/Simulation/Combat/Traits/JobPassive/JobPassiveSystem.cs \
      Assets/_Project/Scripts/InGame_New/Simulation/Combat/Systems/JobPassiveSystem.cs
git commit -m "refactor: JobPassive 파라미터를 ChampionSpec에서 ChampionPool 테이블로 분리"
```
