# 네트워크 동기화 설계

> Photon Quantum 3 기반의 네트워크 아키텍처, 동기화 전략, 매칭, 재접속 처리를 정의한다.
>
> **GameMode 적용 범위**: 이 문서는 주로 **Competitive 모드 (4인 멀티플레이어)** 를 다룬다.
> ClassicBattle / PvECampaign 모드는 Photon Cloud 없이 **로컬 Quantum 세션**으로 실행된다.
> Quantum은 로컬 전용 모드를 기본 지원하므로, 시뮬레이션 코드는 모드와 무관하게 동일하다.
> 네트워크 관련 기능(매칭, 재접속, 안티치트)은 Competitive 모드에서만 활성화된다.

---

## 1. Quantum 네트워크 모델 개요

### 결정론적 Predict/Rollback

```
           Photon Cloud (Quantum Server)
          ┌─────────────────────────────┐
          │  - 입력 수집 & 배포          │
          │  - 클록 동기화              │
          │  - 시뮬레이션 검증           │
          │  - 매칭 서버 역할           │
          └────┬───────┬───────┬───────┘
               │       │       │
        Input  │       │       │  Input
               ▼       ▼       ▼
          ┌────────┐┌────────┐┌────────┐┌────────┐
          │Client 1││Client 2││Client 3││Client 4│
          │        ││        ││        ││        │
          │동일한   ││동일한   ││동일한   ││동일한   │
          │시뮬레이션││시뮬레이션││시뮬레이션││시뮬레이션│
          └────────┘└────────┘└────────┘└────────┘

모든 클라이언트가 동일한 시뮬레이션을 실행.
입력만 교환하면 결과가 동일하게 보장됨.
```

### 오토체스에서의 장점

```
일반적인 상태 동기화 (Fusion 등):
  → 유닛 32개의 HP, Position, State, Target... = 매 틱 대량 데이터 전송
  → 대역폭: 유닛 수에 비례하여 증가

Quantum 입력 동기화:
  → 계획 페이즈: 구매/배치/리롤 Command만 전송 (각 수십 바이트)
  → 전투 페이즈: 플레이어 입력 없음 = 전송 데이터 거의 0
  → 대역폭: 유닛 수에 무관, 거의 일정
```

---

## 2. 동기화 대상 분류

### Quantum에서 자동 동기화되는 것

Quantum의 결정론적 모델에서는 **모든 게임 상태가 자동 동기화**된다. 모든 클라이언트가 동일한 시뮬레이션을 돌리므로 별도 동기화 코드가 불필요하다.

| 데이터 | 처리 방식 | 비고 |
|--------|----------|------|
| 유닛 HP, 위치, 상태 | 자동 (시뮬레이션) | 동기화 코드 불필요 |
| 플레이어 골드, XP, 레벨 | 자동 (시뮬레이션) | |
| 샵 내용 | 자동 (시뮬레이션) | 공유 풀에서 결정론적 추출 |
| 전투 결과 | 자동 (시뮬레이션) | 모든 클라이언트 결과 동일 |
| 시너지 상태 | 자동 (시뮬레이션) | |
| 챔피언 풀 잔량 | 자동 (시뮬레이션) | |

### 클라이언트 → 서버로 전송하는 것 (Command)

| Command | 페이즈 | 데이터 크기 |
|---------|--------|-----------|
| BuyUnitCommand | Preparation | ~4 bytes |
| SellUnitCommand | Preparation | ~8 bytes |
| RerollShopCommand | Preparation | ~1 byte |
| BuyXPCommand | Preparation | ~1 byte |
| MoveUnitCommand | Preparation | ~16 bytes |
| EquipItemCommand | Preparation + **Combat** | ~16 bytes |
| UnequipItemCommand | Preparation | ~8 bytes |
| ReadyCommand | Preparation | ~1 byte |
| UseCommanderSkillCommand | **Combat** | ~12 bytes |
| SpectateCommand | Combat | ~4 bytes |
| SelectDraftCommand | SharedDraft | ~4 bytes |

