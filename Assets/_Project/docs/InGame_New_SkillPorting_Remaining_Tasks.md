# InGame_New 스킬 시스템 포팅 — 남은 작업 목록

## 완료된 작업

### Phase 1: CombatTraitBase + 콜백 인프라 ✅
- `Traits/CombatTraitBase.cs` — 추상 베이스 클래스 (11개 가상 콜백)
- `Traits/TraitSystem.cs` — 콜백 디스패치 시스템 (AddTrait, Invoke* 메서드)
- `Components.cs` — `CombatMatchState`에 `Traits[][]`, `TraitCounts[]`, `_traitCombatStartDone` 추가
- `DamageSystem.cs` — `ApplyDamage()`에 attackerIndex/damageType 파라미터 + Trait 콜백 삽입
- `DamageSystem.cs` — `ExecuteBasicAttack()`에 OnPreAttack/OnCritical/OnPostAttack 콜백 삽입
- `CombatAISystem.cs` — 첫 프레임 OnCombatStart + 매 틱 OnTick 콜백 삽입

---

## 남은 작업

### Phase 2: TraitFactory + 레거시 EffectCode 매핑

**목표**: 레거시 EffectCode를 CombatTraitBase 구현체로 변환하여 유닛에 부착

#### 2-1. TraitFactory 생성
- **파일**: `Traits/TraitFactory.cs` (신규)
- SkillFactory와 동일한 패턴 (Reflection 금지, 수동 등록)
- `EffectCodeId → CombatTraitBase` 매핑 딕셔너리
- `Initialize()`: SpecDataManager에서 캐릭터별 패시브/무기 스킬 스펙 읽어서 등록
- `Create(int effectCodeId) → CombatTraitBase`

#### 2-2. CombatSetupSystem에서 Trait 부착
- **파일**: `CombatSetupSystem.cs` (수정)
- `SpawnTeamUnits()` 또는 별도 `SetupTraits()` 메서드에서:
  - 유닛의 패시브 스킬 스펙 조회
  - `TraitFactory.Create()` → `TraitSystem.AddTrait()` 호출
- SkillSystem.SetupSkills()와 같은 타이밍에 호출

#### 2-3. 레거시 EffectCode → Trait 구현체 변환 (점진적)
- **파일**: `Traits/Impls/` (신규 폴더)
- 레거시 `EffectCodeSkill*` 중 패시브 효과를 CombatTraitBase 서브클래스로 포팅
- 우선순위: 자주 사용되는 공통 패시브부터
  - 예: 공격 시 추가 데미지, 크리티컬 시 마나 회복, 처치 시 HP 회복 등
- 각 구현체는 해당 콜백만 override

#### 2-4. Trait 정리/해제
- **파일**: `Traits/TraitSystem.cs` (수정)
- `Cleanup(CombatMatchState)` 메서드 추가 (SkillSystem.Cleanup과 동일 패턴)
- `CombatAISystem` 또는 매치 종료 시점에서 호출

---

### Phase 3: 스킬 시스템 보강

#### 3-1. CR1 수정: 즉시 스킬 2회 실행 방지 (Critical 이슈)
- **파일**: `SkillSystem.cs`
- **현상**: 즉시 스킬 실행 후 `CastingSkill` 상태 유지 → 다음 틱에서 재실행
- **현재 상태**: TryCast()에서 즉시 스킬 시 `unit.State = CombatState.Idle` 설정으로 이미 수정됨 (SkillSystem.cs:84)
- **확인 필요**: `TickCasting()`의 `SkillCastTimer` 0 진입 경로에서 추가 방어 필요한지 검증

#### 3-2. 스킬에서 Trait 콜백 활용
- **파일**: 각 `SimSkill*.cs`
- 스킬 데미지에도 `attackerIndex` 전달하여 Trait 보정 적용
- 현재 `SkillHelpers.DealDamage()` 등에서 `ApplyDamage(state, ref target, dmg)` 호출 → attackerIndex 추가 필요
- 대상 파일:
  - `SkillHelpers.cs` (DealDamage, DealDamageIgnoreArmor)
  - `SimSkillAoEDamage.cs`, `SimSkillLineDamage.cs`, `SimSkillConeDamage.cs` 등
  - `SimSkillPatternDamage.cs`, `SimSkillTeleportStrike.cs`
  - `ProjectileSystem.cs` (투사체 적중 시)
  - 커스텀 스킬 6개

