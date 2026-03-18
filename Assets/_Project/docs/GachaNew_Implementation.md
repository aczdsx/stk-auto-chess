# Gacha New — 크로스헤어 마크 탐색 미니게임 구현 문서

## 1. 개요

기존 가챠 연출(`GachaFxByTen`)을 대체하는 새로운 인터랙티브 가챠 시스템.
유저가 우주 맵에서 크로스헤어를 드래그하며 숨겨진 마크를 찾아내는 미니게임 형태.
서버 가챠 결과(R/SR/SSR)에 따라 마크 등급이 결정되며, 발견 후 캐릭터 팝업 → 결과 팝업 순으로 연출.

---

## 2. 전체 흐름

```
GachaPopup (1회/10회 모집 버튼)
    ↓
GachaCommonCharacterLayer.OnClickGacha10Button()
    ↓
GachaBaseLayer.ProcessCharacterGacha(GachaCountType)
    ├── SetGachaData() → ONE: _specGachaDataOneTime / TEN: _specGachaDataTenTime
    ├── 서버 API: NetManager.Instance.Gacha.DrawAsync(gacha_id)
    ├── 응답 → List<RewardItem> 변환 (1개 또는 10개)
    ├── 기존 Gacha_New 씬 언로드 (다시모집 시)
    ├── Addressables.LoadSceneAsync("Gacha_New", Additive) ← 핵심
    ├── 씬 root objects에서 GachaNewController 검색
    ├── GachaPopup.SetCanvasTargetDisplay(1) (메인 UI 숨김)
    └── controller.SetItem(results, spec, onContinue, onCleanup)
            ↓
    [GachaNewController — 초기화]
        ├── 등급 분석 (R/SR/SSR, 최고 등급, SSR 포함 여부)
        ├── Timeline 분기 (track.muted로 Star SSR/SR/R + Legend/Normal 제어)
        ├── PlayableDirector.Play() → 도입 연출 재생
        ├── SpawnMarks() → 마크 프리팹 인스턴스 랜덤 배치
        ├── UI 초기화 (카운터 "0/N", SearchCountItem 전체 비활성)
        └── Timeline 완료 후 → crosshair 입력 활성화
            ↓
    [탐색 Phase]
        ├── 유저 터치 → Crosshair 나타남 (scale 0→1, 터치 위치)
        ├── 드래그 → Crosshair 이동 + 마크 범위 판정
        ├── 마크 범위 진입 → mark.Activate() (scale pop + 파티클 + SFX)
        ├── OnMarkFound → 카운터 갱신 + SearchCountItem 순차 활성화
        └── N/N 완료 → OnAllMarksFound()
            ↓
    [완료 Phase]
        ├── GachaGetCharacterPopup × N회 순차 표시 (각 캐릭터별)
        ├── GachaResultPopup 최종 표시
        └── 유저 선택:
            ├── "확인" → OnClickBack() → 씬 언로드 → GachaPopup 복원
            └── "다시모집" → onContinue() → ProcessCharacterGacha 재호출 → 새 씬 로드
```

---

## 3. 신규 스크립트

### 3.1 GachaNewController.cs

**위치**: `Assets/_Project/Addressables/Remote/Gacha_New/Scenes/GachaNewController.cs`
**역할**: 미니게임 메인 컨트롤러

| 메서드 | 역할 |
|--------|------|
| `SetItem(datas, spec, onContinue, onCleanup)` | 진입점 — 등급 분석, Timeline, 마크 생성, UI 초기화 |
| `ConfigureTimeline(hasSSR, highestGrade)` | Timeline 트랙 mute 제어 (눈 아이콘) |
| `SetTimelineTrackMute(trackName, muted)` | GroupTrack 내부 서브트랙 포함 검색 |
| `SpawnMarks(datas)` | 등급별 마크 프리팹 인스턴스, 랜덤 배치 (padding 100px, 최소 거리 120px) |
| `OnMarkFound(mark)` | 카운터 갱신 + SearchCountItem 활성화 + 완료 판정 |
| `ShowCharacterSequenceAsync()` | GachaGetCharacterPopup N회 → GachaResultPopup |
| `OnClickSkip()` | Timeline 스킵 → 즉시 캐릭터 시퀀스 진행 |
| `OnClickBack()` | GachaPopup 복원 + 사운드 복원 + 씬 언로드(onCleanup) |
| `GetUnfoundMarks()` | CrosshairController에서 사용할 마크 리스트 |
| `GetGradeFromRewardItem(item)` | RewardItem → CharacterId → grade_type (fallback: RARE) |