> 전투 페이즈에서도 아이템 장착과 커맨더 스킬 Command가 발생하지만, 빈도가 낮아 대역폭 영향은 미미하다.

### 로컬에서만 처리하는 것 (View)

| 데이터 | 처리 위치 | 비고 |
|--------|----------|------|
| Spine 애니메이션 상태 | View (Unity) | 시뮬레이션과 무관 |
| VFX/파티클 | View (Unity) | |
| UI 레이아웃/애니메이션 | View (Unity) | |
| 카메라 위치/줌 | View (Unity) | |
| 사운드 | View (Unity) | |
| 관전 시 카메라 대상 | View (Unity) | SpectateCommand와 별개로 카메라 자체는 로컬 |

---

## 3. 세션 생명주기

### 3.1 매칭 → 게임 시작

```
── Competitive 모드 (4인 멀티플레이어) ──

[로비 - Unity/Photon Realtime]
  │
  ├─ 1. Photon Realtime 연결 (AppId)
  ├─ 2. 매칭 요청 → Photon 룸 생성/참가
  ├─ 3. 4인 모집 완료
  │
  ▼
[Quantum 세션 시작]
  │
  ├─ 4. QuantumRunner.StartGame(startParams)
  ├─ 5. RuntimeConfig 전달 (GameModeConfig + 게임 설정)
  ├─ 6. RuntimePlayer 전달 (각 플레이어 정보)
  │
  ▼
[시뮬레이션 시작]
  │
  ├─ 7. GameInitSystem → GameModeConfig에 따라 초기화 분기
  ├─ 8. 첫 번째 Preparation 페이즈 시작
  └─ 9. 각 플레이어에게 초기 골드/레벨 지급

── ClassicBattle / PvECampaign 모드 (싱글 플레이) ──

[모드 선택]
  │
  ├─ 1. Photon 연결 없음 (오프라인)
  ├─ 2. 로컬 Quantum 세션 생성 (QuantumRunner.StartGame, offline=true)
  ├─ 3. RuntimeConfig에 GameModeConfig 포함
  │     - ClassicBattle: PlayerRoster (보유 캐릭터)
  │     - PvECampaign: 초기 골드/레벨/챔피언 풀
  │
  ▼
[시뮬레이션 시작]
  │
  ├─ 4. GameInitSystem → UnitSource에 따라 벤치 초기화
  └─ 5. GameLoopSystem → RoundEndCondition에 따라 흐름 결정
```

### 3.2 RuntimeConfig (게임 설정)

```csharp
// 게임 시작 시 한 번 설정. 모든 클라이언트에 동일하게 전달.
public class GameConfig : AssetObject
{
    // 게임 모드 (02_GameLoop.md §0 참조)
    public AssetRef<GameModeConfigAsset> GameModeConfig;

    // 라운드 구성
    public AssetRef<RoundConfigAsset> RoundConfig;

    // 경제 설정
    public int StartingGold;              // 초기 골드
    public int BaseIncomePerRound;        // 라운드당 기본 수입
    public int InterestRate;              // 이자 비율 (10골드당 1)
    public int MaxInterest;               // 최대 이자
    public int RerollCost;                // 리롤 비용
    public int XPBuyCost;                 // XP 구매 비용
    public int XPBuyAmount;               // XP 구매량

    // 전투 설정
    public FP CombatTimeout;              // 전투 제한 시간
    public int PlayerStartHP;             // 플레이어 초기 HP

    // 챔피언 풀 설정
    public AssetRef<ChampionPoolAsset> ChampionPool;
}
```

### 3.3 RuntimePlayer (플레이어 정보)

```csharp
// 각 플레이어의 고유 정보. 게임 시작 시 전달.
public partial class RuntimePlayer
{
    public string Nickname;
    public int ProfileIcon;
    // 필요 시 덱/스킨 정보 등
}
```

