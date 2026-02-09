# 플레이어 시스템 설계

> 플레이어 상태, HP, 탈락/순위, 매칭, 고스트, 관전, 재접속을 정의한다.
>
> **GameMode 적용**: 이 문서는 주로 Competitive 모드 기준이다.
> - ClassicBattle: HP/탈락/매칭/고스트/관전 전부 비활성. 1회 전투 승패만 판정.
> - PvECampaign: HP 시스템 활성 (HP=0→실패), 매칭/고스트/관전/탈락순위 비활성.
> - Competitive: 모든 기능 활성.
> 시스템은 `EnableElimination`, `EnableMatchmaking` 플래그로 분기한다.

---

## 1. 플레이어 상태 개요

```
4인 게임에서 각 플레이어가 가지는 핵심 상태:

┌──────────────────────────────────────┐
│ Player                               │
│                                      │
│  HP: 100          Level: 5           │
│  Gold: 23         XP: 12/36          │
│  Streak: +3 (연승) Alive: true       │
│  Rank: -          Eliminated: false  │
│                                      │
│  Board: [유닛 배치 상태]              │
│  Bench: [벤치 유닛]                   │
│  Shop: [상점 5슬롯]                   │
│  Items: [아이템 인벤토리]              │
│  Synergies: [활성 시너지]             │
│  CommanderSkills: [커맨더 스킬 2개]    │
└──────────────────────────────────────┘
```

---

## 2. HP 시스템

### 2.1 기본 사양

```
초기 HP: 100
최대 HP: 100 (회복 불가, 예외: 캐러셀 보상 등 특수 이벤트)
최소 HP: 0 (0 이하 → 탈락)

HP 감소 시점:
  - PvP 전투 패배 시
  - PvE 크립 라운드 패배 시 (소량)

HP 감소 공식 (07_CombatSystem.md 참조):
  PlayerDamage = BaseDamage(스테이지) + SurvivingUnitDamage(생존 적 유닛)
```

### 2.2 HP 관련 규칙

```
동시 탈락:
  - 같은 라운드에 여러 플레이어가 0 HP에 도달할 수 있음
  - 동시 탈락 시 남은 HP 비교 (높은 쪽이 높은 순위)
  - HP도 동일하면 → 해당 라운드의 전투 결과(남긴 유닛 수)로 판정
  - 그래도 동률 → 동일 순위 부여

HP 표시:
  - 정수로 표시 (소수점 없음)
  - 감소 시 빨간색 텍스트 + 셰이크 이펙트
  - 위험 구간 (HP ≤ 20): UI 경고 효과
```

---

## 3. 탈락 & 순위

### 3.1 탈락 처리

```
플레이어 HP ≤ 0 시:

탈락 순서:
  1. HP를 0으로 고정
  2. Eliminated = true 설정
  3. AlivePlayerCount 감소
  4. 순위 확정: Rank = AlivePlayerCount + 1
     (4인 중 첫 탈락 → 4위, 두 번째 → 3위, ...)
  5. 보유 유닛 전부 풀에 반환
  6. 상점 미구매 챔피언 풀에 반환
  7. PlayerEliminated 이벤트 발행

탈락 후:
  - 다른 플레이어의 전투 관전 가능
  - 상점/배치 등 모든 조작 불가
  - 게임 종료까지 관전 또는 즉시 퇴장 가능
```

### 3.2 순위 결정

```
순위 = 탈락 역순

4인 게임:
  1위: 최후 생존자 (게임 종료 시)
  2위: 마지막에서 2번째 탈락
  3위: 마지막에서 3번째 탈락
  4위: 첫 번째 탈락

게임 종료 조건:
  - AlivePlayerCount ≤ 1
  - 마지막 생존자 = 1위

예외 - 동시 탈락으로 0명 생존:
  - 마지막 라운드에 남은 2명이 동시에 탈락 가능
  - 이 경우 HP가 더 많이 남은 쪽(덜 깎인 쪽)이 1위
  - 동일하면 공동 1위
```

### 3.3 순위 보상

