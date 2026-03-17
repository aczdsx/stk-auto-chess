# Gacha_New.unity 씬 분석 문서

**파일 경로**: `Assets/_Project/Addressables/Remote/Gacha_New/Scenes/Gacha_New.unity`
**총 라인 수**: 570,809줄
**용도**: 가차(소환) 연출 전용 씬

---

## 1. 전체 통계

| 항목 | 수량 |
|------|------|
| GameObjects | 174 |
| Transforms | 131 |
| RectTransforms | 46 |
| MonoBehaviours | 33 |
| ParticleSystems | 116 |
| ParticleSystemRenderers | 116 |
| CanvasRenderers | 24 |
| Animators | 13 |
| Cameras | 1 |
| Canvas | 1 |
| PlayableDirectors | 1 |
| MeshRenderers | 1 |
| MeshFilters | 1 |
| SpriteRenderers | 3 |
| PrefabInstances | 3 |

**레이어 분포**:
- Layer 5 (UI): 96개
- Layer 6 (커스텀): 59개
- Layer 0 (Default): 19개

---

## 2. Root GameObjects

씬에는 **2개의 루트 오브젝트**가 존재:

| 이름 | Layer | 설명 |
|------|-------|------|
| **Gacha** | 0 | 전체 가차 연출 루트 (Transform만 가진 빈 오브젝트) |
| **EventSystem** | 0 | Unity EventSystem + StandaloneInputModule |

---

## 3. Hierarchy 구조

```
[ROOT] Gacha
  └── Timeline  (PlayableDirector)
      ├── Main Camera  (Camera, AudioListener, Animator, URP)
      │   ├── Mask  (Animator)
      │   ├── Black_BG  (Animator)
      │   └── HUD_FX  (Animator, scale 0.4x)
      │       ├── Center_HUD_Out  (8 children)
      │       ├── HUD_ring_02_1 (1)  (1 child)
      │       ├── Center_HUD_In  (8 children)
      │       ├── Side_HUD  (8 children)
      │       ├── Galaxy  (8 children, Animator)
      │       └── Light  (4 children)
      ├── BG
      │   ├── Earth
      │   ├── Sphere  (MeshRenderer + MeshFilter)
      │   └── Map  (Animator)
      ├── FX  (ParticleAlphaController 스크립트)
      │   ├── Legend  (5 children, scale 0.1x)
      │   ├── Normal  (3 children, ParticleSystem, scale 0.1x)
      │   └── pt_shine_01
      ├── Star
      │   ├── Start  (1 child)
      │   └── 01  (4 children)
      │       ├── CometFlight_UR
      │       ├── CometFlight_SSR
      │       ├── CometFlight_SR
      │       └── CometFlight_N
      └── UICanvas  (Canvas + CanvasScaler + GraphicRaycaster)
          └── Panel  (SafeArea)
              ├── FX (UI)
              │   ├── [Prefab] Mark_R
              │   ├── [Prefab] Mark_SR
              │   └── [Prefab] Mark_SSR
              ├── Content
              │   ├── C_Center
              │   └── C_Right
              └── Footer
                  └── CountGroup
                      └── ListGroup  (HorizontalLayoutGroup, 비활성)

[ROOT] EventSystem
  (자식 없음)
```

---

## 4. Camera 설정

**Main Camera**
| 속성 | 값 |
|------|-----|
| Tag | MainCamera |
| Projection | Perspective |
| FOV | 60° |
| Near/Far Clip | 0.01 / 1000 |
| Clear Flags | Skybox |
| Background Color | RGBA(0.192, 0.302, 0.475, 0.02) |
| Culling Mask | Everything |
| Depth | -1 |
| Position | (0, 0, -2) |
| Rotation | X ≈ -35° |
| HDR | Off |
| URP PostProcessing | Off |

---

## 5. Canvas 설정

**UICanvas** — 씬 내 유일한 Canvas