**핵심 설계:**
- `_totalMarkCount = Mathf.Min(datas.Count, 10)` → 1회 모집(1개), 10회 모집(10개) 모두 대응
- Timeline 분기는 `track.muted` 만 사용 (GameObject.SetActive 미사용)
- 씬 정리는 `_onCleanup` 콜백 → GachaBaseLayer가 `Addressables.UnloadSceneAsync` 호출

### 3.2 CrosshairController.cs

**위치**: `Assets/_Project/Addressables/Remote/Gacha_New/Scenes/CrosshairController.cs`
**부착 대상**: TouchInputArea (Image alpha=0, raycastTarget=true)

| 역할 | 상세 |
|------|------|
| 입력 수신 | `IPointerDownHandler`, `IDragHandler`, `IPointerUpHandler` |
| 좌표 변환 | `ScreenPointToLocalPointInRectangle` → UI 로컬 좌표 |
| 범위 판정 | `Vector2.Distance` ≤ `DetectionRadius` → `mark.Activate()` |
| 시각 피드백 | LitMotion scale 0↔1 애니메이션 (OutBack/InBack) |

**핵심 설계:**
- 초기 상태: `_crosshairVisual.gameObject.SetActive(false)` + `scale = 0`
- 터치 시: `SetActive(true)` → scale 0→1 팝 애니메이션 → 터치 위치에 나타남
- 드래그: 손 따라 이동 + 매 프레임 마크 판정
- 손 뗌: scale 1→0 → `SetActive(false)`
- `SetEnabled(false)`: 즉시 숨김 (애니메이션 없이)

### 3.3 MarkController.cs

**위치**: `Assets/_Project/Addressables/Remote/Gacha_New/Scenes/MarkController.cs`
**부착 대상**: Mark_R / Mark_SR / Mark_SSR 프리팹

| 역할 | 상세 |
|------|------|
| 상태 관리 | `IsFound` 플래그 (중복 활성화 방지) |
| 활성화 연출 | scale 0→1 (0.3초, EaseOutBack) + ParticleSystem.Play + SFX |
| 위치/범위 | `anchoredPosition`, `_detectionRadius = 70f` |
| 안전 처리 | `destroyCancellationToken` 연동 |

---

## 4. 수정된 기존 파일

### 4.1 GachaBaseLayer.cs

**위치**: `Assets/_Project/Scripts/UI/Popup/Layers/GachaBaseLayer.cs`

**주요 변경:**
- `Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01")` → `Addressables.LoadSceneAsync("Gacha_New", LoadSceneMode.Additive)`
- 핸들 타입: `AsyncOperationHandle<GameObject>` → `AsyncOperationHandle<SceneInstance>`
- 씬 root objects에서 `GachaNewController` 검색
- `UnloadGachaScene()` 메서드 (4중 유효성 체크: `IsValid`, `IsDone`, `Status == Succeeded`, `Scene.isLoaded`)
- `SetItem` 호출에 `onCleanup` 콜백 추가
- 다시모집 시 기존 씬 자동 언로드 후 새 씬 로드

---

## 5. Timeline 트랙 구조 및 분기

```
GachaTimeline (PlayableDirector)
├── Left (Animator)           항상 재생
├── Right (Animator)          항상 재생
├── Main Camera (Animator)    항상 재생
├── Control Track (3)         항상 재생
├── Map (Animator)            항상 재생
├── Black_BG (Animator)       항상 재생
├── Galaxy (Animator)         항상 재생
├── Black_BG (Animator)       항상 재생
├── HUD_FX (Animator)         항상 재생
│
├── Star (GroupTrack)
│   ├── Start                 항상 재생 (공통 별 도입)
│   ├── SSR                   최고등급 LEGENDARY일 때만 unmute
│   ├── SR                    최고등급 EPIC일 때만 unmute
│   └── R                     최고등급 RARE일 때만 unmute
│
├── Legend (GroupTrack)        SSR 1개 이상 포함 시 unmute
│   └── Control Track (1)
│
└── Normal (GroupTrack)        SSR 0개 시 unmute
    └── Control Track (2)
```