```
순위에 따른 보상 (서버에서 지급):
  - 1위: 최대 보상
  - 2위: 중상 보상
  - 3~4위: 기본 보상 or 없음

보상 유형:
  - 랭킹 포인트 (LP / 트로피)
  - 게임 재화
  - 시즌 패스 경험치

보상 계산은 Quantum 밖에서 처리 (서버 RPC):
  게임 종료 → Quantum 세션 종료 → 결과 데이터 서버 전송 → 보상 지급
```

---

## 4. 매칭 시스템 (라운드 내)

### 4.1 PvP 매칭

```
4인 → 2개의 1v1 매치:

매칭 규칙:
  1. 직전 라운드 상대와 겹치지 않게
  2. 최근 N라운드 내 같은 상대 최소화
  3. 순위(HP) 기반 매칭은 하지 않음 (랜덤성 유지)

4인 매칭 알고리즘:
  - 가능한 조합: AB-CD, AC-BD, AD-BC (3가지)
  - 직전 라운드 조합 제외 → 2가지 중 랜덤 선택
  - 더 오래 안 만난 조합 우선

생존자 3명일 때:
  - 2개 매치: A vs B, C vs Ghost(A or B)
  - 1명이 2번 싸움 (고스트 매치)
  - 고스트 대상: 직전 라운드 고스트가 아닌 플레이어

생존자 2명일 때:
  - 1개 매치: A vs B (항상 동일)
```

### 4.2 고스트 (Ghost) 매치

```
생존자 홀수(3명)일 때 발생:

고스트 = 다른 플레이어의 보드 복제

고스트 매치:
  - 실제 플레이어 C vs 고스트(A의 보드 복제)
  - 고스트 승리 시: C에게 데미지 (A에게 이익 없음)
  - 고스트 패배 시: 아무 영향 없음 (원본 A에게 피해 없음)
  - 고스트 매치에서 커맨더 스킬은 사용 불가 (고스트 쪽)

고스트 선택:
  - 생존 플레이어 중 이미 매칭된 2명 중 한 명의 보드 복제
  - 직전 라운드 고스트 원본과 겹치지 않게
```

### 4.3 매칭 기록

```qtn
component MatchHistory {
    // 최근 4라운드 매칭 기록
    array<MatchRecord>[4] RecentMatches;
    Byte HistoryCount;
}

struct MatchRecord {
    player_ref Opponent;
    Boolean WasGhostMatch;
    player_ref GhostSource;    // 고스트였으면 원본
}
```

### 4.4 MatchmakingSystem

```csharp
public unsafe class MatchmakingSystem : SystemSignalsOnly,
    ISignalOnPhaseStarted
{
    public void OnPhaseStarted(Frame f, GamePhase phase)
    {
        if (phase != GamePhase.Combat) return;
        if (f.Global->CurrentRoundType != RoundType.PvP) return;

        var alivePlayers = GetAlivePlayers(f);

        switch (alivePlayers.Count)
        {
            case 4:
                Match4Players(f, alivePlayers);
                break;
            case 3:
                Match3Players(f, alivePlayers);
                break;
            case 2:
                Match2Players(f, alivePlayers);
                break;
        }
    }

    private void Match4Players(Frame f, List<PlayerRef> players)
    {
        // 3가지 조합 중 직전 라운드와 겹치지 않는 것 선택
        var combinations = new (int, int, int, int)[]
        {
            (0, 1, 2, 3), // A-B, C-D
            (0, 2, 1, 3), // A-C, B-D
            (0, 3, 1, 2), // A-D, B-C
        };

        // 직전 라운드 매칭 제외
        var lastMatch = GetLastMatchCombination(f);
        var candidates = combinations.Where(c => c != lastMatch).ToList();

        // 랜덤 선택 (Quantum RNG)
        var selected = candidates[f.RNG->Next(0, candidates.Count)];

        // 매치 설정
        SetMatch(f, 0, players[selected.Item1], players[selected.Item2], false);
        SetMatch(f, 1, players[selected.Item3], players[selected.Item4], false);
    }

    private void Match3Players(Frame f, List<PlayerRef> players)
    {
        // 고스트 대상 결정 (직전 고스트 아닌 플레이어)
        var ghostTarget = SelectGhostTarget(f, players);
        var nonGhost = players.Where(p => p != ghostTarget).ToList();

        // 2명 매치
        SetMatch(f, 0, nonGhost[0], nonGhost[1], false);

        // 고스트 매치: ghostTarget vs Ghost(nonGhost 중 한 명의 보드)
        var ghostSource = nonGhost[f.RNG->Next(0, 2)];
        SetMatch(f, 1, ghostTarget, ghostSource, isGhost: true);
    }

    private void SetMatch(Frame f, int matchIndex,
                           PlayerRef p1, PlayerRef p2, bool isGhost)
    {
        f.Global->CombatMatches[matchIndex] = new MatchPair
        {
            Player1 = p1,
            Player2 = p2,
            IsGhostMatch = isGhost,
            GhostSource = isGhost ? p2 : default
        };

        f.Events.MatchDecided(matchIndex, p1, p2, isGhost);
    }
}
```

