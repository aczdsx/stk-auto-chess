# 시너지 시스템 설계

> 챔피언 특성(Trait) 조합에 따른 시너지 활성화, 효과 적용, 재계산 로직을 정의한다.
>
> **GameMode 적용**: `EnableSynergy=true`인 모드에서만 활성화 (PvECampaign, Competitive).
> ClassicBattle 모드에서는 시너지 시스템이 비활성화된다 (기존 캐릭터 스탯을 그대로 사용).

---

## 1. 시너지 개요

```
시너지 = 같은 특성(Trait)을 가진 챔피언을 보드에 배치하면 발동하는 보너스

핵심 규칙:
  - 보드 위 유닛만 시너지에 기여 (벤치 유닛 제외)
  - 하나의 챔피언은 여러 특성을 보유할 수 있음 (보통 2~3개)
  - 특성별 **고유 챔피언 수** 기준으로 단계적 보너스 증가
  - 같은 챔피언을 여러 개 배치해도 시너지 카운트는 1만 증가
    → 예: Warrior 특성 "불꽃 기사" 1★ 2개 + 2★ 1개 = Warrior **1** 카운트
    → Warrior 시너지를 올리려면 **다른** Warrior 챔피언을 배치해야 함
```

---

## 2. 특성 구조

### 2.1 특성 분류

```
크게 두 축:

1. 출신(Origin) — 배경/종족 기반
   예: Human, Elf, Demon, Dragon, Machine, ...

2. 직업(Class) — 역할 기반
   예: Warrior, Mage, Assassin, Ranger, Guardian, Healer, ...

각 챔피언은 1~2개 출신 + 1개 직업을 보유.
(특수 챔피언은 직업 2개 가능)

예시 챔피언:
  - "불꽃 기사" → 출신: Dragon, 직업: Warrior
  - "어둠 마법사" → 출신: Demon, 직업: Mage
  - "숲의 궁수" → 출신: Elf, 직업: Ranger
```

### 2.2 활성화 단계

```
특성마다 활성화에 필요한 유닛 수가 다름:

예시 - Warrior (전사):
  2/4/6 명 단계
  - 2명: 전사 유닛 방어력 +20
  - 4명: 전사 유닛 방어력 +50
  - 6명: 전사 유닛 방어력 +80, 공격력 +15%

예시 - Assassin (암살자):
  2/4 명 단계
  - 2명: 암살자 유닛 전투 시작 시 적 후방으로 점프
  - 4명: 추가로 첫 공격 크리티컬 확정 + 크리티컬 배율 증가

예시 - Dragon (용족):
  2/3 명 단계
  - 2명: 용족 유닛 마나 시작값 +30
  - 3명: 용족 유닛 스킬 데미지 +40%

4인 게임 조정:
  - TFT의 최대 단계(보통 6~8)보다 낮게 설정 (최대 4~6)
  - 최대 8유닛이므로 높은 단계 달성이 어려움
  - 적은 수로 더 강한 보너스 제공하여 보상
```

---

## 3. Quantum 컴포넌트 설계

### 3.1 챔피언 특성 데이터

```qtn
// 유닛에 부착 (변경 불가, 스펙에서 결정)
component UnitTraits {
    // 비트 플래그로 특성 관리 (최대 32개 특성)
    Int32 TraitFlags;
}
```

### 3.2 플레이어 시너지 상태

```qtn
// 플레이어별 현재 시너지 상태
component PlayerSynergy {
    // 각 특성별 활성 유닛 수 (최대 32개 특성, 값: 0~8)
    array<Byte>[32] TraitCounts;

    // 각 특성별 현재 활성 단계 (0=비활성, 1=1단계, 2=2단계, ...)
    array<Byte>[32] TraitTiers;

    // 활성된 시너지 수
    Byte ActiveSynergyCount;
}
```

### 3.3 시너지 스펙 (Asset)