#### 3-3. 멀티히트 스킬 개선
- **파일**: `SimSkillMultiHit.cs`
- 현재: 단일 Execute() 내에서 N회 즉시 데미지
- 개선: 프레임 분산 히트 (히트 간격 기반) → View 연출과 동기화

---

### Phase 4: 평가 문서 이슈 수정

#### 4-1. CR2: 멀티타일 유닛 중복 스폰 (Critical)
- **파일**: `CombatSetupSystem.cs`
- 멀티타일 유닛의 동일 entityId가 여러 슬롯에 존재 시 앵커 체크 없이 전부 스폰
- 수정: 스폰 시 entityId 중복 체크 또는 앵커 좌표 검증

#### 4-2. H1: WithdrawUnit 멀티타일 풋프린트 부분 해제
- **파일**: `BoardSystem.cs`
- 회수 시 anchor 1칸만 `InvalidId` 처리 → `ClearBoardFootprint` 사용으로 통일

#### 4-3. H2: 스킬 이벤트 ID 불일치 (CombatId vs SourceEntityId)
- **파일**: `SkillSystem.cs`, `CombatViewManager.cs`, `AutoChessViewBridge.cs`
- 시뮬레이션이 SourceEntityId 전달하는데 뷰가 CombatId로 해석
- 수정: 이벤트에 CombatId 사용으로 통일, 또는 뷰에서 SourceEntityId→CombatId 매핑

#### 4-4. H3: 보드 입력 PlayerIndex 0 고정
- **파일**: `BoardInputHandler.cs`, `BenchUnitSlot.cs`
- 멀티 플레이어/관전 시 잘못된 보드 조작
- 수정: 현재 플레이어 인덱스 참조로 변경

---

### Phase 5: CC/면역 시스템

#### 5-1. CC 저항/면역
- **파일**: `StatusEffectSystem.cs` (수정), `CombatUnit` (필드 추가)
- CC 면역 플래그 또는 CC 저항 확률 필드
- Trait 콜백으로도 구현 가능 (ModifyIncomingDamage 패턴 응용)

#### 5-2. CC 해제 스킬
- **파일**: `SkillHelpers.cs` (신규 헬퍼), 해당 Sim 스킬
- 아군의 CC 상태를 해제하는 스킬 패턴

---

## 우선순위 요약

| 순위 | 작업 | 심각도 | 설명 |
|------|------|--------|------|
| 1 | 4-1 CR2 멀티타일 중복 스폰 | Critical | 전투 결과 왜곡 |
| 2 | 3-1 CR1 즉시 스킬 재실행 검증 | Critical | 스킬 2회 적용 가능성 |
| 3 | 4-2 H1 WithdrawUnit 풋프린트 | High | 유령 타일 잔상 |
| 4 | 4-3 H2 이벤트 ID 불일치 | High | VFX 누락 |
| 5 | 2-1~2-4 TraitFactory + 매핑 | Medium | 패시브 스킬 기능 확장 |
| 6 | 3-2 스킬 데미지 Trait 연동 | Medium | 스킬에서도 Trait 보정 적용 |
| 7 | 3-3 멀티히트 프레임 분산 | Low | 연출 품질 개선 |
| 8 | 5-1~5-2 CC 면역/해제 | Low | 전투 깊이 확장 |

---

## 파일 구조 (현재 + 계획)

```
InGame_New/Simulation/Combat/
├── Traits/
│   ├── CombatTraitBase.cs       ✅ 완료
│   ├── TraitSystem.cs           ✅ 완료
│   ├── TraitFactory.cs          ⬜ Phase 2
│   └── Impls/                   ⬜ Phase 2 (개별 Trait 구현체)
├── Skills/
│   ├── SimSkillBase.cs          ✅ 기존
│   ├── SkillFactory.cs          ✅ 기존
│   ├── SkillHelpers.cs          ⬜ Phase 3 (attackerIndex 추가)
│   ├── SimSkill*.cs             ⬜ Phase 3 (attackerIndex 추가)
│   └── Custom/                  ✅ 기존 (7개 커스텀 스킬)
├── DamageSystem.cs              ✅ Phase 1 수정 완료
├── CombatAISystem.cs            ✅ Phase 1 수정 완료
├── CombatSetupSystem.cs         ⬜ Phase 2 + Phase 4
└── ...
```
