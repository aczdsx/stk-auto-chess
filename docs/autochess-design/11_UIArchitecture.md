# UI 아키텍처 설계

> 인게임 UI 레이아웃, HUD, Quantum→View 바인딩, 화면 전환을 정의한다.
>
> **GameMode 적용**: UI는 GameModeConfig에 따라 컴포넌트를 선택적으로 표시한다.
> - ClassicBattle: 상점패널/경제HUD/플레이어리스트/시너지패널 숨김. 보드+전투UI만 표시.
> - PvECampaign: 상점/경제/시너지 표시. 상대 플레이어 목록/관전 숨김.
> - Competitive: 모든 UI 컴포넌트 활성.
> View 레이어에서 `GameModeConfig` 플래그를 읽어 `SetActive()`로 분기한다.

---

## 1. UI 레이어 구조

```
기존 프로젝트의 SceneUILayerManager 체계를 활용하되,
Quantum View에 맞게 데이터 바인딩 방식을 변경한다.

┌──────────────────────────────────────┐
│ Cover Layer                          │  전체 화면 덮는 UI
│  - 로딩 화면, 게임 결과 화면           │
├──────────────────────────────────────┤
│ Overlay Layer                        │  상시 표시 HUD
│  - PhaseHUD, PlayerInfoBar           │
│  - MiniMap, TimerBar                 │
├──────────────────────────────────────┤
│ Popup Layer                          │  팝업 UI
│  - SynergyDetailPopup                │
│  - ItemDetailPopup                   │
│  - ChampionDetailPopup               │
│  - SettingsPopup                     │
├──────────────────────────────────────┤
│ Modal Layer                          │  모달 다이얼로그
│  - 확인/취소, 기권 확인               │
├──────────────────────────────────────┤
│ Game Layer (3D + UI)                 │  게임 보드, 유닛, 이펙트
│  - BoardView, UnitView, VFX          │
│  - ShopPanel (하단 고정)              │
│  - SynergyPanel (좌측 사이드)         │
│  - PlayerListPanel (우측/상단)        │
└──────────────────────────────────────┘
```

---

## 2. 인게임 화면 레이아웃

### 2.1 Preparation 페이즈 레이아웃

```
┌──────────────────────────────────────────────────┐
│ [타이머 30s]  Stage 2-3  Preparation   [설정 ⚙] │  ← 상단 바
├────┬─────────────────────────────────────┬───────┤
│    │                                     │ P1 ♥80│
│시  │                                     │ P2 ♥65│
│너  │         전투 영역 (7×4 그리드)       │ P3 ♥45│
│지  │         [유닛 배치된 보드]            │ P4 ♥30│
│패  │                                     │       │
│널  │                                     │ [관전]│
│    │                                     │       │
├────┼─────────────────────────────────────┼───────┤
│    │ ┌─┬─┬─┬─┬─┬─┬─┬─┬─┐               │       │
│    │ │ │ │ │ │ │ │ │ │ │ Bench (9칸)    │ 아이템│
│    │ └─┴─┴─┴─┴─┴─┴─┴─┴─┘               │ 인벤 │
├────┴─────────────────────────────────────┴───────┤
│ [🔒]  [챔피언A] [챔피언B] [챔피언C] [챔피언D] [챔피언E] │
│       [리롤 2G]                    [XP 구매 4G]   │
│ Gold: 23  Level: 5 (12/36 XP)     [레디 ✓]       │
└──────────────────────────────────────────────────┘
```

### 2.2 Combat 페이즈 레이아웃

```
┌──────────────────────────────────────────────────┐
│ [타이머 45s]  Stage 2-3  Combat        [설정 ⚙] │
├────┬─────────────────────────────────────┬───────┤
│    │    ┌─── 상대 유닛들 ───┐            │ P1 ♥80│
│시  │    │                   │            │ P2 ♥65│
│너  │    │   전투 영역 (8행)  │            │ P3 ♥45│
│지  │    │  유닛 자동 전투 중  │            │ P4 ♥30│
│패  │    │                   │            │       │
│널  │    └─── 아군 유닛들 ───┘            │[매치2]│
│    │                                     │[관전] │
├────┼─────────────────────────────────────┼───────┤
│    │         [커맨더 스킬1] [스킬2]       │ 아이템│
│    │                                     │ 인벤 │
├────┴─────────────────────────────────────┴───────┤
│         (상점 비활성 / 숨김)                       │
│ Gold: 23  Level: 5                               │
└──────────────────────────────────────────────────┘
```

### 2.3 Result 페이즈 레이아웃

```
┌──────────────────────────────────────────────────┐
│                    VICTORY / DEFEAT               │
│                                                   │
│            상대 플레이어: P3                        │
│            받은 데미지: -7 HP                      │
│                                                   │
│    ┌─────────────────────────────────────┐        │
│    │  HP: 65 → 58  ████████░░░           │        │
│    └─────────────────────────────────────┘        │
│                                                   │
│    [다음 라운드 준비 중... 5s]                      │
└──────────────────────────────────────────────────┘
```

---

## 3. 핵심 UI 컴포넌트

### 3.1 PhaseHUD (상단 바)

```
표시 정보:
  - 현재 페이즈 이름 (Preparation / Combat / Result)
  - 스테이지-라운드 번호 (Stage 2-3)
  - 남은 시간 (타이머)
  - 라운드 타입 (PvP / PvE / 캐러셀 아이콘)

데이터 소스:
  - f.Global->CurrentPhase
  - f.Global->CurrentStage, CurrentRound
  - f.Global->PhaseTimer
  - f.Global->CurrentRoundType
```

### 3.2 ShopPanel (하단 상점)

