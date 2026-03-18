# Gacha New - 크로스헤어 마크 탐색 미니게임 기획서

## Context

기존 가챠 연출(GachaFxByTen/One)을 대체하는 새로운 인터랙티브 가챠 연출 시스템.
유저가 우주 맵에서 크로스헤어를 드래그하며 숨겨진 10개의 마크를 찾아내는 미니게임 형태.
서버 가챠 결과(R/SR/SSR)에 따라 마크 등급이 결정되며, 발견 후 GachaGetCharacterPopup → GachaResultPopup 순서로 연출.

---

## 1. 전체 흐름

```
GachaBaseLayer.ProcessCharacterGacha()
  → 서버 API (NetManager.Instance.Gacha.DrawAsync)
  → 결과 수신 (List<RewardItem> 10개)
  → Addressables.InstantiateAsync("Gacha_New")
  → GachaNewController.SetItem(resultGachaList)

[초기화 Phase]
  ① 결과별 등급 판별 (R/SR/SSR)
  ② 최고 등급 & SSR 포함 여부 산출
  ③ Timeline 분기 설정 (Star 트랙 + Legend/Normal 트랙)
  ④ PlayableDirector.Play() → 도입 연출 재생
  ⑤ 해당 Mark 프리팹 인스턴스 생성 + Map 영역 내 랜덤 배치
  ⑥ 전체 비활성 상태, 카운터 UI "0/10" 표시

[도입 연출 Phase]
  → GachaTimeline 재생 (카메라, BG, Map, Star, Legend/Normal 등)
  → Timeline 완료 or 특정 시점 → 크로스헤어 입력 활성화

[탐색 Phase]
  → 유저 터치 → Crosshair 생성 (scale 0→1 애니메이션)
  → 드래그 → Crosshair 이동 + 범위 판정
  → Mark 범위 진입 → Mark 활성화 (파티클 재생 + scale pop)
  → 카운터 갱신 "N/10"
  → 10개 완료 시 → 완료 Phase 진입

[완료 Phase]
  → Crosshair 비활성
  → GachaGetCharacterPopup × 10회 순차 표시 (Pop → 다음 Push)
  → 전체 완료 → GachaResultPopup 표시
  → 닫기 → GachaPopup 복원 + 씬 파괴
```

---

## 2. Timeline / PlayableDirector 연동

### 2.1 GachaTimeline 트랙 구조

```
GachaTimeline (Timeline)
├── Left (Animator)          ─ 항상 재생
├── Right (Animator)         ─ 항상 재생
├── Main Camera (Animator)   ─ 항상 재생
├── Control Track (3)        ─ 항상 재생
├── Map (Animator)           ─ 항상 재생
├── Black_BG (Animator)      ─ 항상 재생
├── Galaxy (Animator)        ─ 항상 재생
├── Black_BG (Animator)      ─ 항상 재생
├── HUD_FX (Animator)        ─ 항상 재생
│
├── Star                     ─ 그룹
│   ├── Start                ─ 항상 재생 (공통 별 도입)
│   ├── SSR                  ─ 최고등급 SSR일 때만 활성
│   ├── SR                   ─ 최고등급 SR일 때만 활성
│   └── R                    ─ 최고등급 R일 때만 활성
│
├── Legend                   ─ SSR 1개 이상 포함 시 활성
│   └── Control Track (1)
│
└── Normal                   ─ SSR 0개 (미포함) 시 활성
    └── Control Track (2)
```

### 2.2 분기 로직

**Star 트랙 (최고 등급 기준)**:
- 10개 결과 중 **최고 등급**을 산출
- 해당 등급 트랙만 Mute 해제, 나머지 Mute 처리

```csharp
// 최고 등급 산출
GradeType highestGrade = GradeType.RARE;
bool hasSSR = false;

for (int i = 0; i < datas.Count; i++)
{
    var grade = GetGradeFromRewardItem(datas[i]);
    if (grade == GradeType.LEGENDARY) hasSSR = true;
    if (grade > highestGrade) highestGrade = grade;
}

// Star 트랙 Mute 설정
SetTimelineTrackMute("SSR", highestGrade != GradeType.LEGENDARY);
SetTimelineTrackMute("SR",  highestGrade != GradeType.EPIC);
SetTimelineTrackMute("R",   highestGrade != GradeType.RARE);
```