---

## 5. 관전 시스템

### 5.1 관전 대상

```
플레이어가 볼 수 있는 보드:

전투 중:
  - 기본: 자신의 전투 (자기 매치)
  - SpectateCommand로 다른 매치로 전환 가능
  - 2개 매치 중 선택

Preparation 중:
  - 기본: 자기 보드
  - 다른 플레이어 보드 관전 가능 (읽기 전용)
  - 상대 배치를 보고 전략 조정

탈락 후:
  - 모든 매치/보드 자유롭게 관전
  - 조작 불가
```

### 5.2 SpectateCommand

```csharp
public class SpectateCommand : DeterministicCommand
{
    public Byte TargetMatchIndex;    // 0 또는 1 (매치 선택)
    // 또는 player_ref로 특정 플레이어 보드 관전

    public override void Serialize(BitStream stream)
    {
        stream.Serialize(ref TargetMatchIndex);
    }
}
```

### 5.3 관전 시 View 처리

```
관전 시 View Layer:
  1. 현재 표시 중인 보드를 대상 보드로 전환
  2. 카메라 이동 (또는 보드 데이터 교체)
  3. 상대 보드는 Quantum 상태에서 직접 읽기 (추가 동기화 불필요)
  4. UI: 관전 대상 플레이어 정보 표시 (HP, 레벨, 시너지)

Quantum 관점:
  - 관전은 순수 View 기능 (시뮬레이션에 영향 없음)
  - 모든 플레이어의 전체 게임 상태가 모든 클라이언트에 존재
  - SpectateCommand는 View 힌트용 (서버에서는 무시 가능)
```

---

## 6. 연승/연패 (Streak)

### 6.1 스트릭 규칙

```
연승 (Win Streak):
  - 연속 승리 횟수 기록
  - PvE 승리도 포함

연패 (Loss Streak):
  - 연속 패배 횟수 기록
  - PvE 패배도 포함

리셋 조건:
  - 승리 → 연패 리셋, 연승 +1
  - 패배 → 연승 리셋, 연패 +1
  - 무승부 → 연승/연패 카운터 유지 (리셋하지 않음)

보너스 골드 (라운드 시작 시):
  | 스트릭 | 보너스 |
  |--------|--------|
  | 1회 | +0 |
  | 2~3회 | +1 |
  | 4~5회 | +2 |
  | 6회+ | +3 |

연패에도 동일한 보너스 적용 → 약자 보호 (자금으로 만회)
```

### 6.2 스트릭 컴포넌트

```qtn
component PlayerStreak {
    Int32 CurrentStreak;      // 양수=연승, 음수=연패
    Int32 TotalWins;
    Int32 TotalLosses;
}
```

---

## 7. Quantum 컴포넌트 설계

### 7.1 플레이어 엔티티 구성

```
각 플레이어는 하나의 엔티티로 관리.
다음 컴포넌트들이 부착됨:

PlayerEntity:
  ├── PlayerState          (HP, 레벨, 생존 여부, 순위)
  ├── PlayerEconomy         (골드, XP, 레벨) ← 04_EconomySystem.md
  ├── PlayerBoard           (보드 + 벤치) ← 06_BoardSystem.md
  ├── PlayerShop            (상점 5슬롯) ← 05_ShopSystem.md
  ├── PlayerItemInventory   (미장착 아이템) ← 09_ItemSystem.md
  ├── PlayerSynergy         (시너지 상태) ← 08_SynergySystem.md
  ├── PlayerStreak          (연승/연패)
  ├── MatchHistory          (매칭 기록)
  └── CommanderSkillState   (커맨더 스킬) ← 07_CombatSystem.md
```