---

## 4. 페이즈별 동기화 상세

### 4.1 Preparation 페이즈

```
동기화 흐름:
  Player Input → Command → Quantum Server → 모든 Client에 배포

시퀀스:
  1. 플레이어가 샵에서 챔피언 터치
  2. Unity View → BuyUnitCommand 생성
  3. QuantumRunner.Default.Game.AddCommand(buyCommand)
  4. Quantum Server가 Command를 확정 틱에 삽입
  5. 모든 클라이언트의 ShopSystem에서 동일하게 처리
  6. 결과: 모든 클라이언트에서 동일한 상태

예측(Prediction):
  - 로컬 클라이언트는 Command를 즉시 예측 실행
  - 서버 확인 후 불일치 시 자동 롤백 + 재시뮬레이션
  - 오토체스에서는 커맨드가 단순하여 롤백 발생 드묾
```

### 4.2 Combat 페이즈

```
동기화 흐름:
  자동 전투 + 제한적 플레이어 입력 (아이템 장착, 커맨더 스킬)

  모든 클라이언트가 동일한 초기 상태에서 시뮬레이션 실행:
    - 동일한 유닛 배치
    - 동일한 스탯/시너지
    - 동일한 RNG 시드 (Quantum 내장)
    → 모든 클라이언트에서 동일한 전투 결과

  전투 중 Command:
    - EquipItemCommand: 아이템 장착 (해제 불가)
      → 벤치/인벤토리의 아이템을 전투 중인 유닛에 장착
      → 즉시 스탯 반영
    - UseCommanderSkillCommand: 커맨더 스킬 발동
      → 전투에 직접 영향 (힐, 데미지, 버프 등)
      → 타겟 지정형은 위치 좌표 포함
    - 이 Command들도 결정론적으로 처리 (모든 클라이언트 동일 결과)

대역폭:
  전투 페이즈에서 네트워크 트래픽은 여전히 매우 낮음
  (커맨더 스킬/아이템 장착은 비빈번 이벤트)
```

### 4.3 관전 (Spectating)

```
다른 플레이어의 전투를 관전하는 기능:

모든 클라이언트가 전체 게임 상태를 가지고 있으므로:
  - 추가 네트워크 통신 없이 관전 가능
  - 카메라만 다른 보드로 이동하면 됨
  - Unity View 레이어에서만 처리

SpectateCommand는:
  - 시뮬레이션 상태에 관전 대상을 기록 (통계용)
  - 실제 카메라 이동은 View에서 로컬 처리
```

---

## 5. 재접속 처리

### 5.1 일시적 연결 끊김

```
Quantum의 자동 처리:
  1. 연결 끊김 감지
  2. 로컬 시뮬레이션은 입력 예측으로 계속 진행
  3. 재연결 시 서버에서 누적 입력 수신
  4. 롤백 + 재시뮬레이션으로 정확한 상태 복구
  5. 플레이어 경험: 짧은 끊김 → 자동 복구

오토체스 특성:
  - 전투 중에는 입력이 없으므로 끊김 영향 거의 없음
  - 계획 페이즈에서 끊기면 Command가 유실될 수 있음
  - 재접속 후 View만 갱신하면 됨
```

### 5.2 장시간 연결 끊김 / 이탈

```
처리 전략:
  1. 연결 끊김 지속 시간 체크
  2. N초(예: 30초) 이상 미접속 → 봇 대체
  3. Quantum Bot SDK로 자동 플레이:
     - 계획 페이즈: 기본 AI (가장 비싼 챔피언 구매, 레벨업)
     - 전투 페이즈: 입력 불필요 (원래 자동)
  4. 재접속 시:
     - 스냅샷에서 현재 상태 복원
     - 봇 비활성화, 플레이어 복귀
```

### 5.3 Quantum 스냅샷 복구