```
구성:
  - 5개 챔피언 슬롯 (드래그/탭으로 구매)
  - 리롤 버튼 (2골드)
  - XP 구매 버튼 (4골드)
  - 샵 잠금 토글
  - 골드/레벨/XP 표시

슬롯 표시:
  - 챔피언 초상화 + 이름
  - 코스트 (1~5골드, 등급 색상)
  - 특성 아이콘 (2~3개)
  - 보유 수량 표시 (같은 챔피언이 보드/벤치에 있으면 "2/3" 등)

상태별 표시:
  - Preparation: 활성 (구매/리롤 가능)
  - Combat: 비활성 or 축소 (구매 불가)
  - Result: 비활성

데이터 소스:
  - PlayerShop 컴포넌트 (5개 슬롯)
  - PlayerEconomy (골드, 레벨, XP)
```

### 3.3 SynergyPanel (좌측 사이드)

```
구성:
  - 활성 시너지 목록 (세로 스크롤)
  - 각 항목: 아이콘 + 이름 + 유닛수/필요수
  - 단계별 색상 (브론즈/실버/골드/크롬)
  - 탭 시 상세 팝업

정렬:
  1. 최대 단계 달성 → 상단
  2. 같은 단계면 유닛 많은 순
  3. 미달성(유닛 1개 이상) → 하단
  4. 유닛 0개 → 숨김

데이터 소스:
  - PlayerSynergy 컴포넌트 (TraitCounts, TraitTiers)
  - SynergySpecAsset (이름, 아이콘, 단계 정보)
```

### 3.4 PlayerListPanel (우측/상단)

```
구성:
  - 4인 플레이어 목록
  - 각 플레이어: 닉네임, HP바, 레벨, 연승/연패

표시:
  ┌───────────────┐
  │ ♛ Player1  L7 │  ← 1위 (HP 순)
  │ ████████░░ 80 │
  │ 🔥3연승       │
  ├───────────────┤
  │   Player2  L6 │
  │ ██████░░░░ 58 │
  ├───────────────┤
  │   Player3  L5 │
  │ ████░░░░░░ 38 │
  │ 💀2연패       │
  ├───────────────┤
  │ ☠ Player4     │  ← 탈락 (4위)
  │ ELIMINATED    │
  └───────────────┘

탭 시:
  - 해당 플레이어 보드 관전 모드
  - 시너지/배치 미리보기

정렬: HP 내림차순 (탈락자 최하단)

데이터 소스:
  - 각 PlayerState (HP, IsAlive, Rank)
  - PlayerEconomy (Level)
  - PlayerStreak (CurrentStreak)
```

### 3.5 ItemInventoryPanel

```
구성:
  - 10칸 아이템 슬롯 (벤치 아래 또는 우측)
  - 드래그로 유닛에 장착
  - 탭으로 상세 보기 + 조합 가이드

표시:
  - 기본 아이템: 단일 아이콘
  - 완성 아이템: 고급 프레임 + 아이콘
  - 빈 슬롯: 회색 테두리

데이터 소스:
  - PlayerItemInventory 컴포넌트
  - ItemSpecAsset (아이콘, 이름, 효과)
```

### 3.6 CommanderSkillPanel (전투 중)

```
전투 페이즈에서만 표시:

구성:
  - 2개 스킬 버튼 (하단 중앙)
  - 쿨타임 오버레이 (원형 게이지)
  - 남은 사용 횟수

조작:
  - 즉시 발동형: 버튼 탭
  - 타겟 지정형: 버튼 탭 → 전투장 위치 선택 → 확정

상태 표시:
  - 사용 가능: 밝은 아이콘, 글로우 효과
  - 쿨다운 중: 어두운 아이콘 + 남은 시간
  - 사용 불가: 회색 처리

데이터 소스:
  - CommanderSkillState (Cooldowns, UsesRemaining)
  - CommanderSkillSpecAsset (아이콘, 이름, 설명)
```

---

## 4. Quantum → View 데이터 바인딩

### 4.1 바인딩 패턴

```
Quantum 상태를 Unity View에 반영하는 2가지 패턴:

1. 폴링 (Polling) — 매 프레임 읽기
   - 자주 변하는 상태 (HP, 마나, 위치, 타이머)
   - QuantumCallback.Subscribe<CallbackUpdateView>에서 처리
   - Frame 객체에서 직접 컴포넌트 읽기

2. 이벤트 (Event) — 변경 시에만
   - 간헐적 변경 (시너지 갱신, 유닛 합성, 아이템 장착)
   - Quantum Event → Unity 이벤트 핸들러
   - 주요 변경에만 사용하여 성능 최적화
```

### 4.2 QuantumViewBridge

```csharp
public class QuantumViewBridge : MonoBehaviour
{
    // Quantum 콜백 등록
    private void OnEnable()
    {
        QuantumCallback.Subscribe<CallbackUpdateView>(this, OnUpdateView);
        QuantumEvent.Subscribe<EventPhaseChanged>(this, OnPhaseChanged);
        QuantumEvent.Subscribe<EventUnitCombined>(this, OnUnitCombined);
        QuantumEvent.Subscribe<EventSynergyUpdated>(this, OnSynergyUpdated);
        QuantumEvent.Subscribe<EventItemEquipped>(this, OnItemEquipped);
        QuantumEvent.Subscribe<EventItemCombined>(this, OnItemCombined);
        QuantumEvent.Subscribe<EventUnitAttacked>(this, OnUnitAttacked);
        QuantumEvent.Subscribe<EventUnitDied>(this, OnUnitDied);
        QuantumEvent.Subscribe<EventCombatResult>(this, OnCombatResult);
        QuantumEvent.Subscribe<EventPlayerEliminated>(this, OnPlayerEliminated);
        QuantumEvent.Subscribe<EventGameOver>(this, OnGameOver);
        QuantumEvent.Subscribe<EventCommanderSkillUsed>(this, OnCommanderSkillUsed);
        // ... 기타 이벤트
    }

    // 매 프레임 View 갱신
    private void OnUpdateView(CallbackUpdateView callback)
    {
        var frame = callback.Game.Frames.Verified;
        var localPlayer = callback.Game.GetLocalPlayerRef();

        // HUD 갱신 (폴링)
        UpdatePhaseHUD(frame);
        UpdatePlayerListPanel(frame);
        UpdateShopPanel(frame, localPlayer);
        UpdateGoldDisplay(frame, localPlayer);

        // 전투 중이면 유닛 위치 보간
        if (frame.Global->CurrentPhase == GamePhase.Combat)
        {
            UpdateCombatUnitPositions(frame);
        }
    }
}
```