**분기 코드:**
```csharp
SetTimelineTrackMute("SSR", highestGrade != GradeType.LEGENDARY);
SetTimelineTrackMute("SR",  highestGrade != GradeType.EPIC);
SetTimelineTrackMute("R",   highestGrade != GradeType.RARE);
SetTimelineTrackMute("Legend", !hasSSR);
SetTimelineTrackMute("Normal", hasSSR);
```

> **중요**: `track.muted` (Timeline 눈 아이콘)만 사용. `GameObject.SetActive`는 사용하지 않음.

---

## 6. 씬 하이어라키

```
Gacha_New (Additive Scene)
├── Gacha
│   └── Timeline (PlayableDirector — GachaTimeline)
│       ├── Main Camera, BG, FX, Star, Map, Galaxy ...
│       └── UICanvas (ScreenSpace-Camera)
│           └── Panel (SafeArea)
│               ├── FX (기존 파티클)
│               ├── MarkContainer (full-stretch) ← 마크 인스턴스 배치 영역
│               ├── TouchInputArea (Image alpha=0) ← CrosshairController 부착
│               │   └── Crosshair (프리팹 인스턴스) ← 크로스헤어 비주얼
│               ├── Content (카운터 UI, Skip 버튼)
│               └── Footer
│                   └── CountGroup
│                       ├── BgGroup (장식)
│                       └── ListGroup (GachaSearchCountItem × 10)
├── EventSystem
└── GachaNewManager ← GachaNewController 부착
```

---

## 7. 씬 로드/언로드 생명주기

```
[로드]
GachaBaseLayer.ProcessCharacterGacha()
  → Addressables.LoadSceneAsync("Gacha_New", LoadSceneMode.Additive)
  → _gachaSceneHandle에 핸들 저장

[정상 종료 — 확인 버튼]
GachaNewController.OnClickBack()
  → GachaPopup.SetCanvasTargetDisplay(0)  (메인 UI 복원)
  → SoundManager 복원 (BGM, SFX, IsPlayingGacha)
  → _onCleanup?.Invoke()
    → GachaBaseLayer.UnloadGachaScene()
      → Addressables.UnloadSceneAsync(_gachaSceneHandle)

[다시모집]
GachaResultPopup → OnContinueGacha 콜백
  → GachaBaseLayer.ProcessCharacterGacha() 재호출
  → UnloadGachaScene() (기존 씬 정리)
  → 새 Gacha_New 씬 Additive 로드
  → 전체 플로우 반복

[비정상 종료 — GachaPopup 자체 닫힘]
GachaBaseLayer.OnDestroy()
  → UnloadGachaScene() (안전 정리)
```

---

## 8. Footer SearchCountItem 연동

Footer의 `ListGroup` 하위에 `GachaSearchCountItem` × 10개가 존재.
마크 발견 시 순차적으로 활성화.

- **초기화**: `SetItem()` → 전체 `SetActive(false)`
- **마크 발견**: `OnMarkFound()` → `_searchCountItems[_foundCount - 1].SetActive(true)`
- **Skip**: `ActivateAllSearchCountItems()` → `_totalMarkCount` 개수만큼 활성화
- **1회 모집**: 10개 중 1개만 활성화 (나머지 비활성 유지)

---

## 9. 1회 모집 vs 10회 모집

| 항목 | 1회 모집 | 10회 모집 |
|------|---------|----------|
| 서버 응답 | RewardItem 1개 | RewardItem 10개 |
| 마크 생성 | 1개 | 10개 |
| `_totalMarkCount` | 1 | 10 |
| 카운터 UI | "0/1" → "1/1" | "0/10" → "10/10" |
| SearchCountItem | 1개만 활성화 | 10개 활성화 |
| 캐릭터 팝업 | 1회 | 10회 |
| Timeline 분기 | 동일 (최고 등급 기준) | 동일 |