```csharp
// Quantum 내장 기능: 스냅샷 기반 상태 복구
// 재접속 시 서버에서 최신 스냅샷을 전달받아 복원

// Unity 측 재접속 처리
public class ReconnectHandler : MonoBehaviour
{
    public async UniTask Reconnect()
    {
        // 1. Photon Realtime 재연결
        await PhotonNetwork.ReconnectAndRejoin();

        // 2. Quantum 세션 재참가
        // Quantum이 자동으로 최신 스냅샷 수신 + 캐치업
        QuantumRunner.StartGame(reconnectParams);

        // 3. View 재구성
        // Frame 데이터를 읽어서 모든 View 오브젝트 재생성
        RebuildAllViews();
    }
}
```

---

## 6. 지연(Latency) 처리

### 오토체스와 지연

```
오토체스는 지연에 매우 관대한 장르:

계획 페이즈:
  - 드래그 앤 드롭, 버튼 클릭 등 비실시간 입력
  - 100~300ms 지연도 체감하기 어려움
  - Command 예측 실행으로 즉시 반영 느낌

전투 페이즈:
  - 입력 자체가 없음
  - 시뮬레이션은 모든 클라이언트에서 로컬 실행
  - 지연과 완전히 무관

결론:
  - 모바일 환경의 높은 지연(100~500ms)에서도 문제없음
  - Quantum의 predict/rollback이 제공하는 부드러운 경험
```

### 입력 예측 상세

```
플레이어가 챔피언 구매 시:

Tick 100: 플레이어가 구매 버튼 클릭
  → 로컬: 예측 실행 (즉시 샵에서 사라지고, 벤치에 등장)
  → 네트워크: Command 전송

Tick 102: 서버에서 Command 확정 (지연 2틱)
  → 확정 결과가 예측과 동일 → 롤백 없음, 매끄러움
  → 만약 다른 플레이어가 같은 틱에 동일 챔피언 구매했다면:
     → 서버가 선착순 결정
     → 늦은 플레이어의 Command는 실패
     → 해당 클라이언트만 롤백 + 골드 환불
```

---

## 7. 매치메이킹 (게임 매칭)

### 7.1 게임 매칭 (로비 → 게임 시작)

```
Photon Realtime 룸 기반:

1. 플레이어가 "게임 찾기" 버튼 클릭
2. Photon Realtime에서 적절한 룸 검색
   - 대기 중인 룸이 있으면 참가
   - 없으면 새 룸 생성
3. 룸에 4인 도달하면 자동 시작
4. Quantum 세션 생성, 시뮬레이션 시작

룸 설정:
  - MaxPlayers: 4
  - 타임아웃: 60초 (4인 미달 시 봇으로 채움)
  - 매칭 조건: MMR 범위 (선택적)
```

### 7.2 라운드 매칭 (상대 배정)

```csharp
// MatchmakerSystem - 라운드별 상대 배정
public unsafe class MatchmakerSystem : SystemSignalsOnly,
    ISignalOnPhaseStarted
{
    public void OnPhaseStarted(Frame f, GamePhase phase)
    {
        if (phase != GamePhase.Combat) return;

        var global = f.Global;
        var alivePlayers = GetAlivePlayers(f);

        if (global->CurrentRoundType == RoundType.PvE)
        {
            // PvE: 각 플레이어가 동일 몬스터와 전투
            SetupPvEMatches(f, alivePlayers);
            return;
        }

        // PvP 매칭
        var rng = f.RNG;  // 결정론적 RNG
        Shuffle(alivePlayers, rng);

        // 직전 상대 회피 (가능한 경우)
        AvoidPreviousOpponents(f, alivePlayers);

        // 2인씩 매칭
        for (int i = 0; i < alivePlayers.Count / 2; i++)
        {
            global->CombatMatches[i] = new MatchPair
            {
                Player1 = alivePlayers[i * 2],
                Player2 = alivePlayers[i * 2 + 1],
                IsGhostMatch = false
            };
        }

        // 홀수 인원 → 고스트 매치
        if (alivePlayers.Count % 2 == 1)
        {
            var lastPlayer = alivePlayers[alivePlayers.Count - 1];
            var ghostSource = alivePlayers[rng->Next(0, alivePlayers.Count - 1)];

            global->CombatMatches[alivePlayers.Count / 2] = new MatchPair
            {
                Player1 = lastPlayer,
                Player2 = ghostSource,
                IsGhostMatch = true,
                GhostSource = ghostSource
            };
        }
    }
}
```