### 7.2 PlayerState 컴포넌트

```qtn
component PlayerState {
    player_ref PlayerRef;
    Int32 HP;
    Int32 MaxHP;
    Boolean IsAlive;
    Boolean IsEliminated;
    Int32 Rank;               // 0=미확정, 1~4=최종 순위
    Boolean IsConnected;      // 접속 상태
    Boolean IsReady;          // Preparation에서 준비 완료
}
```

### 7.3 PlayerSystem

```csharp
public unsafe class PlayerSystem : SystemMainThread,
    ISignalOnPlayerDamaged,
    ISignalOnPhaseStarted
{
    public override void Update(Frame f)
    {
        // Ready 커맨드 처리
        if (f.Global->CurrentPhase != GamePhase.Preparation) return;

        for (int p = 0; p < f.PlayerCount; p++)
        {
            var player = (PlayerRef)p;
            foreach (var cmd in f.GetPlayerCommands<ReadyCommand>(player))
            {
                var state = GetPlayerState(f, player);
                if (!state->IsAlive) continue;
                state->IsReady = true;
                f.Events.PlayerReady(player);
            }
        }

        // 전원 레디 체크
        if (AllPlayersReady(f))
        {
            // 페이즈 타이머 즉시 종료
            f.Global->PhaseTimer = FP._0;
        }
    }

    public void OnPhaseStarted(Frame f, GamePhase phase)
    {
        if (phase == GamePhase.Preparation)
        {
            // 모든 플레이어 Ready 초기화
            for (int p = 0; p < f.PlayerCount; p++)
            {
                var state = GetPlayerState(f, (PlayerRef)p);
                state->IsReady = false;
            }
        }
    }

    public void OnPlayerDamaged(Frame f, PlayerRef player,
                                 int damage, int remainingHP)
    {
        var state = GetPlayerState(f, player);
        state->HP = remainingHP;

        if (state->HP <= 0)
        {
            state->HP = 0;
            state->IsAlive = false;
            state->IsEliminated = true;
            state->Rank = f.Global->AlivePlayerCount; // 탈락 시점 순위

            // 보유 자원 풀 반환
            ReturnAllToPool(f, player);

            f.Events.PlayerEliminated(player, state->Rank);

            // 게임 종료 체크
            if (f.Global->AlivePlayerCount <= 1)
            {
                var winner = GetLastAlivePlayer(f);
                var winnerState = GetPlayerState(f, winner);
                winnerState->Rank = 1;

                f.Global->IsGameOver = true;
                f.Events.GameOver(winner);
            }
        }
    }
}
```

---

## 8. 재접속 (Reconnection)

### 8.1 재접속 흐름

```
Quantum의 결정론적 특성 덕분에 재접속이 간단:

접속 끊김:
  1. 클라이언트 연결 해제 감지
  2. IsConnected = false
  3. 플레이어의 입력이 없어짐 (자동으로 아무 조작 안 함)
  4. Preparation에서: 아무 것도 안 함 (기존 배치 유지)
  5. 전투에서: 자동 전투 계속 진행
  6. 커맨더 스킬 미사용

재접속:
  1. Photon 서버에 재연결
  2. Quantum 세션에 Rejoin
  3. 전체 게임 상태 스냅샷 수신 (또는 입력 로그 리플레이)
  4. 현재 프레임까지 따라잡기 (Fast-forward)
  5. IsConnected = true
  6. 정상 플레이 재개

제한 시간:
  - 접속 끊긴 후 N분(설정 가능) 이내 재접속 가능
  - 초과 시 → 자동 기권 (탈락 처리)
```

### 8.2 AFK(방치) 처리