**Legend/Normal 트랙 (SSR 포함 여부)**:
- SSR(LEGENDARY)이 **1개 이상** → Legend 활성, Normal Mute
- SSR **0개** → Normal 활성, Legend Mute

```csharp
// Legend/Normal 분기
SetTimelineTrackMute("Legend", !hasSSR);   // SSR 있으면 Legend 재생
SetTimelineTrackMute("Normal", hasSSR);    // SSR 없으면 Normal 재생
```

### 2.3 Timeline Mute 제어 방식

PlayableDirector의 TimelineAsset에서 트랙을 이름으로 찾아 muted 설정:

```csharp
private void SetTimelineTrackMute(string trackName, bool muted)
{
    var timelineAsset = _playableDirector.playableAsset as TimelineAsset;
    if (timelineAsset == null) return;

    foreach (var track in timelineAsset.GetOutputTracks())
    {
        if (track.name == trackName)
        {
            track.muted = muted;
            break;
        }
    }
    // GroupTrack 내부 서브트랙도 검색
    foreach (var track in timelineAsset.GetRootTracks())
    {
        if (track is GroupTrack groupTrack && track.name == trackName)
        {
            track.muted = muted;
            break;
        }
        // GroupTrack 자식 검색
        foreach (var child in track.GetChildTracks())
        {
            if (child.name == trackName)
            {
                child.muted = muted;
                break;
            }
        }
    }
}
```

### 2.4 Timeline 재생 → 탐색 Phase 전환

```csharp
// Timeline 재생 시작
_playableDirector.Play();

// 특정 시간 후 크로스헤어 입력 활성화
// (Timeline 전체 길이 또는 탐색 시작 시점)
float searchStartTime = (float)_playableDirector.duration;  // 또는 지정 시간
Run.After(searchStartTime, () =>
{
    _crosshair.SetEnabled(true);
    _skipButton.SetActive(true);
});
```

---

## 3. 신규 스크립트 3개

### 3.1 GachaNewController.cs

**위치**: `Assets/_Project/Addressables/Remote/Gacha_New/Scenes/GachaNewController.cs`
**네임스페이스**: `CookApps.AutoBattler`
**부착 대상**: Gacha_New 씬 루트 또는 전용 매니저 오브젝트

| 역할 | 설명 |
|------|------|
| Timeline 제어 | PlayableDirector 재생 + Star/Legend/Normal 트랙 분기 |
| 마크 생성/배치 | 결과 등급에 따라 Mark_R/SR/SSR 프리팹 인스턴스, 랜덤 위치 |
| 진행 추적 | 발견 카운트 관리, UI 갱신 |
| 완료 처리 | 10개 완료 시 GachaGetCharacterPopup 순차 호출 → GachaResultPopup |
| Skip/Back | 스킵 시 전체 마크 활성화 후 결과 흐름 진행 |

**주요 SerializedField**:
```csharp
[Header("Timeline")]
[SerializeField] private PlayableDirector _playableDirector;

[Header("Star VFX Objects")]
[SerializeField] private GameObject _starSSRObject;    // Star > SSR
[SerializeField] private GameObject _starSRObject;     // Star > SR
[SerializeField] private GameObject _starRObject;      // Star > R

[Header("Legend/Normal VFX Objects")]
[SerializeField] private GameObject _legendObject;     // Legend 그룹
[SerializeField] private GameObject _normalObject;     // Normal 그룹

[Header("Mark Prefabs")]
[SerializeField] private GameObject _markPrefabR;      // Mark_R.prefab
[SerializeField] private GameObject _markPrefabSR;     // Mark_SR.prefab
[SerializeField] private GameObject _markPrefabSSR;    // Mark_SSR.prefab
[SerializeField] private RectTransform _markContainer; // 마크 배치 영역

[Header("Crosshair")]
[SerializeField] private CrosshairController _crosshair;

[Header("UI")]
[SerializeField] private TextMeshProUGUI _countText;   // "3/10" 카운터
[SerializeField] private GameObject _skipButton;
[SerializeField] private GameObject _closeButton;
```