---

## 8. 안티 치트

### Quantum 내장 보호

```
1. 결정론적 검증
   - Photon Cloud의 Quantum Server가 시뮬레이션을 독립 실행
   - 클라이언트 결과와 서버 결과 비교
   - 불일치 시 클라이언트 강제 동기화 (서버가 진실)

2. 입력만 전송
   - 클라이언트가 상태를 직접 수정할 수 없음
   - "내 HP를 무한으로" 같은 메모리 핵이 무의미
   - 서버가 모든 상태를 결정

3. Command 검증
   - ShopSystem에서 골드 충분한지 서버에서도 동일하게 체크
   - 불가능한 Command는 시뮬레이션에서 무시됨
```

### 추가 보호 (선택적)

```
1. 서버 사이드 검증 강화
   - Quantum Custom Plugin으로 서버에서 추가 검증 로직 실행
   - 비정상 패턴 감지 (과도한 리롤, 비정상 골드 등)

2. 클라이언트 무결성
   - 현행 ObfuscatorFloat/Int → Quantum에서는 불필요 (서버 권위)
   - 필요 시 Unity 측 메모리 보호는 별도 솔루션
```

---

## 9. 대역폭 예측

### 4인 오토체스 예상 대역폭

| 페이즈 | 주요 트래픽 | 예상 대역폭 (per client) |
|--------|-----------|------------------------|
| Preparation | Command (구매/배치/리롤) | 0.1~1 KB/s |
| Combat | 빈 입력 프레임 | 0.05~0.1 KB/s |
| Result | 없음 (시뮬레이션 계산) | 0.05 KB/s |
| SharedDraft | 선택 Command | 0.1 KB/s |

> **총 예상: 평균 0.5 KB/s 이하**
> 모바일 3G 환경에서도 문제없는 수준.
> Fusion 대비 10~100배 적은 대역폭.

---

## 10. 아키텍처 다이어그램 (네트워크 포함)

```
┌─────────────────────────────────────────────────┐
│                Photon Cloud                      │
│  ┌──────────┐  ┌──────────────────┐             │
│  │ Realtime │  │ Quantum Server   │             │
│  │ (매칭)   │  │ (시뮬레이션 검증) │             │
│  └────┬─────┘  └────────┬─────────┘             │
└───────┼─────────────────┼───────────────────────┘
        │                 │
        │    Input/Command│ (수 bytes/tick)
        │                 │
┌───────┴─────────────────┴───────────────────────┐
│              Client (Unity + Quantum)            │
│                                                  │
│  ┌─────────────────────────────────────────┐    │
│  │  Quantum Simulation (로컬 실행)          │    │
│  │  - 전체 게임 상태 보유                    │    │
│  │  - 4인 모든 데이터 시뮬레이션             │    │
│  │  - 결정론적 결과 보장                    │    │
│  └──────────────────┬──────────────────────┘    │
│                     │ Frame 읽기                  │
│  ┌──────────────────▼──────────────────────┐    │
│  │  Unity View Layer                        │    │
│  │  - 내 보드: 실시간 렌더링                  │    │
│  │  - 상대 보드: 미니맵 / 관전 시 렌더링       │    │
│  │  - UI: 샵, 시너지, 타이머, HP             │    │
│  └─────────────────────────────────────────┘    │
└──────────────────────────────────────────────────┘
```