```
접속은 유지하지만 입력이 없는 경우:

감지:
  - N라운드 연속 아무 입력 없음
  - Preparation에서 리롤/구매/배치 모두 0회

대응:
  - 경고 UI 표시 (본인에게)
  - 추가 M라운드 방치 시 → 자동 기권 (탈락)
  - 다른 플레이어에게 AFK 표시

Quantum에서는 AFK 감지를 위한 카운터만 관리:
  - PlayerState에 IdleRoundCount 필드 추가
  - 서버에서 최종 판단 (클라이언트 신뢰하지 않음)
```

---

## 9. 플레이어 초기화

### 9.1 게임 시작 시 초기화

```csharp
public void InitializePlayer(Frame f, PlayerRef player)
{
    var entity = f.Create();

    // PlayerState
    var state = f.Add<PlayerState>(entity);
    state->PlayerRef = player;
    state->HP = 100;
    state->MaxHP = 100;
    state->IsAlive = true;
    state->IsEliminated = false;
    state->Rank = 0;
    state->IsConnected = true;

    // PlayerEconomy
    var economy = f.Add<PlayerEconomy>(entity);
    economy->Gold = 0;     // 첫 라운드 시작 시 지급
    economy->Level = 1;
    economy->XP = 0;

    // PlayerBoard (빈 보드)
    var board = f.Add<PlayerBoard>(entity);
    // Tiles, Bench 모두 EntityRef.None으로 초기화

    // PlayerShop
    var shop = f.Add<PlayerShop>(entity);
    shop->IsLocked = false;

    // PlayerItemInventory
    f.Add<PlayerItemInventory>(entity);

    // PlayerSynergy
    f.Add<PlayerSynergy>(entity);

    // PlayerStreak
    f.Add<PlayerStreak>(entity);

    // MatchHistory
    f.Add<MatchHistory>(entity);

    // CommanderSkillState
    var cmdSkill = f.Add<CommanderSkillState>(entity);
    // 로비에서 선택한 커맨더 스킬 세팅 (매치 시작 데이터에서)
}
```

---

## 10. View 이벤트

```qtn
event PlayerReady {
    player_ref Player;
}

event PlayerDamaged {
    player_ref Player;
    Int32 Damage;
    Int32 RemainingHP;
}

event PlayerEliminated {
    player_ref Player;
    Int32 Rank;
}

event GameOver {
    player_ref Winner;
}

event MatchDecided {
    Byte MatchIndex;
    player_ref Player1;
    player_ref Player2;
    Boolean IsGhostMatch;
}

event PlayerReconnected {
    player_ref Player;
}

event PlayerDisconnected {
    player_ref Player;
}
```

---

## 11. 전체 플레이어 데이터 흐름

```
┌───────────────┐
│  로비 서버     │ ── 매칭 요청/결과
└──────┬────────┘
       │ 4인 매칭 완료
       ▼
┌───────────────┐
│ Photon Room   │ ── Quantum 세션 생성
│ (4 Players)   │
└──────┬────────┘
       │
       ▼
┌───────────────────────────────────────────────────────┐
│ Quantum Simulation                                     │
│                                                        │
│  Player 0 ─┬─ PlayerState (HP, Rank)                  │
│             ├─ PlayerEconomy (Gold, Level)              │
│             ├─ PlayerBoard (7×4 + Bench 9)             │
│             ├─ PlayerShop (5 slots)                    │
│             ├─ PlayerItemInventory (10 slots)          │
│             ├─ PlayerSynergy (32 traits)               │
│             ├─ PlayerStreak (Win/Loss)                 │
│             ├─ MatchHistory (4 rounds)                 │
│             └─ CommanderSkillState (2 slots)           │
│                                                        │
│  Player 1 ─ (동일 구조)                                │
│  Player 2 ─ (동일 구조)                                │
│  Player 3 ─ (동일 구조)                                │
│                                                        │
│  Global ─── GamePhase, Round, Timer, Matches, Pool     │
└───────────────────────────────────────────────────────┘
       │
       ▼ (Verified Frame 읽기)
┌───────────────────────────────────────────────────────┐
│ Unity View Layer                                       │
│                                                        │
│  각 클라이언트는 전체 상태를 볼 수 있음                   │
│  → 자기 보드: 조작 가능                                 │
│  → 상대 보드: 관전만 가능                               │
│  → 상점/인벤토리: 자기 것만 표시                         │
└───────────────────────────────────────────────────────┘
```