```csharp
public class SynergySpecAsset : AssetObject
{
    public int TraitId;           // 특성 ID (비트 인덱스)
    public string TraitName;      // 표시 이름
    public TraitCategory Category; // Origin / Class
    public SynergyTier[] Tiers;   // 단계별 효과
}

public enum TraitCategory
{
    Origin,
    Class
}

public struct SynergyTier
{
    public int RequiredCount;     // 필요 유닛 수
    public SynergyEffect[] Effects; // 적용 효과 목록
}

public struct SynergyEffect
{
    public SynergyEffectType Type;
    public SynergyTarget Target;  // 해당 특성 유닛만 / 아군 전체
    public FP Value;              // 효과 수치
    public FP ValuePercent;       // 퍼센트 수치
}

public enum SynergyEffectType
{
    // 스탯 보너스
    BonusArmor,
    BonusMagicResist,
    BonusAttack,
    BonusAttackPercent,
    BonusHP,
    BonusHPPercent,
    BonusAttackSpeed,
    BonusMana,
    BonusCritChance,
    BonusCritMultiplier,

    // 특수 효과
    StartingMana,           // 전투 시작 시 마나
    SpellDamagePercent,     // 스킬 데미지 배율
    LifeSteal,              // 생명력 흡수
    DodgeChance,            // 회피 확률
    BacklineJump,           // 전투 시작 시 후방 점프 (암살자)
    ShieldOnCombatStart,    // 전투 시작 시 보호막
    DamageReduction,        // 피해 감소율
}

public enum SynergyTarget
{
    TraitUnits,     // 해당 특성 유닛에만 적용
    AllAllies       // 아군 전체
}
```

---

## 4. SynergySystem 설계

### 4.1 시너지 재계산

```csharp
public unsafe class SynergySystem : SystemSignalsOnly,
    ISignalOnBoardChanged,
    ISignalOnPhaseStarted
{
    // 보드 변경 시 재계산
    public void OnBoardChanged(Frame f, PlayerRef player)
    {
        RecalculateSynergies(f, player);
    }

    // 전투 시작 시 최종 확인
    public void OnPhaseStarted(Frame f, GamePhase phase)
    {
        if (phase != GamePhase.Combat) return;

        for (int p = 0; p < f.PlayerCount; p++)
        {
            RecalculateSynergies(f, (PlayerRef)p);
        }
    }

    private void RecalculateSynergies(Frame f, PlayerRef player)
    {
        var synergy = GetPlayerSynergy(f, player);
        var board = GetPlayerBoard(f, player);

        // 1. 모든 카운트 초기화
        for (int i = 0; i < 32; i++)
        {
            synergy->TraitCounts[i] = 0;
            synergy->TraitTiers[i] = 0;
        }

        // 2. 보드 유닛의 특성 집계 (고유 챔피언 기준)
        // 같은 챔피언이 여러 개 있어도 시너지 카운트는 1만 증가
        // 보드에 최대 8유닛이므로 고유 챔피언도 최대 8종
        // Quantum에서는 fixed-size 배열 사용 (힙 할당 회피)
        var counted = new FixedArray8<Int32>();
        int countedLen = 0;

        for (int tile = 0; tile < 28; tile++)
        {
            if (board->Tiles[tile] == EntityRef.None) continue;

            var unitData = f.Get<UnitData>(board->Tiles[tile]);
            int specId = unitData->ChampionSpecId;

            // 이미 이 챔피언 종류를 카운트했으면 스킵
            bool alreadyCounted = false;
            for (int c = 0; c < countedLen; c++)
            {
                if (counted[c] == specId) { alreadyCounted = true; break; }
            }
            if (alreadyCounted) continue;
            counted[countedLen++] = specId;

            var traits = f.Get<UnitTraits>(board->Tiles[tile]);
            int flags = traits->TraitFlags;

            for (int bit = 0; bit < 32; bit++)
            {
                if ((flags & (1 << bit)) != 0)
                {
                    synergy->TraitCounts[bit]++;
                }
            }
        }

        // 3. 각 특성의 활성 단계 결정
        synergy->ActiveSynergyCount = 0;
        for (int i = 0; i < 32; i++)
        {
            if (synergy->TraitCounts[i] == 0) continue;

            var spec = FindSynergySpec(f, i);
            if (spec == null) continue;

            // 가장 높은 달성 단계 찾기
            byte tier = 0;
            for (int t = spec.Tiers.Length - 1; t >= 0; t--)
            {
                if (synergy->TraitCounts[i] >= spec.Tiers[t].RequiredCount)
                {
                    tier = (byte)(t + 1);
                    break;
                }
            }

            synergy->TraitTiers[i] = tier;
            if (tier > 0) synergy->ActiveSynergyCount++;
        }

        // View 이벤트
        f.Events.SynergyUpdated(player, synergy->ActiveSynergyCount);
    }
}
```