---

## 10. 주요 참조 패턴

### GachaFxByTen (기존 — 대체됨)
- `Assets/_Project/Addressables/Remote/Gacha_STK/Script/GachaFxByTen.cs`
- Singleton 기반, 프리팹 인스턴스화
- SetItem, Skip, Back, SSR 분기 패턴을 GachaNewController에서 계승

### UI 팝업 재사용
- `GachaGetCharacterPopup` — 캐릭터 획득 연출 (LD 일러스트 + SD 캐릭터)
- `GachaResultPopup` — 최종 결과 리스트 + "확인"/"다시모집" 버튼
- `GachaPopup` — 가챠 메인 화면 (`SetCanvasTargetDisplay` 전환)

### 사운드 패턴
```csharp
// 시작
SoundManager.Instance.StopBGM();
SoundManager.Instance.IsPlayingGacha = true;
SoundManager.Instance.PlaySFX(hasSSR ? snd_sfx_gacha_start_001 : snd_sfx_gacha_start_002);

// Skip
SoundManager.Instance.StopAllSound();

// 종료 (OnClickBack)
SoundManager.Instance.StopSFX(snd_sfx_gacha_result_ambient_001);
SoundManager.Instance.PlayBGM(snd_bgm_command01);
SoundManager.Instance.IsPlayingGacha = false;
```

---

## 11. 파일 목록

| 파일 | 타입 | 역할 |
|------|------|------|
| `Gacha_New/Scenes/GachaNewController.cs` | 신규 | 메인 컨트롤러 |
| `Gacha_New/Scenes/CrosshairController.cs` | 신규 | 크로스헤어 입력/판정 |
| `Gacha_New/Scenes/MarkController.cs` | 신규 | 마크 상태/연출 |
| `Scripts/UI/Popup/Layers/GachaBaseLayer.cs` | 수정 | Additive 씬 로드 진입점 |
| `Gacha_New/Scenes/Gacha_New.unity` | 수정 | 씬 하이어라키 재구성 |
| `Gacha_New/Prefab/Mark_R.prefab` | 수정 | MarkController 부착 |
| `Gacha_New/Prefab/Mark_SR.prefab` | 수정 | MarkController 부착 |
| `Gacha_New/Prefab/Mark_SSR.prefab` | 수정 | MarkController 부착 |
| `Gacha_New/Timeline/GachaTimeline.playable` | 기존 | Timeline 에셋 |
| `Gacha_New/Prefab/Crosshair.prefab` | 기존 | 크로스헤어 비주얼 |
| `Scripts/UI/Popup/GachaGetCharacterPopup.cs` | 기존 재사용 | 캐릭터 순차 연출 |
| `Scripts/UI/Popup/GachaResultPopup.cs` | 기존 재사용 | 최종 결과 팝업 |
| `_Project/docs/GachaNew_Design.md` | 기획서 | 원본 기획 문서 |

---

## 12. 엣지 케이스 처리

| 케이스 | 처리 |
|--------|------|
| 빠른 드래그로 마크 건너뜀 | OnDrag 콜백 빈도(60fps)로 충분 |
| 마크 겹침 배치 | 최소 거리 120px, 30회 재시도 후 수용 |
| 동시 다수 마크 발견 | CheckOverlap 루프에서 모두 활성화 |
| 중복 활성화 | `IsFound` 플래그로 방어 |
| 화면 밖 터치 | `ScreenPointToLocalPointInRectangle` false → 무시 |
| Timeline 중 Skip | `_playableDirector.time = duration` → 즉시 캐릭터 시퀀스 |
| 씬 파괴 중 async | `destroyCancellationToken` 연동 |
| 1회 모집 완료 판정 | `_totalMarkCount = datas.Count` 동적 설정 |
| 다시모집 시 씬 정리 | ProcessCharacterGacha 시작부에서 기존 씬 언로드 |
| Addressable 해제 실패 | 4중 유효성 체크 (IsValid, IsDone, Succeeded, isLoaded) |