### 4.3 유닛 View 동기화

```csharp
public class UnitViewManager : MonoBehaviour
{
    private Dictionary<EntityRef, UnitView> _unitViews = new();
    private BoardLODManager _lodManager;

    // 4인 플레이어 전체 보드 동기화
    // Active 보드: 풀 보간 + UI 갱신
    // Inactive 보드: Transform 위치만 갱신 (보간 없음)
    public void SyncAllBoards(Frame frame)
    {
        for (int p = 0; p < frame.PlayerCount; p++)
        {
            bool isActive = _lodManager.IsActiveBoard(p);
            var board = GetPlayerBoard(frame, (PlayerRef)p);
            int boardIndex = p;

            // 보드 유닛
            for (int i = 0; i < 28; i++)
            {
                if (board->Tiles[i] == EntityRef.None)
                {
                    HideUnitView(boardIndex, i);
                    continue;
                }

                var unitData = frame.Get<UnitData>(board->Tiles[i]);
                var view = GetOrCreateUnitView(board->Tiles[i], unitData);

                // 월드 위치 = 보드 오프셋 + 타일 위치
                Vector3 worldPos = BoardWorldHelper.GridToWorldPosition(boardIndex,
                    i % 7, i / 7);

                if (isActive)
                {
                    view.UpdatePosition(worldPos);  // 부드러운 보간
                    view.UpdateStarLevel(unitData->StarLevel);
                    view.UpdateItems(frame, unitData);
                }
                else
                {
                    view.SetPositionImmediate(worldPos);  // 즉시 배치 (보간 없음)
                }
            }

            // 벤치 유닛
            for (int i = 0; i < 9; i++)
            {
                if (board->Bench[i] == EntityRef.None)
                {
                    HideBenchView(boardIndex, i);
                    continue;
                }

                var unitData = frame.Get<UnitData>(board->Bench[i]);
                var view = GetOrCreateUnitView(board->Bench[i], unitData);
                Vector3 benchPos = BoardWorldHelper.BenchToWorldPosition(boardIndex, i);

                if (isActive)
                {
                    view.UpdatePosition(benchPos);
                    view.UpdateStarLevel(unitData->StarLevel);
                }
                else
                {
                    view.SetPositionImmediate(benchPos);
                }
            }
        }
    }

    // 전투 유닛 보간 (매 프레임)
    // Active 보드의 유닛만 풀 보간, 나머지는 위치만 갱신
    public void SyncCombatUnits(Frame frame)
    {
        var filter = frame.Filter<CombatUnit>();
        while (filter.NextUnsafe(out var entity, out var unit))
        {
            var view = GetOrCreateCombatUnitView(entity, unit);
            int boardIndex = GetBoardIndexForUnit(frame, unit);
            bool isActive = _lodManager.IsActiveBoard(boardIndex);

            // 그리드 좌표 → 월드 좌표
            Vector3 targetPos = BoardWorldHelper.GridToWorldPosition(
                boardIndex, unit->GridCol, unit->GridRow);

            if (isActive)
            {
                // 풀 보간 + UI 갱신
                view.transform.position = Vector3.Lerp(
                    view.transform.position, targetPos, Time.deltaTime * 15f);
                view.UpdateHP(unit->CurrentHP, unit->MaxHP);
                view.UpdateMana(unit->CurrentMana, unit->MaxMana);
                view.UpdateState(unit->State);
            }
            else
            {
                // 위치만 즉시 갱신 (HP바 등 UI 스킵)
                view.transform.position = targetPos;
            }
        }
    }
}
```

---

## 5. 입력 → Command 변환

### 5.1 InputCommandBridge