### 4.2 시너지 효과 적용 (전투 시)

```csharp
public unsafe class SynergyEffectSystem : SystemSignalsOnly,
    ISignalOnPhaseStarted
{
    public void OnPhaseStarted(Frame f, GamePhase phase)
    {
        if (phase != GamePhase.Combat) return;

        // 각 매치의 CombatUnit에 시너지 보너스 적용
        for (int p = 0; p < f.PlayerCount; p++)
        {
            var player = (PlayerRef)p;
            var synergy = GetPlayerSynergy(f, player);

            // 활성 시너지 순회
            for (int traitId = 0; traitId < 32; traitId++)
            {
                byte tier = synergy->TraitTiers[traitId];
                if (tier == 0) continue;

                var spec = FindSynergySpec(f, traitId);
                var tierData = spec.Tiers[tier - 1];

                foreach (var effect in tierData.Effects)
                {
                    ApplyEffect(f, player, traitId, effect);
                }
            }
        }
    }

    private void ApplyEffect(Frame f, PlayerRef player,
                              int traitId, SynergyEffect effect)
    {
        var filter = f.Filter<CombatUnit>();
        while (filter.NextUnsafe(out var entity, out var unit))
        {
            if (unit->Owner != player) continue;
            if (unit->State == CombatState.Dead) continue;

            // 대상 체크
            if (effect.Target == SynergyTarget.TraitUnits)
            {
                // 원본 유닛의 특성 확인
                var traits = f.Get<UnitTraits>(unit->SourceUnit);
                if ((traits->TraitFlags & (1 << traitId)) == 0) continue;
            }

            // 효과 적용
            switch (effect.Type)
            {
                case SynergyEffectType.BonusArmor:
                    unit->Armor += effect.Value;
                    break;
                case SynergyEffectType.BonusAttack:
                    unit->Attack += effect.Value;
                    break;
                case SynergyEffectType.BonusAttackPercent:
                    unit->Attack += unit->Attack * effect.ValuePercent / 100;
                    break;
                case SynergyEffectType.BonusHP:
                    unit->MaxHP += effect.Value;
                    unit->CurrentHP += effect.Value;
                    break;
                case SynergyEffectType.BonusHPPercent:
                    FP bonus = unit->MaxHP * effect.ValuePercent / 100;
                    unit->MaxHP += bonus;
                    unit->CurrentHP += bonus;
                    break;
                case SynergyEffectType.BonusAttackSpeed:
                    unit->AttackSpeed += effect.Value;
                    break;
                case SynergyEffectType.StartingMana:
                    unit->CurrentMana += effect.Value;
                    ClampMana(unit);
                    break;
                // ... 기타 효과
            }
        }
    }
}
```

---

## 5. 특수 시너지 효과

### 5.1 전투 시작 시 트리거

```
일부 시너지는 스탯 보너스가 아닌 전투 시작 시 특수 행동:

BacklineJump (암살자):
  - 전투 시작 직후 적 후방 Row의 빈 칸으로 점프
  - 적 유닛 인접 빈 칸 우선 선택
  - CombatUnit.HasBacklineJump = true 설정
  - BacklineJumpSystem에서 전투 첫 프레임에 처리 (07_CombatSystem.md 참조)

ShieldOnCombatStart (수호자):
  - 전투 시작 시 HP의 N% 보호막 부여
  - 보호막은 일정 시간 후 소멸

처리 시점:
  - CombatSetup 완료 직후, 첫 프레임 전에 적용
  - SynergyEffectSystem.OnPhaseStarted에서 처리
```

### 5.2 전투 중 지속 효과

```
LifeSteal (흡혈):
  - 기본 공격 데미지의 N%만큼 HP 회복
  - DamageSystem에서 공격 시 체크

DodgeChance (회피):
  - 기본 공격 N% 확률로 회피 (데미지 0)
  - DamageSystem에서 피격 시 체크

DamageReduction (피해 감소):
  - 받는 데미지 N% 감소
  - DamageSystem에서 데미지 적용 전 체크

이런 전투 중 효과는 CombatUnit의 플래그/수치에 기록:
  → 전투 시스템이 데미지 계산 시 참조
```