**핵심 API**:
```csharp
// GachaBaseLayer에서 호출 — 미니게임 시작
public void SetItem(List<RewardItem> datas, GachaInfo specGachaData, Action onContinueGacha)

// Timeline 분기 설정
private void ConfigureTimeline(bool hasSSR, GradeType highestGrade)

// MarkController에서 호출 — 마크 발견 시
public void OnMarkFound(MarkController mark)

// 결과 데이터에서 등급 판별
private GradeType GetGradeFromRewardItem(RewardItem item)

// 미발견 마크 목록 (CrosshairController에서 사용)
public List<MarkController> GetUnfoundMarks()

// Skip 버튼 — Timeline 스킵 + 전체 마크 즉시 활성화
public void OnClickSkip()

// Back 버튼 — GachaPopup 복원 + 씬 파괴
public void OnClickBack()
```

**SetItem 흐름**:
```csharp
public void SetItem(List<RewardItem> datas, GachaInfo specGachaData, Action onContinueGacha)
{
    _resultData = datas;
    _specGachaData = specGachaData;
    _onContinueGacha = onContinueGacha;

    // 1) 등급 분석
    GradeType highestGrade = GradeType.RARE;
    bool hasSSR = false;
    for (int i = 0; i < datas.Count; i++)
    {
        var grade = GetGradeFromRewardItem(datas[i]);
        if (grade == GradeType.LEGENDARY) hasSSR = true;
        if (grade > highestGrade) highestGrade = grade;
    }

    // 2) Timeline 분기 + 재생
    ConfigureTimeline(hasSSR, highestGrade);
    _playableDirector.Play();

    // 3) 마크 생성 + 랜덤 배치
    SpawnMarks(datas);

    // 4) UI 초기화
    _foundCount = 0;
    UpdateCountUI();
    _crosshair.SetEnabled(false);  // Timeline 끝날 때까지 비활성
    _skipButton.SetActive(true);
    _closeButton.SetActive(false);

    // 5) Timeline 완료 후 탐색 시작
    float delay = (float)_playableDirector.duration;
    Run.After(delay, () =>
    {
        _crosshair.SetEnabled(true);
    });
}
```

**ConfigureTimeline**:
```csharp
private void ConfigureTimeline(bool hasSSR, GradeType highestGrade)
{
    // Star 분기: 최고 등급에 해당하는 것만 활성
    _starSSRObject?.SetActive(highestGrade == GradeType.LEGENDARY);
    _starSRObject?.SetActive(highestGrade == GradeType.EPIC);
    _starRObject?.SetActive(highestGrade == GradeType.RARE);

    // Legend/Normal 분기: SSR 포함 여부
    _legendObject?.SetActive(hasSSR);
    _normalObject?.SetActive(!hasSSR);

    // Timeline 트랙 Mute 설정 (GameObject 활성화와 병행)
    SetTimelineTrackMute("SSR", highestGrade != GradeType.LEGENDARY);
    SetTimelineTrackMute("SR",  highestGrade != GradeType.EPIC);
    SetTimelineTrackMute("R",   highestGrade != GradeType.RARE);
    SetTimelineTrackMute("Legend", !hasSSR);
    SetTimelineTrackMute("Normal", hasSSR);
}
```

**랜덤 배치 로직**:
- `_markContainer.rect`에서 가용 영역 계산
- 가장자리 패딩 100px, 하단 Footer 영역 추가 패딩
- 마크 간 최소 거리 120px (겹침 방지, 최대 30회 재시도)
- 좌표 = `anchoredPosition` (UI 로컬 좌표)

**등급 판별**:
```csharp
private GradeType GetGradeFromRewardItem(RewardItem item)
{
    if (!item.Id.GetCharacterId(out int charId)) return GradeType.RARE;
    var charInfo = SpecDataManager.Instance.GetCharacterData(charId);
    return charInfo?.grade_type ?? GradeType.RARE;
}
```

**완료 후 캐릭터 순차 표시** (GachaGetCharacterPopup 활용):
```csharp
private async UniTask ShowCharacterSequence()
{
    for (int i = 0; i < _resultData.Count; i++)
    {
        if (!_resultData[i].Id.GetCharacterId(out int charId)) continue;

        var param = new GachaGetCharacterPopupParam { CharacterId = charId };
        await SceneUILayerManager.Instance.PushUILayerAsync<GachaGetCharacterPopup>(param);

        // 유저가 팝업 닫을 때까지 대기
        await UniTask.WaitUntil(() =>
            SceneUILayerManager.Instance.GetUILayer<GachaGetCharacterPopup>() == null);
    }

    // 전체 결과 팝업
    var resultParam = new GachaResultPopupParam
    {
        ResultItems = _resultData,
        SpecGachaData = _specGachaData,
        OnContinueGacha = () => _onContinueGacha?.Invoke()
    };
    SceneUILayerManager.Instance.PushUILayerAsync<GachaResultPopup>(resultParam).Forget();
}
```