```csharp
public class InputCommandBridge : MonoBehaviour
{
    private QuantumGame _game;

    // 샵 구매
    public void OnBuySlotClicked(int slotIndex)
    {
        var cmd = new BuyUnitCommand { ShopSlotIndex = (byte)slotIndex };
        _game.SendCommand(cmd);
    }

    // 리롤
    public void OnRerollClicked()
    {
        _game.SendCommand(new RerollShopCommand());
    }

    // XP 구매
    public void OnBuyXPClicked()
    {
        _game.SendCommand(new BuyXPCommand());
    }

    // 유닛 드래그 완료
    public void OnUnitDragEnd(EntityRef unit, Vector3 worldPos, DropZone zone)
    {
        switch (zone)
        {
            case DropZone.Board:
                var (col, row) = WorldToGrid(worldPos);
                _game.SendCommand(new PlaceUnitCommand
                {
                    UnitEntity = unit,
                    TargetCol = (byte)col,
                    TargetRow = (byte)row
                });
                break;

            case DropZone.Bench:
                _game.SendCommand(new WithdrawUnitCommand
                {
                    UnitEntity = unit,
                    TargetBenchSlot = 0xFF // 자동 배정
                });
                break;

            case DropZone.UnitSwap:
                var targetUnit = GetUnitAtPosition(worldPos);
                _game.SendCommand(new SwapUnitsCommand
                {
                    UnitA = unit,
                    UnitB = targetUnit
                });
                break;

            case DropZone.Sell:
                _game.SendCommand(new SellUnitCommand
                {
                    UnitEntity = unit
                });
                break;
        }
    }

    // 아이템 드래그 완료
    public void OnItemDragEnd(EntityRef item, EntityRef targetUnit)
    {
        _game.SendCommand(new EquipItemCommand
        {
            ItemEntity = item,
            TargetUnit = targetUnit
        });
    }

    // 커맨더 스킬
    public void OnCommanderSkillUsed(int skillIndex, Vector3 worldPos)
    {
        var fpPos = UnityToFPPosition(worldPos);
        _game.SendCommand(new UseCommanderSkillCommand
        {
            SkillIndex = skillIndex,
            TargetPos = fpPos
        });
    }

    // 레디
    public void OnReadyClicked()
    {
        _game.SendCommand(new ReadyCommand());
    }
}
```

---

## 6. VFX & 연출

### 6.1 이벤트 기반 VFX

```csharp
public class CombatVFXManager : MonoBehaviour
{
    private void OnEnable()
    {
        // 공격
        QuantumEvent.Subscribe<EventUnitAttacked>(this, OnUnitAttacked);
        QuantumEvent.Subscribe<EventUnitDied>(this, OnUnitDied);
        QuantumEvent.Subscribe<EventUnitCastSkill>(this, OnSkillCast);
        QuantumEvent.Subscribe<EventSkillHit>(this, OnSkillHit);
        QuantumEvent.Subscribe<EventUnitCombined>(this, OnCombine);
        QuantumEvent.Subscribe<EventCommanderSkillUsed>(this, OnCommanderSkill);

        // 투사체
        QuantumEvent.Subscribe<EventProjectileSpawned>(this, OnProjectileSpawned);
        QuantumEvent.Subscribe<EventProjectileHit>(this, OnProjectileHit);
        QuantumEvent.Subscribe<EventProjectileMoved>(this, OnProjectileMoved);
        QuantumEvent.Subscribe<EventProjectileExpired>(this, OnProjectileExpired);
        QuantumEvent.Subscribe<EventProjectileExploded>(this, OnProjectileExploded);
    }

    private void OnUnitAttacked(EventUnitAttacked e)
    {
        var attackerView = GetUnitView(e.Attacker);
        var targetView = GetUnitView(e.Target);

        // 공격 애니메이션 (Spine)
        attackerView.PlayAttackAnimation();

        if (e.IsProjectile)
        {
            // 원거리 → Homing 투사체 스폰 (데미지/히트는 ProjectileHit에서 처리)
            SpawnHomingProjectile(attackerView, targetView, e.ProjectileSpecId);
        }
        else
        {
            // 근접 → 즉시 히트 이펙트 + 데미지 텍스트
            targetView.PlayHitEffect();
            ShowDamageText(targetView.transform.position, e.Damage, e.IsCrit);
        }
    }

    private void OnProjectileSpawned(EventProjectileSpawned e)
    {
        switch (e.Type)
        {
            case ProjectileType.Linear:
                var sourceView = GetUnitView(e.Source);
                _projectileViewManager.SpawnLinearProjectile(
                    sourceView.transform.position,
                    e.DirCol, e.DirRow, e.MaxDistance,
                    e.ProjectileSpecId);
                break;

            case ProjectileType.AreaTarget:
                var casterView = GetUnitView(e.Source);
                var targetWorldPos = GridToWorldPosition(e.TargetCol, e.TargetRow);
                _projectileViewManager.SpawnAreaProjectile(
                    casterView.transform.position,
                    targetWorldPos,
                    e.RemainingFrames,
                    e.ProjectileSpecId);
                break;

            // Homing은 OnUnitAttacked에서 SpawnHomingProjectile로 처리
        }
    }

    private void OnProjectileHit(EventProjectileHit e)
    {
        var targetView = GetUnitView(e.Target);
        if (targetView == null) return;

        // 투사체 도착 → 히트 이펙트 + 데미지 텍스트
        targetView.PlayHitEffect();
        ShowDamageText(targetView.transform.position, e.Damage, e.IsCrit);
    }

    private void OnProjectileExploded(EventProjectileExploded e)
    {
        // 범위 폭발 VFX
        var worldPos = GridToWorldPosition(e.TargetCol, e.TargetRow);
        PlayAreaExplosionVFX(worldPos, e.Radius, e.ProjectileSpecId);
    }

    private void OnUnitDied(EventUnitDied e)
    {
        var view = GetUnitView(e.Unit);
        view.PlayDeathAnimation(() =>
        {
            Destroy(view.gameObject);
        });
    }

    private void OnCombine(EventUnitCombined e)
    {
        var view = GetUnitView(e.ResultUnit);
        PlayCombineVFX(view.transform.position, e.NewStarLevel);
        view.UpdateStarLevel(e.NewStarLevel);
    }
}
```

### 6.2 투사체 View 시스템