| 속성 | 값 |
|------|-----|
| Render Mode | Screen Space - Overlay |
| Sorting Order | 0 |
| Pixel Perfect | Off |
| UI Scale Mode | Scale With Screen Size |
| Reference Resolution | 1920 x 1080 |
| Match Width/Height | 0.8 (높이 우선) |

---

## 6. MonoBehaviour 스크립트

| 사용 횟수 | 스크립트 | 비고 |
|-----------|---------|------|
| 23 | `Image` (UnityEngine.UI) | UI 이미지 |
| 1 | `TextMeshProUGUI` | TMP 텍스트 |
| 1 | `CAButton` | CookApps 커스텀 버튼 |
| 1 | `GraphicRaycaster` | Canvas 레이캐스트 |
| 1 | `UniversalAdditionalCameraData` | URP 카메라 데이터 |
| 1 | `ParticleAlphaController` | 파티클 그룹 알파 제어 |
| 1 | `EventSystem` | 이벤트 시스템 |
| 1 | `StandaloneInputModule` | 입력 모듈 |
| 1 | `HorizontalLayoutGroup` | 수평 레이아웃 |
| 1 | `CanvasScaler` | 캔버스 스케일러 |
| 1 | `SafeArea` | SafeArea 처리 |

**커스텀 스크립트 경로**:
- `Assets/_Project/Scripts_Libs/CookApps/Selectables/CAButton.cs`
- `Assets/_Project/Scripts_Libs/CookApps/Utility/SafeArea/SafeArea.cs`
- `Assets/_Project/Addressables/Remote/Gacha_New/Scenes/ParticleGroup.cs` (ParticleAlphaController)

---

## 7. Timeline / PlayableDirector

| 속성 | 값 |
|------|-----|
| PlayableAsset | `GachaTimeline.playable` |
| 경로 | `Gacha_New/Timeline/GachaTimeline.playable` |
| Initial State | Playing |
| Wrap Mode | Hold |
| Update Mode | GameTime |

**Scene Bindings** (12개 트랙 바인딩):

| Animator | 용도 |
|----------|------|
| HUD_FX | HUD 이펙트 전체 애니메이션 |
| Galaxy | 은하 배경 이펙트 |
| Main Camera | 카메라 무빙/쉐이크 연출 |
| Map | 배경 맵 애니메이션 |
| Black_BG | 암전 페이드 |
| CometFlight_UR | UR 등급 혜성 연출 |
| CometFlight_N | N 등급 혜성 연출 |
| CometFlight_SSR | SSR 등급 혜성 연출 |
| CometFlight_SR | SR 등급 혜성 연출 |
| Left | 좌측 UI/효과 슬라이드 |
| Right | 우측 UI/효과 슬라이드 |
| Mask | 마스킹 연출 |

> 모든 Animator의 Controller가 None — Timeline PlayableDirector가 런타임에 직접 제어

---

## 8. PrefabInstances (3개)

모두 `UICanvas > Panel > FX` 하위에 배치:

| 프리팹 | 경로 | 용도 |
|--------|------|------|
| Mark_R | `Gacha_New/Prefab/Mark_R.prefab` | R등급 마크 UI |
| Mark_SR | `Gacha_New/Prefab/Mark_SR.prefab` | SR등급 마크 UI |
| Mark_SSR | `Gacha_New/Prefab/Mark_SSR.prefab` | SSR등급 마크 UI |

---

## 9. Particle Systems (116개)

| 이름 | 인스턴스 수 | 설명 |
|------|-----------|------|
| CometFlight_core_01 | 12 | 혜성 코어 파티클 |
| CometFlight_core_02 | 6 | 혜성 코어 파티클 변형 |
| Comet_dust_01/02 | 6/5 | 혜성 먼지 |
| Comet_shine_01/02/03 | 5/2/1 | 혜성 빛남 |
| Galaxy_core_01 | 3 | 은하 코어 |
| Galaxy_star_01/02/03 | 1/2/2 | 은하 별 |
| Galaxy_twirl_01/02 | 1/1 | 은하 소용돌이 |
| Galaxy_aura_01 | 1 | 은하 오라 |
| Galaxy_black_01 | 1 | 은하 블랙홀 |
| CastLight_core/light_01 | 1/2 | 캐스트 라이트 |
| HUD 관련 | ~30+ | HUD 링, 점, 숫자, 박스 등 |

