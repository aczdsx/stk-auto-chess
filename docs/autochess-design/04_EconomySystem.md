# 경제 시스템 설계

> 골드, 경험치(XP), 레벨, 이자, 연승/연패 보너스 등 인게임 경제를 정의한다.
>
> **GameMode 적용**: `EnableEconomy=true`인 모드에서만 활성화 (PvECampaign, Competitive).
> ClassicBattle 모드에서는 경제 시스템 전체가 비활성화된다.

---

## 1. 경제 자원 개요

| 자원 | 용도 | 획득 | 소비 |
|------|------|------|------|
| **골드** | 챔피언 구매, 리롤, XP 구매 | 라운드 수입, 이자, 연승/연패 | 구매, 리롤 |
| **경험치(XP)** | 레벨업 → 필드 유닛 수 증가 | 라운드별 자동, 골드로 구매 | 레벨업 시 소모 |
| **레벨** | 필드 유닛 수 상한, 챔피언 등급 출현율 | XP 누적 | - |

---

## 2. 골드 시스템

### 2.1 라운드별 수입

```
라운드 종료 시 골드 지급:
  총 수입 = 기본 수입 + 이자 + 연승/연패 보너스 + 승리 보너스
```

### 2.2 기본 수입

| 스테이지 | 라운드 | 기본 수입 |
|---------|--------|----------|
| 1 (PvE) | 1-1 | 2 |
| 1 | 1-2 | 2 |
| 1 | 1-3 | 3 |
| 2+ | 모든 | 5 |

> 스펙 데이터(`RoundConfigAsset`)로 라운드별 기본 수입 설정 가능.

### 2.3 이자

```
이자 = floor(현재 골드 / 10)
최대 이자 = 5 (50골드 이상이면 항상 5)

예시:
  8골드 → 이자 0
  13골드 → 이자 1
  27골드 → 이자 2
  50골드 → 이자 5
  80골드 → 이자 5 (최대)
```

### 2.4 연승/연패 보너스

| 연속 횟수 | 보너스 골드 |
|----------|-----------|
| 2~3 연속 | +1 |
| 4~5 연속 | +2 |
| 6+ 연속 | +3 |

```
연승: 연속 승리 시 보너스
연패: 연속 패배 시에도 동일 보너스 (의도적 연패 전략 허용)
무승부: 연승/연패 카운터 리셋하지 않음 (유지)
PvE 라운드: 연승/연패에 포함하지 않음
```

### 2.5 승리 보너스

```
PvP 승리 시 +1 골드
PvP 패배 시 +0 골드
PvE 승리 시 +1 골드
```

### 2.6 수입 계산 예시

```
상황: Stage 3, 현재 37골드, 4연승

기본 수입:    5
이자:        3  (37/10 = 3)
연승 보너스:  2  (4연승)
승리 보너스:  1
─────────────
총 수입:     11골드

결과: 37 + 11 = 48골드
```

---

## 3. 경험치(XP) & 레벨 시스템

### 3.1 레벨별 필드 유닛 상한 & 필요 XP

| 레벨 | 필드 유닛 수 | 레벨업 필요 XP | 누적 XP |
|------|------------|--------------|---------|
| 1 | 1 | - | - |
| 2 | 2 | 2 | 2 |
| 3 | 3 | 6 | 8 |
| 4 | 4 | 10 | 18 |
| 5 | 5 | 20 | 38 |
| 6 | 6 | 36 | 74 |
| 7 | 7 | 48 | 122 |
| 8 | 8 | 72 | 194 |

> 4인 게임 (TFT 8인 대비 짧은 게임). 최대 레벨 8.

### 3.2 XP 획득

```
라운드별 자동 XP:
  매 라운드 종료 시 +2 XP (PvE/PvP 모두)

골드로 XP 구매:
  4골드 → +4 XP
  Preparation 페이즈에서만 가능
  횟수 제한 없음
```

### 3.3 레벨과 챔피언 등급 출현율

레벨이 높을수록 고등급 챔피언 출현 확률 증가:

| 레벨 | 1코스트 | 2코스트 | 3코스트 | 4코스트 | 5코스트 |
|------|--------|--------|--------|--------|--------|
| 1 | 100% | 0% | 0% | 0% | 0% |
| 2 | 100% | 0% | 0% | 0% | 0% |
| 3 | 75% | 25% | 0% | 0% | 0% |
| 4 | 55% | 30% | 15% | 0% | 0% |
| 5 | 40% | 35% | 20% | 5% | 0% |
| 6 | 25% | 35% | 30% | 10% | 0% |
| 7 | 19% | 30% | 35% | 15% | 1% |
| 8 | 14% | 20% | 35% | 25% | 6% |

> 이 테이블은 스펙 데이터로 관리. 밸런스 조정 시 데이터만 변경.

---

## 4. Quantum 컴포넌트 설계

### 4.1 플레이어 경제 데이터

```qtn
component PlayerEconomy {
    Int32 Gold;
    Int32 XP;
    Int32 Level;
    Int32 WinStreak;       // 양수: 연승, 음수: 연패
    Int32 TotalWins;
    Int32 TotalLosses;
}
```

### 4.2 경제 설정 (Asset)