```csharp
/// <summary>
/// 시뮬레이션의 ProjectileSystem과 동기화되는 View 레이어 투사체 관리.
/// 시뮬레이션에서 데미지 판정, View에서는 비주얼만 담당.
/// </summary>
public class ProjectileViewManager : MonoBehaviour
{
    [SerializeField] private ProjectileVFXDatabase _vfxDatabase;

    // 활성 투사체 View 추적
    private readonly List<ProjectileView> _activeProjectiles = new();

    /// <summary>Homing 투사체: 타겟을 추적하며 비행</summary>
    public void SpawnHomingProjectile(UnitView attacker, UnitView target,
        int projectileSpecId)
    {
        var prefab = _vfxDatabase.GetPrefab(projectileSpecId);
        var proj = Instantiate(prefab, attacker.ProjectileSpawnPoint.position,
            Quaternion.identity);
        var view = proj.GetComponent<ProjectileView>();

        // 비행 시간은 시뮬레이션과 동일하게 계산
        // (거리 × framesPerTile / 30fps)
        float dist = Vector3.Distance(
            attacker.transform.position, target.transform.position);
        float duration = dist * 0.033f * 4; // 4프레임/칸 기본값

        view.InitHoming(target.transform, duration, () =>
        {
            // 도착 시 자동 제거 (히트 이펙트는 ProjectileHit 이벤트에서 처리)
            _activeProjectiles.Remove(view);
            Destroy(proj);
        });

        _activeProjectiles.Add(view);
    }

    /// <summary>Linear 투사체: 직선으로 이동하며 관통</summary>
    public void SpawnLinearProjectile(Vector3 startPos, int dirCol, int dirRow,
        int projectileSpecId)
    {
        var prefab = _vfxDatabase.GetPrefab(projectileSpecId);
        var proj = Instantiate(prefab, startPos, Quaternion.identity);
        var view = proj.GetComponent<ProjectileView>();

        Vector3 worldDir = GridDirToWorldDir(dirCol, dirRow);
        view.InitLinear(worldDir);

        _activeProjectiles.Add(view);
    }

    /// <summary>AreaTarget 투사체: 목표 위치로 비행 후 폭발</summary>
    public void SpawnAreaProjectile(Vector3 startPos, int targetCol, int targetRow,
        float duration, int projectileSpecId)
    {
        var prefab = _vfxDatabase.GetPrefab(projectileSpecId);
        var proj = Instantiate(prefab, startPos, Quaternion.identity);
        var view = proj.GetComponent<ProjectileView>();

        Vector3 targetPos = GridToWorldPosition(targetCol, targetRow);
        view.InitAreaTarget(targetPos, duration, () =>
        {
            _activeProjectiles.Remove(view);
            Destroy(proj);
        });

        _activeProjectiles.Add(view);
    }
}
```

### 6.3 ProjectileView 컴포넌트

```csharp
/// <summary>
/// 개별 투사체의 비주얼 이동을 담당.
/// 시뮬레이션 이벤트(ProjectileMoved 등)로 위치 동기화.
/// </summary>
public class ProjectileView : MonoBehaviour
{
    [SerializeField] private ParticleSystem _trailFX;
    [SerializeField] private ParticleSystem _hitFX;

    private MotionHandle _motionHandle;

    /// <summary>Homing: 타겟 Transform을 향해 LitMotion 보간 이동</summary>
    public void InitHoming(Transform target, float duration, Action onArrive)
    {
        // LitMotion으로 타겟 추적 보간
        var startPos = transform.position;
        _motionHandle = LMotion.Create(0f, 1f, duration)
            .WithEase(Ease.Linear)
            .Bind(t =>
            {
                if (target != null)
                    transform.position = Vector3.Lerp(startPos, target.position, t);
            });

        // 도착 시 콜백
        LMotion.Create(0f, 1f, duration)
            .WithOnComplete(onArrive)
            .RunWithoutBinding();
    }

    /// <summary>Linear: 방향으로 등속 이동 (위치는 시뮬레이션 이벤트로 보정)</summary>
    public void InitLinear(Vector3 worldDir)
    {
        // 매 프레임 등속 이동 (Update에서 처리)
        _moveDir = worldDir.normalized;
        _isLinear = true;
    }

    /// <summary>AreaTarget: 목표 위치로 포물선/직선 비행</summary>
    public void InitAreaTarget(Vector3 targetPos, float duration, Action onArrive)
    {
        var startPos = transform.position;
        var midPoint = (startPos + targetPos) / 2f + Vector3.up * 2f; // 포물선

        _motionHandle = LMotion.Create(0f, 1f, duration)
            .WithEase(Ease.InQuad)
            .Bind(t =>
            {
                // 3점 베지어 곡선
                var a = Vector3.Lerp(startPos, midPoint, t);
                var b = Vector3.Lerp(midPoint, targetPos, t);
                transform.position = Vector3.Lerp(a, b, t);
            });

        LMotion.Create(0f, 1f, duration)
            .WithOnComplete(onArrive)
            .RunWithoutBinding();
    }

    /// <summary>시뮬레이션 ProjectileMoved 이벤트로 위치 보정 (Linear)</summary>
    public void SyncPosition(Vector3 gridWorldPos)
    {
        // 현재 위치 → 새 그리드 위치로 보간
        var from = transform.position;
        _motionHandle.TryCancel();
        _motionHandle = LMotion.Create(from, gridWorldPos, 0.1f)
            .WithEase(Ease.Linear)
            .BindToPosition(transform);
    }

    private void OnDestroy()
    {
        _motionHandle.TryCancel();
        if (_trailFX != null) _trailFX.Stop();
    }
}
```

### 6.4 투사체 VFX 데이터베이스