---

### 3.2 MarkController.cs

**위치**: `Assets/_Project/Addressables/Remote/Gacha_New/Scenes/MarkController.cs`
**네임스페이스**: `CookApps.AutoBattler`
**부착 대상**: Mark_R / Mark_SR / Mark_SSR 프리팹

| 역할 | 설명 |
|------|------|
| 상태 관리 | found/not-found 플래그 |
| 활성화 연출 | SetActive(true) + scale pop + 파티클 재생 |
| 위치/범위 제공 | anchoredPosition, detectionRadius |

**주요 필드**:
```csharp
[SerializeField] private float _detectionRadius = 70f;  // 판정 반경 (UI 단위)
[SerializeField] private ParticleSystem _particleSystem;

public RectTransform RectTransform { get; private set; }
public bool IsFound { get; private set; }
public float DetectionRadius => _detectionRadius;
public Vector2 Position => RectTransform.anchoredPosition;
public int ResultIndex { get; private set; }  // 대응하는 가챠 결과 인덱스
```

**핵심 API**:
```csharp
public void Initialize(GachaNewController controller, int resultIndex)
public void Activate()  // found=true, 활성화 애니메이션, controller.OnMarkFound 호출
```

**활성화 연출**:
- `gameObject.SetActive(true)`
- Scale: `Vector3.zero → Vector3.one` (0.3초, EaseOutBack 커브)
- 파티클 Play
- 사운드 재생

---

### 3.3 CrosshairController.cs

**위치**: `Assets/_Project/Addressables/Remote/Gacha_New/Scenes/CrosshairController.cs`
**네임스페이스**: `CookApps.AutoBattler`
**부착 대상**: 전체 화면 터치 입력 영역 (Image, alpha=0, raycastTarget=true)

| 역할 | 설명 |
|------|------|
| 입력 수신 | IPointerDownHandler, IDragHandler, IPointerUpHandler |
| 크로스헤어 이동 | 터치 좌표 → UI 로컬 좌표 변환 |
| 범위 판정 | 드래그 중 미발견 마크와 거리 비교 |
| 시각 피드백 | 나타남/사라짐 scale 애니메이션 |

**입력 구조**:
```
UICanvas (ScreenSpace-Camera)
└── Panel (SafeArea)
    ├── MarkContainer (full-stretch, 마크 배치)
    ├── TouchInputArea (full-stretch, Image alpha=0, raycastTarget=true)
    │   └── Crosshair (프리팹 인스턴스, 비주얼)  ← CrosshairController 부착
    ├── Content (카운터 UI)
    └── Footer
```

> **TouchInputArea가 MarkContainer 위(sibling order)**에 있어야 터치를 먼저 받음

**핵심 로직**:
```csharp
public void OnPointerDown(PointerEventData eventData)
{
    if (!_isEnabled) return;
    _isDragging = true;
    MoveToPosition(eventData.position);
    ShowCrosshair();  // scale 0→1
    CheckOverlap();
}

public void OnDrag(PointerEventData eventData)
{
    if (!_isDragging || !_isEnabled) return;
    MoveToPosition(eventData.position);
    CheckOverlap();
}

public void OnPointerUp(PointerEventData eventData)
{
    _isDragging = false;
    HideCrosshair();  // scale 1→0
}

private void MoveToPosition(Vector2 screenPos)
{
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _parentRect, screenPos, _canvas.worldCamera, out Vector2 localPoint);
    _crosshairVisual.anchoredPosition = localPoint;
}

private void CheckOverlap()
{
    var marks = _controller.GetUnfoundMarks();
    for (int i = 0; i < marks.Count; i++)
    {
        float dist = Vector2.Distance(_crosshairVisual.anchoredPosition, marks[i].Position);
        if (dist <= marks[i].DetectionRadius)
        {
            marks[i].Activate();
        }
    }
}
```

**범위 판정 방식**: RectTransform `anchoredPosition` 간 `Vector2.Distance`
- 마크 크기 100x100 → 반경 50
- detectionRadius 70 = 중심에서 70px 이내 접촉 시 활성화 (모바일 터치 관용도 고려)
- OnDrag 콜백에서만 실행 (매 프레임 X) → 10개 거리 계산은 무시할 수준