---

## 6. 시너지 예시 (4인 게임 밸런스)

### 6.1 출신 (Origin) 시너지

```
| 특성 | 단계 | 효과 |
|------|------|------|
| Human | 2/4 | 아군 전체 공격속도 +15% / +35% |
| Elf | 2/3 | 특성 유닛 회피 +20% / +35% |
| Demon | 1/2/4 | 특성 유닛 스킬 데미지 +20% / +40% / +70%, 추가 마나 흡수 |
| Dragon | 2/3 | 특성 유닛 시작 마나 +30 / 스킬 데미지 +40% |
| Machine | 2/4 | 특성 유닛 시작 시 보호막 (최대HP 30% / 60%) |
| Undead | 2/3 | 적 전체 방어력 -20% / -40% |
```

### 6.2 직업 (Class) 시너지

```
| 특성 | 단계 | 효과 |
|------|------|------|
| Warrior | 2/4/6 | 특성 유닛 방어력 +20 / +50 / +80 & 공격력 +15% |
| Mage | 2/4 | 특성 유닛 마법 데미지 +25% / +55%, 적 마저 -20% |
| Assassin | 2/4 | 전투 시작 후방 점프 / 추가 크리티컬 +30% 배율 +50% |
| Ranger | 2/4 | 특성 유닛 공격속도 +25% / +55% |
| Guardian | 2/3 | 인접 유닛 방어력 +30 / 아군 전체 피해감소 15% |
| Healer | 2/3 | 가장 HP 낮은 아군 주기적 회복 / 회복량 증가 |
```

### 6.3 4인 게임 밸런스 원칙

```
최대 8유닛 환경:
  - 2단계: 쉽게 달성 (초반~중반)
  - 3~4단계: 의도적 투자 필요 (중반~후반)
  - 5~6단계: 거의 올인 빌드 (특정 특성 집중)

특성 개수:
  - 출신: 6~8종
  - 직업: 6~8종
  - 총 12~16개 특성

빌드 다양성:
  - 2개 특성 2단계 + 1개 3단계 → 일반적 빌드
  - 1개 특성 4단계 이상 → 집중 빌드
  - 여러 2단계 조합 → 유연 빌드
```

---

## 7. View 이벤트

```qtn
event SynergyUpdated {
    player_ref Player;
    Byte ActiveCount;
}

event SynergyTierChanged {
    player_ref Player;
    Int32 TraitId;
    Byte OldTier;
    Byte NewTier;
}
```

---

## 8. UI 표현

### 8.1 시너지 패널

```
왼쪽 사이드바에 활성 시너지 목록 표시:

┌──────────────┐
│ ⬟ Dragon (2/3) │  ← 2/3 달성, 골드 색상
│ ⬡ Warrior (4/4) │  ← 4/4 달성, 크롬 색상
│ ◇ Mage (2/4) │  ← 2/4 달성, 실버 색상
│ △ Assassin (1/2) │  ← 미달성, 회색
└──────────────┘

색상 규칙:
  - 미달성 (0단계): 회색 (비활성 표시이나 1개 이상이면 표시)
  - 1단계: 브론즈
  - 2단계: 실버
  - 3단계: 골드
  - 최대 단계: 크롬 (특수 이펙트)

정렬 순서:
  1. 최대 단계 달성 → 상단
  2. 같은 단계면 유닛 수 많은 순
  3. 미달성은 하단
```

### 8.2 유닛 시너지 하이라이트

```
보드 위 유닛을 탭하면:
  - 해당 유닛의 특성 표시
  - 같은 특성의 다른 유닛 하이라이트
  - 시너지 효과 상세 팝업
```

---

## 9. 시너지 계산 성능

```
계산 빈도:
  - Preparation 중 보드 변경 시 (유닛 배치/회수마다)
  - 전투 시작 시 1회 (최종 확정)
  - 전투 중에는 재계산 없음

성능:
  - 최대 8유닛의 특성 비트 플래그 OR 연산 → O(8)
  - 32개 특성 단계 결정 → O(32 × 최대단계수)
  - 효과 적용 → O(시너지 수 × 유닛 수)
  - 전체: 극히 가벼움 (매 프레임 아님, 이벤트 기반)
```