```csharp
/// <summary>
/// ProjectileSpecId → VFX 프리팹 매핑.
/// Addressables로 로드하거나 직접 참조.
/// </summary>
[CreateAssetMenu(menuName = "AutoChess/VFX/ProjectileVFXDatabase")]
public class ProjectileVFXDatabase : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public int ProjectileSpecId;
        public GameObject Prefab;       // ProjectileView 컴포넌트 포함
        public float Scale;             // VFX 스케일 조절
    }

    [SerializeField] private Entry[] _entries;

    public GameObject GetPrefab(int specId)
    {
        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].ProjectileSpecId == specId)
                return _entries[i].Prefab;
        }
        return _defaultPrefab; // 폴백
    }
}
```

### 6.5 LitMotion 트윈 활용

```
View에서의 모든 트윈 애니메이션은 LitMotion 사용:

사용처:
  - 데미지 텍스트 팝업 (Scale + Fade + 이동)
  - HP바 감소 애니메이션
  - 마나 게이지 충전
  - 유닛 합성 이펙트 (Scale Punch)
  - 페이즈 전환 배너 (Slide In/Out)
  - 골드 변경 텍스트 (+3, -2)
  - 아이템 장착 이펙트 (Glow + Scale)
  - 관전 전환 (카메라 위치 트윈, §7.5 SpectateSystem)
  - 투사체 비행 (Homing 추적, Linear 등속, AreaTarget 포물선)

주의:
  - 인게임에서는 .WithInGameScheduler() 사용하지 않음
    → Quantum 시뮬레이션과 별도 (View 전용 트윈)
  - 일반 LitMotion 사용으로 충분
  - 일시정지 시 Time.timeScale = 0 대응만 필요
```

---

## 7. 화면 전환

### 7.1 페이즈별 UI 전환

```
PhaseChanged 이벤트 수신 시:

Preparation:
  1. 상점 패널 활성화 (Slide Up)
  2. 벤치 영역 표시
  3. 드래그&드롭 입력 활성화
  4. 커맨더 스킬 버튼 숨김
  5. 전투 결과 배너 숨김

Combat:
  1. 상점 패널 숨김 or 최소화 (Slide Down)
  2. 전투 보드 전환 (미러링된 8행 보드)
  3. 드래그&드롭 비활성화 (배치 변경 불가)
  4. 아이템 드래그는 유지 (전투 중 장착 가능)
  5. 커맨더 스킬 버튼 표시
  6. 관전 전환 버튼 표시

Result:
  1. 결과 배너 표시 (승리/패배 + 데미지)
  2. HP 변경 애니메이션
  3. 탈락 플레이어 연출 (있을 경우)
  4. 자동 전환 대기 (5초)
```

### 7.2 보드 월드 배치 전략

```
4인 플레이어의 보드를 모두 월드에 배치하고, 카메라 이동으로 전환한다.
오브젝트를 매번 생성/파괴하지 않는다.

이유:
  - 보드 전환이 즉시 가능 (카메라 팬만으로 0.3초 전환)
  - 스폰/디스폰 지연 없음 (모바일에서 GC 스파이크 방지)
  - 4인 × 최대 16유닛 = 64유닛은 모바일에서 충분히 감당 가능
  - Quantum 시뮬레이션은 어차피 전체 보드를 동시에 처리

월드 배치도 (탑다운 뷰):

  ┌──────────┐  ┌──────────┐
  │  P1 보드  │  │  P2 보드  │
  │  (0, 0)  │  │ (20, 0)  │
  └──────────┘  └──────────┘

  ┌──────────┐  ┌──────────┐
  │  P3 보드  │  │  P4 보드  │
  │  (0,-20) │  │ (20,-20) │
  └──────────┘  └──────────┘

  보드 간격: 20 유닛 (카메라 FOV 밖으로 충분히 이격)
  각 보드: 7열 × 8행 그리드 + 벤치 영역
```

### 7.3 보드별 LOD (Level of Detail) 관리

```
현재 보고 있는 보드만 풀 퀄리티, 나머지는 경량화:

Active Board (카메라가 보고 있는 보드):
  - Spine 애니메이션: 풀 프레임 재생
  - 파티클 시스템: 활성 (공격, 스킬, 투사체 VFX)
  - UI 요소: 활성 (HP바, 마나바, 데미지 텍스트, 스타 아이콘)
  - 투사체 View: 활성 (ProjectileViewManager 처리)
  - 보드 그리드 시각 효과: 활성 (배치 하이라이트 등)

Inactive Board (카메라 밖 보드):
  - Spine 애니메이션: 일시정지 (SkeletonAnimation.enabled = false)
  - 파티클 시스템: 일시정지 (ParticleSystem.Pause + Clear)
  - UI 요소: 비활성 (Canvas.enabled = false)
  - 투사체 View: 비활성 (스폰하지 않음)
  - Transform 위치만 Quantum 동기화 (좌표값만 갱신, 보간 없음)

전환 시 처리:
  1. 이전 보드 → Inactive 전환 (경량화)
  2. 대상 보드 → Active 전환 (풀 퀄리티 복원)
  3. 전환 시간 ~1프레임 (비활성화/활성화만)
```

### 7.4 BoardLODManager