```csharp
public class EconomyConfigAsset : AssetObject
{
    // 이자
    public int InterestPer;           // 10 (10골드당 1이자)
    public int MaxInterest;           // 5

    // 연승/연패 보너스
    public StreakBonus[] StreakBonuses; // [{min:2, max:3, gold:1}, {min:4, max:5, gold:2}, ...]

    // 리롤
    public int RerollCost;            // 2 (골드)

    // XP
    public int XPPerRound;            // 2
    public int XPBuyCost;             // 4 (골드)
    public int XPBuyAmount;           // 4

    // 레벨업 테이블
    public int[] XPToLevelUp;         // [0, 2, 6, 10, 20, 36, 48, 72]

    // 등급 출현율 테이블
    public RarityChance[] RarityByLevel; // 레벨별 [1코스트%, 2코스트%, ...]

    // 라운드별 기본 수입
    public int[] BaseIncomeByRound;   // 라운드 인덱스별 기본 수입
}

public struct StreakBonus
{
    public int MinStreak;
    public int MaxStreak;
    public int BonusGold;
}

public struct RarityChance
{
    public int Level;
    public int[] Chances; // [1코스트%, 2코스트%, 3코스트%, 4코스트%, 5코스트%]
}
```

---

## 5. EconomySystem 설계

```csharp
public unsafe class EconomySystem : SystemSignalsOnly,
    ISignalOnPhaseStarted,
    ISignalOnCombatEnd
{
    // 계획 페이즈 시작 시: 수입 지급
    public void OnPhaseStarted(Frame f, GamePhase phase)
    {
        if (phase != GamePhase.Preparation) return;

        var config = f.FindAsset<EconomyConfigAsset>(...);
        var players = f.Filter<PlayerEconomy>();

        while (players.Next(out var entity, out var economy))
        {
            // 기본 수입
            int income = GetBaseIncome(f, config);

            // 이자
            int interest = Math.Min(economy->Gold / config.InterestPer, config.MaxInterest);

            // 연승/연패 보너스
            int streakBonus = GetStreakBonus(config, economy->WinStreak);

            // 지급
            economy->Gold += income + interest + streakBonus;

            // 자동 XP
            economy->XP += config.XPPerRound;
            CheckLevelUp(f, entity, economy, config);

            // View 이벤트
            f.Events.GoldChanged(entity, economy->Gold, income, interest, streakBonus);
        }
    }

    // 전투 종료 시: 연승/연패 갱신 + 승리 보너스
    public void OnCombatEnd(Frame f, player_ref winner, player_ref loser, Int32 matchIndex)
    {
        UpdateStreak(f, winner, won: true);
        UpdateStreak(f, loser, won: false);

        // 승리 보너스
        var winnerEconomy = f.Get<PlayerEconomy>(GetPlayerEntity(f, winner));
        winnerEconomy->Gold += 1;
    }
}
```

### BuyXP Command 처리

```csharp
// ShopSystem 또는 EconomySystem에서 처리
public void OnBuyXP(Frame f, PlayerRef player)
{
    var economy = f.Get<PlayerEconomy>(GetPlayerEntity(f, player));
    var config = f.FindAsset<EconomyConfigAsset>(...);

    if (economy->Gold < config.XPBuyCost) return;
    if (economy->Level >= MaxLevel) return;

    economy->Gold -= config.XPBuyCost;
    economy->XP += config.XPBuyAmount;
    CheckLevelUp(f, entity, economy, config);
}
```

---

## 6. View 이벤트

```qtn
// 골드 변동 (수입 내역 표시용)
event GoldChanged {
    entity_ref Player;
    Int32 TotalGold;
    Int32 BaseIncome;
    Int32 Interest;
    Int32 StreakBonus;
}

// 레벨업
event LevelUp {
    entity_ref Player;
    Int32 NewLevel;
    Int32 MaxUnits;
}

// XP 변동
event XPChanged {
    entity_ref Player;
    Int32 CurrentXP;
    Int32 RequiredXP;
}
```

---

## 7. 경제 밸런스 고려사항

### 4인 게임 특성

```
TFT(8인) 대비 차이점:
  - 게임 길이 짧음 → 경제 곡선 조정 필요
  - 챔피언 풀이 작음 → 풀 고갈이 빠름
  - 매칭 다양성 낮음 → 연승/연패 밸런스 중요

조정 포인트:
  - 기본 수입을 TFT보다 높게 → 빠른 경제 성장
  - 최대 레벨을 8로 제한 → 8인 게임의 9~10 레벨 생략
  - 이자 최대 5 유지 → 저축 전략은 동일하게 유효
```

### 전략적 선택지

```
공격적 경제:
  - 매 라운드 리롤/XP 구매로 빠른 파워 스파이크
  - 이자 포기 → 초반 우위, 후반 자원 부족

방어적 경제 (이코노미):
  - 50골드 유지 (이자 5) → 안정적 수입
  - 특정 시점에 대량 리롤 → 핵심 챔피언 2~3★ 달성

연패 전략:
  - 의도적 패배 → 연패 보너스 + 캐러셀 우선권
  - 체력 관리가 핵심 (4인이라 탈락이 빠름)
```