---

## 10. 등급별 연출 분기 구조

가차 결과 등급에 따라 3가지 레이어에서 분기 처리:

### 3D 파티클 이펙트 (FX)
- `FX/Legend` — 레전드 등급 전용 (5 children, scale 0.1x)
- `FX/Normal` — 일반 등급 전용 (3 children, ParticleSystem)

### 혜성 비행 연출 (Star/01)
- `CometFlight_UR` — UR 등급 (Animator)
- `CometFlight_SSR` — SSR 등급 (Animator)
- `CometFlight_SR` — SR 등급 (Animator)
- `CometFlight_N` — N 등급 (Animator x2)

### UI 등급 마크 (UICanvas/Panel/FX)
- `Mark_R.prefab` — R 등급 마크
- `Mark_SR.prefab` — SR 등급 마크
- `Mark_SSR.prefab` — SSR 등급 마크

---

## 11. RenderSettings

| 속성 | 값 |
|------|-----|
| Fog | Off |
| Ambient Mode | Custom (3) |
| Ambient Color | RGB(0.2, 0.2, 0.2) |
| Skybox Material | `Dark Nebula Skybox MAT.mat` |

**스카이박스 큐브맵 텍스처** (6면):
- `Gacha_New/Textures/Dark Nebula {Back/Bottom/Front/Left/Right/Top} TEX.png`

---

## 12. 관련 에셋 디렉토리 구조

```
Gacha_New/
├── Materials/
│   └── Dark Nebula Skybox MAT.mat        # 스카이박스 머테리얼
├── Prefab/
│   ├── Crosshair.prefab                  # 크로스헤어
│   ├── Mark_R.prefab                     # R등급 마크 UI
│   ├── Mark_SR.prefab                    # SR등급 마크 UI
│   └── Mark_SSR.prefab                   # SSR등급 마크 UI
├── Scenes/
│   ├── Gacha_New.unity                   # 메인 씬 (본 문서 대상)
│   ├── ParticleGroup.cs                  # ParticleAlphaController 스크립트
│   ├── MosaicShader.shader               # 모자이크 전환 셰이더
│   ├── Custom_URP_MosaicTransition.mat   # 모자이크 전환 머테리얼
│   ├── CrossHair*.png (4개)              # 크로스헤어 텍스처
│   ├── Grad.png                          # 그라데이션 텍스처
│   ├── Map_upscayl*.png                  # 배경 맵 텍스처 (업스케일)
│   └── Searching.png                     # 검색 텍스처
├── Textures/
│   └── Dark Nebula {6면} TEX.png         # 큐브맵 텍스처 6장
└── Timeline/
    └── GachaTimeline.playable            # 타임라인 에셋
```

---

## 13. 아키텍처 요약

이 씬은 **가차(소환) 연출 전용 씬**으로 다음 특징을 가짐:

1. **Timeline 기반 연출**: 하나의 PlayableDirector가 12개 Animator 트랙을 제어하여 가차 연출 시퀀스 구동
2. **등급별 분기**: 3D 파티클(FX), 혜성 비행(Star), UI 마크(Mark_*) 3가지 레이어에서 등급별 분기 처리
3. **파티클 집약적**: 116개의 ParticleSystem으로 우주/은하/혜성 테마 연출 구현
4. **UI 구조**: Screen Space Overlay Canvas (1920x1080, 0.8 높이 매칭) + SafeArea 적용
5. **3D 배경**: Dark Nebula 스카이박스 + Earth/Sphere/Map 오브젝트
6. **ParticleAlphaController**: FX 오브젝트에 부착, 하위 전체 파티클의 알파를 일괄 제어 (Timeline 연동)
7. **모든 Animator Controller = None**: Timeline이 런타임에 직접 제어하는 구조