```csharp
public class BoardLODManager : MonoBehaviour
{
    [SerializeField] private float _boardSpacing = 20f;

    // 플레이어별 보드 루트 오브젝트
    private BoardViewRoot[] _boardRoots;  // [0]=P1, [1]=P2, [2]=P3, [3]=P4
    private int _activeBoardIndex = -1;

    /// <summary>플레이어별 보드 월드 위치 계산</summary>
    public Vector3 GetBoardWorldOrigin(int playerIndex)
    {
        int col = playerIndex % 2;  // 0 or 1
        int row = playerIndex / 2;  // 0 or 1
        return new Vector3(col * _boardSpacing, 0f, -row * _boardSpacing);
    }

    /// <summary>보드 전환 (관전 또는 자기 보드로 복귀)</summary>
    public void SwitchToBoard(int playerIndex)
    {
        if (_activeBoardIndex == playerIndex) return;

        // 이전 보드 비활성화
        if (_activeBoardIndex >= 0)
        {
            SetBoardLOD(_boardRoots[_activeBoardIndex], BoardLOD.Inactive);
        }

        // 새 보드 활성화
        _activeBoardIndex = playerIndex;
        SetBoardLOD(_boardRoots[playerIndex], BoardLOD.Active);
    }

    private void SetBoardLOD(BoardViewRoot root, BoardLOD lod)
    {
        bool active = lod == BoardLOD.Active;

        // Spine 애니메이션
        foreach (var spine in root.SpineAnimations)
            spine.enabled = active;

        // 파티클 시스템
        foreach (var ps in root.ParticleSystems)
        {
            if (active) ps.Play();
            else { ps.Stop(); ps.Clear(); }
        }

        // UI Canvas (HP바, 마나바 등)
        root.UnitUICanvas.enabled = active;

        // 투사체 매니저
        root.ProjectileViewManager.enabled = active;
    }

    private enum BoardLOD { Active, Inactive }
}
```

### 7.5 관전 시스템

```csharp
public class SpectateSystem : MonoBehaviour
{
    [SerializeField] private BoardLODManager _lodManager;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private float _transitionDuration = 0.3f;

    private int _localPlayerIndex;
    private int _spectatingIndex;
    private bool _isSpectating;
    private MotionHandle _cameraMotion;

    /// <summary>다른 플레이어 보드 관전 시작</summary>
    public void SpectatePlayer(int targetPlayerIndex)
    {
        if (targetPlayerIndex == _spectatingIndex) return;

        _isSpectating = targetPlayerIndex != _localPlayerIndex;
        _spectatingIndex = targetPlayerIndex;

        // LOD 전환
        _lodManager.SwitchToBoard(targetPlayerIndex);

        // 카메라 이동 (LitMotion)
        Vector3 targetCamPos = _lodManager.GetBoardWorldOrigin(targetPlayerIndex)
            + _cameraOffset;

        _cameraMotion.TryCancel();
        _cameraMotion = LMotion.Create(
                _mainCamera.transform.position, targetCamPos, _transitionDuration)
            .WithEase(Ease.OutCubic)
            .BindToPosition(_mainCamera.transform);

        // 관전 UI 표시
        if (_isSpectating)
            ShowSpectateBar(targetPlayerIndex);
        else
            HideSpectateBar();
    }

    /// <summary>자기 보드로 복귀</summary>
    public void ReturnToOwnBoard()
    {
        SpectatePlayer(_localPlayerIndex);
    }

    /// <summary>다음/이전 플레이어로 순환</summary>
    public void SpectateNext()
    {
        int next = (_spectatingIndex + 1) % 4;
        // 탈락한 플레이어는 스킵
        while (!IsPlayerAlive(next) && next != _spectatingIndex)
            next = (next + 1) % 4;
        SpectatePlayer(next);
    }
}
```

### 7.6 관전 UI

```
관전 중 표시되는 추가 UI:

관전 바 (상단):
  ┌──────────────────────────────────┐
  │  ◀  P3 (Lv.6) 보드 관전 중  ▶   │
  │         [내 보드로 돌아가기]       │
  └──────────────────────────────────┘
  - ◀ ▶: 이전/다음 플레이어 전환
  - 탈락한 플레이어는 스킵

관전 모드 제약:
  - 드래그&드롭 비활성 (관전 대상 보드 조작 불가)
  - 상점은 항상 자기 것만 표시 (관전 중에도 구매 가능)
  - 시너지 패널은 관전 대상의 시너지 표시

데이터:
  - Quantum Frame에서 대상 플레이어 컴포넌트 직접 읽기
  - 추가 네트워크 비용 없음 (Quantum은 전체 상태를 로컬에 보유)
```

### 7.7 보드별 그리드↔월드 좌표 변환

```csharp
public static class BoardWorldHelper
{
    private const float TileSize = 1.2f;
    private const float BoardSpacing = 20f;

    /// <summary>보드 인덱스 + 그리드 좌표 → 월드 좌표</summary>
    public static Vector3 GridToWorldPosition(int boardIndex, int col, int row)
    {
        Vector3 boardOrigin = GetBoardOrigin(boardIndex);
        return boardOrigin + new Vector3(col * TileSize, 0f, row * TileSize);
    }

    /// <summary>월드 좌표 → 어느 보드의 몇 번 그리드인지</summary>
    public static (int boardIndex, int col, int row) WorldToBoard(Vector3 worldPos)
    {
        int bCol = Mathf.RoundToInt(worldPos.x / BoardSpacing);
        int bRow = Mathf.RoundToInt(-worldPos.z / BoardSpacing);
        int boardIndex = bCol + bRow * 2;

        Vector3 boardOrigin = GetBoardOrigin(boardIndex);
        Vector3 local = worldPos - boardOrigin;
        int col = Mathf.RoundToInt(local.x / TileSize);
        int row = Mathf.RoundToInt(local.z / TileSize);
        return (boardIndex, col, row);
    }

    /// <summary>보드 인덱스 + 벤치 슬롯 → 월드 좌표</summary>
    public static Vector3 BenchToWorldPosition(int boardIndex, int benchSlot)
    {
        Vector3 boardOrigin = GetBoardOrigin(boardIndex);
        // 벤치는 보드 아래쪽(row = -1)에 가로로 나열
        return boardOrigin + new Vector3(benchSlot * TileSize, 0f, -1f * TileSize);
    }

    private static Vector3 GetBoardOrigin(int boardIndex)
    {
        int c = boardIndex % 2;
        int r = boardIndex / 2;
        return new Vector3(c * BoardSpacing, 0f, -r * BoardSpacing);
    }
}
```