---

## 4. 기존 파일 수정

### 4.1 GachaBaseLayer.cs
**경로**: `Assets/_Project/Scripts/UI/Popup/Layers/GachaBaseLayer.cs`

`ProcessCharacterGacha()` 메서드에서 기존 `Gacha_VFX_Ver_Final_01` 대신 `Gacha_New` 인스턴스:

```csharp
// 기존 (111번 줄)
var handle = Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01");
await handle.WaitUntilDone();
_loadedGachaFxHandles.Add(handle);

// 변경
var handle = Addressables.InstantiateAsync("Gacha_New");
await handle.WaitUntilDone();
_loadedGachaFxHandles.Add(handle);

var controller = handle.Result.GetComponentInChildren<GachaNewController>();
controller.SetItem(resultGachaList, _currentSpecGachaData,
    () => ProcessCharacterGacha(gachaCountType).Forget());
```

또한 기존 `GachaResultPopup` 직접 호출 부분(124~130줄)을 제거하고 `GachaNewController`가 완료 후 처리하도록 위임.

---

## 5. 씬 셋업 (Unity Editor 작업)

**Gacha_New.unity 하이어라키 변경**:

```
Gacha_New*
├── Gacha (기존 유지)
│   ├── Timeline (PlayableDirector — GachaTimeline)
│   ├── Main Camera, Mask, Black_BG, HUD_FX, Galaxy, BG, Map
│   └── ... (기존 유지)
├── FX (기존 유지)
│   ├── Legend     ← GachaNewController._legendObject 연결
│   ├── Normal     ← GachaNewController._normalObject 연결
│   └── Star
│       ├── Start  ← 항상 활성
│       ├── SSR    ← GachaNewController._starSSRObject 연결
│       ├── SR     ← GachaNewController._starSRObject 연결
│       └── R      ← GachaNewController._starRObject 연결
├── UICanvas
│   └── Panel
│       ├── FX (기존 — Mark_SR, Mark_SSR, Mark_R 인스턴스 제거)
│       ├── MarkContainer ← [신규] full-stretch RectTransform
│       ├── TouchInputArea ← [신규] full-stretch Image(alpha=0, raycastTarget=true)
│       │   └── Crosshair ← [기존 프리팹 이동]
│       ├── Content (기존 유지 — 카운터 UI)
│       └── Footer (기존 유지)
└── GachaNewManager ← [신규] GachaNewController 부착
```

**MCP 작업 순서**:
1. Panel 하위에 `MarkContainer` RectTransform 생성 (full-stretch)
2. Panel 하위에 `TouchInputArea` Image 생성 (full-stretch, alpha=0)
3. 기존 FX 하위의 Mark 인스턴스 제거 (프리팹 참조로 대체)
4. Crosshair 인스턴스를 TouchInputArea 하위로 이동
5. `GachaNewManager` 오브젝트 생성 + GachaNewController 부착
6. SerializedField 연결:
   - `_playableDirector` → Timeline 오브젝트
   - `_starSSRObject` / `_starSRObject` / `_starRObject` → Star 하위 오브젝트
   - `_legendObject` / `_normalObject` → FX 하위 오브젝트
   - `_markPrefabR/SR/SSR` → Prefab 폴더의 프리팹
   - `_markContainer` → MarkContainer
   - `_crosshair` → CrosshairController
7. CrosshairController → TouchInputArea에 부착
8. MarkController → Mark_R/SR/SSR 프리팹에 각각 부착

---

## 6. 엣지 케이스 처리

| 케이스 | 처리 |
|--------|------|
| 빠른 드래그로 마크를 건너뜀 | OnDrag 콜백 빈도로 충분 (60fps). 필요시 이전↔현재 위치 사이 보간 체크 추가 |
| 마크 겹침 배치 | 최소 거리 120px 보장, 30회 재시도 후 수용 |
| 동시 다수 마크 발견 | CheckOverlap 루프에서 모두 활성화 (정상 동작) |
| 중복 활성화 | `IsFound` 플래그로 방어 |
| 화면 밖 터치 | `ScreenPointToLocalPointInRectangle` false 반환 시 무시 |
| Timeline 중 Skip | `_playableDirector.time = _playableDirector.duration` 후 즉시 탐색 Phase 진입 |
| 탐색 중 Skip | 미발견 마크 전체 활성화 → 캐릭터 순차 표시 → 결과 팝업 |
| 씬 파괴 중 코루틴 | `destroyCancellationToken` 연동 |
| Addressable 정리 | `GachaBaseLayer._loadedGachaFxHandles`에서 Release 자동 처리 |
| SSR 0개 결과 | Normal 트랙 재생, Star는 최고등급(SR/R) 트랙만 활성 |