---

## 8. 모바일 최적화

### 8.1 터치 입력

```
드래그 & 드롭:
  - 유닛/아이템 터치 → 0.15초 이상 홀드 → 드래그 시작
  - 짧은 탭: 유닛 상세 팝업
  - 드래그 중: 유효 영역 하이라이트
  - 놓기: 해당 위치에 Command 전송
  - 유효하지 않은 위치: 원래 자리로 스냅백

상점:
  - 슬롯 탭: 즉시 구매 (확인 없이)
  - 드래그: 구매 + 원하는 보드 위치에 배치 (고급 조작)

판매:
  - 유닛을 상점 영역으로 드래그 → 판매
  - 또는 유닛 상세 팝업에서 판매 버튼
```

### 8.2 해상도 대응

```
기존 프로젝트의 STKForceCameraRatio 활용:
  - 세로 고정 (Portrait) 또는 가로 (Landscape) 결정 필요
  - 오토체스 UI 특성상 가로(Landscape) 권장
    → 보드가 넓고, 양쪽 패널 배치에 유리
  - 다양한 종횡비(16:9, 18:9, 20:9) 대응
  - 노치/펀치홀 세이프 에어리어
```

### 8.3 UI 성능

```
최적화 포인트:
  - Canvas 분리: HUD / 상점 / 보드 UI / 팝업 별도 Canvas
    → SetDirty 영역 최소화
  - 상점 슬롯: 챔피언 교체 시에만 리빌드
  - 시너지 패널: SynergyUpdated 이벤트 시에만 갱신
  - HP바: CombatUnit.CurrentHP 폴링하되 변화 없으면 스킵
  - 데미지 텍스트: 오브젝트 풀링
  - 타이머: 매 프레임 텍스트 갱신 (가벼움)

보드 LOD 최적화 (§7.3):
  - Active 보드 1개만 풀 렌더링 → 실질 부하 = 보드 1개분
  - Inactive 보드: Spine/파티클/UI 비활성 → CPU/GPU 거의 0
  - 4보드 × 16유닛 = 64 Transform 갱신은 무시할 수 있는 비용
  - 카메라 이동 관전: 오브젝트 생성/파괴 없음 → GC 스파이크 방지
```

---

## 9. 게임 결과 화면

### 9.1 구성

```
GameOver 이벤트 수신 시 전체 화면 결과 표시:

┌──────────────────────────────────────┐
│                                      │
│          🏆 VICTORY / DEFEAT          │
│             당신의 순위: 2위           │
│                                      │
│  ┌──────────────────────────────┐    │
│  │ 1위  Player1  ♛  L8  🔥5연승 │    │
│  │ 2위  Player2      L7         │ ◄  │
│  │ 3위  Player3      L6         │    │
│  │ 4위  Player4  ☠  L4         │    │
│  └──────────────────────────────┘    │
│                                      │
│  보상:                               │
│    🏅 +15 LP                         │
│    💰 +500 Gold                      │
│                                      │
│          [로비로 돌아가기]             │
└──────────────────────────────────────┘
```

### 9.2 결과 데이터

```
Quantum 세션에서 수집되는 결과 데이터:

struct GameResult {
    PlayerRef[] Rankings;          // 순위 배열
    int[] FinalLevels;             // 최종 레벨
    int TotalRounds;               // 총 라운드 수
    float GameDurationSeconds;     // 게임 소요 시간
    // 플레이어별 통계
    int[] TotalDamageDealt;
    int[] TotalDamageReceived;
    int[] UnitsKilled;
    int[] GoldEarned;
}

결과 전송:
  Quantum GameOver → Unity View에서 수집 → 서버 RPC → 보상 수신
```

---

## 10. 전체 UI 데이터 흐름 요약

```
Quantum Simulation (결정론적)
    │
    ├─ Verified Frame (매 프레임)
    │   │
    │   └─ QuantumViewBridge.OnUpdateView()
    │       ├─ PhaseHUD 갱신 (타이머, 페이즈)
    │       ├─ PlayerListPanel 갱신 (HP, 레벨)
    │       ├─ ShopPanel 갱신 (골드, 슬롯)
    │       ├─ CombatUnit 위치 보간
    │       └─ HP/마나 바 갱신
    │
    └─ Quantum Events (변경 시)
        ├─ PhaseChanged → UI 레이아웃 전환
        ├─ SynergyUpdated → SynergyPanel 재빌드
        ├─ UnitCombined → 합성 VFX + 뷰 갱신
        ├─ ItemEquipped/Combined → 아이템 UI 갱신
        ├─ UnitAttacked → 근접: 즉시 히트VFX / 원거리: 투사체 스폰
        ├─ ProjectileHit → 투사체 도착 시 히트VFX + 데미지텍스트
        ├─ ProjectileExploded → 범위 폭발 VFX
        ├─ UnitDied → 사망 연출
        ├─ CombatResult → 결과 배너
        ├─ PlayerEliminated → 탈락 연출
        └─ GameOver → 결과 화면

Unity Input (플레이어 조작)
    │
    └─ InputCommandBridge
        ├─ 상점 탭 → BuyUnitCommand
        ├─ 리롤 버튼 → RerollShopCommand
        ├─ 유닛 드래그 → PlaceUnit/MoveUnit/WithdrawUnit
        ├─ 아이템 드래그 → EquipItemCommand
        ├─ 커맨더 스킬 → UseCommanderSkillCommand
        └─ 레디 버튼 → ReadyCommand
```