---

## 7. 참조 파일 목록

| 파일 | 용도 |
|------|------|
| `Assets/_Project/Scripts/UI/Popup/Layers/GachaBaseLayer.cs` | 진입점 수정 |
| `Assets/_Project/Scripts/UI/Popup/GachaGetCharacterPopup.cs` | 캐릭터 순차 연출 (재사용) |
| `Assets/_Project/Scripts/UI/Popup/GachaResultPopup.cs` | 최종 결과 팝업 (재사용) |
| `Assets/_Project/Addressables/Remote/Gacha_STK/Script/GachaFxByTen.cs` | 패턴 참조 (SetItem, Skip, Back, SSR 분기) |
| `Assets/_Project/Addressables/Remote/Gacha_New/Scenes/ParticleGroup.cs` | 동일 폴더 코딩 스타일 참조 |
| `Assets/_Project/Addressables/Remote/Gacha_New/Timeline/GachaTimeline.playable` | Timeline 에셋 |
| `Assets/_Project/Addressables/Remote/Gacha_New/Prefab/Mark_R.prefab` | 마크 프리팹 |
| `Assets/_Project/Addressables/Remote/Gacha_New/Prefab/Mark_SR.prefab` | 마크 프리팹 |
| `Assets/_Project/Addressables/Remote/Gacha_New/Prefab/Mark_SSR.prefab` | 마크 프리팹 |
| `Assets/_Project/Addressables/Remote/Gacha_New/Prefab/Crosshair.prefab` | 크로스헤어 프리팹 |
| `Assets/_Project/Addressables/Remote/Gacha_New/Scenes/Gacha_New.unity` | 씬 설정 |

---

## 8. 검증 방법

### 8.1 에디터 테스트
1. `GachaBaseLayer`에 `GACHA_MOCK_MODE` 활성화
2. GachaPopup → 10연 뽑기 버튼 클릭
3. Gacha_New 씬 로드 + **Timeline 도입 연출 재생** 확인
4. **SSR 포함 시**: Star > SSR 활성 + Legend 활성 확인
5. **SSR 미포함 시**: Star > SR 또는 R 활성 + Normal 활성 확인
6. Timeline 완료 후 크로스헤어 입력 활성화 확인
7. 10개 마크가 Map 영역에 랜덤 배치되고 전부 비활성 상태인지 확인
8. 터치/드래그 시 Crosshair 나타나고 따라오는지 확인
9. 마크 근처 드래그 시 활성화 + 파티클 + 카운터 갱신 확인
10. 10/10 완료 시 GachaGetCharacterPopup 10회 순차 표시 확인
11. 전체 완료 후 GachaResultPopup 표시 확인
12. Skip 버튼으로 즉시 완료 흐름 확인 (Timeline 스킵 포함)
13. Back 버튼으로 GachaPopup 복원 확인

### 8.2 MCP 검증
- `list_game_objects_in_hierarchy`로 씬 하이어라키 확인
- `get_game_object_info`로 컴포넌트 부착 상태 확인
- `check_compile_errors`로 컴파일 오류 확인
- `play_game` / `stop_game`으로 런타임 테스트

---

## 9. 구현 순서

1. **MarkController.cs** 작성 (가장 단순, 의존성 없음)
2. **CrosshairController.cs** 작성 (MarkController 참조)
3. **GachaNewController.cs** 작성 (Timeline 제어 + 마크 + 크로스헤어 조율)
4. **Mark_R/SR/SSR 프리팹**에 MarkController 부착 (MCP)
5. **Gacha_New.unity 씬** 하이어라키 재구성 (MCP)
6. **SerializedField 연결** — Timeline, Star/Legend/Normal 오브젝트, 프리팹 등 (MCP)
7. **GachaBaseLayer.cs** 수정 (진입점 연결)
8. 컴파일 확인 + 에디터 테스트
