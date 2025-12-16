# Addressables 폴더 구조 규칙 문서

## 개요

`Assets/_Project/Addressables` 폴더는 Unity Addressables 시스템을 위한 에셋 관리 구조입니다. 이 문서는 폴더 정리 규칙과 구조를 정의합니다.

## 최상위 구조

```
Assets/_Project/Addressables/
├── BuiltIn/          # 앱 빌드에 포함되는 기본 에셋
└── Remote/           # 원격 다운로드 가능한 에셋 (번들화)
```

### 1. BuiltIn (빌트인 에셋)

앱 초기 빌드에 반드시 포함되어야 하는 필수 에셋들을 관리합니다.

#### 구조

```
BuiltIn/
├── Data/                    # 게임 데이터 에셋
│   ├── UIElementData/       # UI 요소 데이터
│   ├── AtlasManager.asset
│   ├── ColorData.asset
│   └── VignetteData.asset
├── Fonts/                   # 폰트 파일
├── Materials/               # 머티리얼
│   ├── Character/           # 캐릭터용 머티리얼
│   └── UI/                  # UI용 머티리얼
├── Scenes/                  # 필수 씬
│   ├── StartUp.unity        # 시작 씬
│   └── Title.unity          # 타이틀 씬
├── Shaders/                 # 셰이더 파일
├── SpineRes/                # Spine 애니메이션 리소스
│   └── Title/               # 타이틀 화면용
├── Splash_STK/              # 스플래시 이펙트
│   ├── Prefab/
│   ├── Resource/
│   └── Script/
├── SpriteAtlas/             # 스프라이트 아틀라스
├── Texture_StandAlone/      # 독립 텍스처
└── Textures/                # 기타 텍스처
    ├── aso/                 # 앱스토어 최적화 이미지
    └── Common/              # 공통 텍스처
```

#### 포함 기준

- 앱 실행에 즉시 필요한 에셋
- 타이틀 화면 및 초기 로딩 관련 리소스
- 공통 UI 머티리얼 및 셰이더
- 기본 폰트
- 필수 씬 (StartUp, Title)

---

### 2. Remote (원격 에셋)

런타임에 다운로드 가능한 에셋들을 관리합니다. 번들 단위로 관리되며, 필요 시 다운로드됩니다.

#### 구조

```
Remote/
├── Animations/              # 애니메이션 클립 (135개)
├── Gacha_STK/              # 가챠 시스템 리소스
│   ├── Prefab/
│   │   ├── FX_Popup_Get_Character/  # 캐릭터 획득 팝업 이펙트
│   │   ├── Gacha/
│   │   ├── Gacha_EDIT/
│   │   └── UI/
│   ├── Resource/
│   │   ├── FX_Gacha_SR/            # SR 등급 가챠 이펙트
│   │   ├── FX_Gacha_SSR/           # SSR 등급 가챠 이펙트
│   │   ├── Gacha_VFX/              # 가챠 VFX
│   │   ├── Illust/                 # 일러스트 이미지
│   │   ├── Shader/                 # 가챠용 셰이더
│   │   ├── Sprites/                # 가챠 UI 스프라이트
│   │   └── Textures/               # 가챠 텍스처
│   └── Script/
├── Prefabs/                # 프리팹
│   ├── Fx/                         # 이펙트 프리팹
│   │   ├── Adria/                  # 캐릭터별 이펙트
│   │   ├── Alice/
│   │   ├── April/
│   │   ├── Aran/
│   │   ├── Common/                 # 공통 이펙트
│   │   ├── Monster/                # 몬스터 이펙트
│   │   ├── UI/                     # UI 이펙트
│   │   └── ...
│   ├── InGame/                     # 인게임 프리팹
│   ├── Stages/                     # 스테이지 프리팹
│   │   ├── Ingame/
│   │   └── Outgame/
│   ├── UI/                         # UI 프리팹
│   │   ├── 00_Main/                # 메인 UI
│   │   ├── 01_Pops/                # 팝업 UI
│   │   ├── 02_Elements/            # UI 요소
│   │   ├── InGame/
│   │   ├── Loading/
│   │   ├── Lobby/
│   │   ├── Title/
│   │   ├── Top/
│   │   └── UI_FX/
│   └── Util/                       # 유틸리티 프리팹
├── Scenes/                 # 원격 씬
├── SD/                     # Super Deformed (SD 캐릭터) 리소스
│   ├── Characters/                 # SD 캐릭터
│   ├── Item/                       # SD 아이템
│   ├── Mob/                        # SD 몬스터
│   └── Obstacle/                   # SD 장애물
├── SpriteAtlas/            # 스프라이트 아틀라스 (30개)
├── StageProps/             # 스테이지 소품 (40개)
├── Territory/              # 영역/테리토리 관련 (20개)
├── Texture_StandAlone/     # 독립 텍스처 (12개)
├── Textures/               # 일반 텍스처 (12개)
└── VFX/                    # 비주얼 이펙트 (51개)
```

---

## SD (Super Deformed) 리소스 구조

SD 폴더는 캐릭터, 몬스터, 장애물, 아이템의 2D 리소스를 관리합니다. 모든 엔티티는 ID 기반으로 관리됩니다.

### 공통 폴더 구조 규칙

```
SD/
├── Characters/{CharacterID}/     # 캐릭터 ID (예: 15252102)
├── Mob/{MobID}/                  # 몹 ID (예: 10106, 30101001)
├── Obstacle/{ObstacleID}/        # 장애물 ID (예: 20001, 100002)
└── Item/{ItemID}/                # 아이템 ID
```

### 각 엔티티 하위 구조

각 ID 폴더는 다음과 같은 표준 구조를 따릅니다:

```
{EntityID}/
├── Back/                   # 뒷모습 (적 캐릭터 방향)
│   ├── IDLE/               # 대기 애니메이션 프레임
│   ├── MOVE/               # 이동 애니메이션 프레임
│   ├── ATK/                # 공격 애니메이션 프레임
│   ├── SKL/                # 스킬 애니메이션 프레임
│   └── DEAD/               # 죽음 애니메이션 프레임
├── Front/                  # 앞모습 (아군 캐릭터 방향)
│   ├── IDLE/
│   ├── MOVE/
│   ├── ATK/
│   ├── SKL/
│   └── DEAD/
└── GenerateResources/      # 자동 생성된 리소스
    ├── {ID}.spriteatlas    # 스프라이트 아틀라스
    ├── {ID}_AnimationController.controller  # 애니메이션 컨트롤러
    ├── CharacterView_{ID}.prefab  # 캐릭터 프리팹
    ├── Back_IDLE.anim      # 뒷모습 대기 애니메이션
    ├── Back_MOVE.anim      # 뒷모습 이동 애니메이션
    ├── Back_ATK.anim       # 뒷모습 공격 애니메이션
    ├── Back_SKL.anim       # 뒷모습 스킬 애니메이션
    ├── Back_DEAD.anim      # 뒷모습 죽음 애니메이션
    ├── Front_IDLE.anim     # 앞모습 대기 애니메이션
    ├── Front_MOVE.anim     # 앞모습 이동 애니메이션
    ├── Front_ATK.anim      # 앞모습 공격 애니메이션
    ├── Front_SKL.anim      # 앞모습 스킬 애니메이션
    └── Front_DEAD.anim     # 앞모습 죽음 애니메이션
```

### 애니메이션 상태 규칙

| 상태 | 폴더명 | 설명 |
|------|--------|------|
| 대기 | IDLE | 아무 행동도 하지 않는 기본 상태 |
| 이동 | MOVE | 캐릭터가 이동하는 상태 |
| 공격 | ATK | 일반 공격 모션 |
| 스킬 | SKL | 스킬/특수 공격 모션 |
| 죽음 | DEAD | 사망 모션 |

### ID 명명 규칙

#### Characters
- 8자리 숫자 ID (예: `14613502`, `15252102`, `17213101`)
- 앞 2자리: 등급/타입 구분
- 나머지: 순번 및 버전

#### Mob
- 5자리 또는 8자리 숫자 ID
- 5자리 (예: `10106`, `10206`): 일반 몹
- 8자리 (예: `30101001`, `30405003`): 엘리트/보스 몹

#### Obstacle
- 5자리 또는 6자리 숫자 ID
- 5자리 (예: `20001`): 일반 장애물
- 6자리 (예: `100002`, `100003`): 특수 장애물

---

## Gacha_STK 리소스 구조

가챠 시스템 관련 모든 리소스를 관리합니다.

### 주요 구성 요소

1. **FX_Popup_Get_Character**: 캐릭터 획득 팝업 이펙트
   - 01_Prefab: 프리팹
   - 02_Model: 3D 모델
   - 03_Texture: 텍스처
   - 04_Material: 머티리얼
   - 05_Animator: 애니메이터
   - 06_Animation: 애니메이션
   - 07_Timeline: 타임라인

2. **FX_Gacha_SR/SSR**: 등급별 가챠 연출 이펙트
   - Material: 머티리얼
   - Prefab: 프리팹
   - Shader: 셰이더
   - Texture: 텍스처

3. **Gacha_VFX**: 가챠 비주얼 이펙트
   - 01_Scenes: 씬 파일 및 배경
   - 02_Resources: 리소스 (애니메이션, 프리팹, 메시, 텍스처, 머티리얼)
   - 03_Timeline: 타임라인
   - 04_Prefabs: 프리팹

4. **Sprites**: 가챠 UI 스프라이트
   - BG: 배경 이미지
   - UI: UI 요소
     - Common: 공통 UI
     - Icons: 아이콘
     - Popup 시리즈: 각종 팝업 UI

---

## Prefabs 구조

### Fx (이펙트) 프리팹

캐릭터별로 폴더를 분리하여 관리합니다.

```
Fx/
├── {CharacterName}/        # 캐릭터 이름 (예: Adria, Alice, April)
├── Common/                 # 공통 이펙트
├── Monster/                # 몬스터 이펙트
└── UI/                     # UI 이펙트
```

### UI 프리팹

계층적 구조로 UI를 관리합니다.

```
UI/
├── 00_Main/                # 메인 화면
├── 01_Pops/                # 팝업
│   ├── ArenaPopup/
│   ├── CharacterCollectionPopup/
│   └── WindowPopup/
├── 02_Elements/            # UI 요소
│   ├── Btns/               # 버튼
│   ├── Illust_Characters/  # 캐릭터 일러스트
│   ├── Illust_Monsters/    # 몬스터 일러스트
│   ├── SD_Characters/      # SD 캐릭터
│   ├── SD_InfoMonsters/    # SD 정보 몬스터
│   └── Slots/              # 슬롯
├── InGame/                 # 인게임 UI
├── Loading/                # 로딩 UI
├── Lobby/                  # 로비 UI
├── Title/                  # 타이틀 UI
├── Top/                    # 상단 UI
└── UI_FX/                  # UI 이펙트
```

---

## 에셋 배치 규칙

### BuiltIn vs Remote 배치 기준

| 에셋 타입 | BuiltIn | Remote |
|----------|---------|--------|
| 필수 씬 (StartUp, Title) | ✅ | ❌ |
| 게임 씬 (Lobby, InGame) | ❌ | ✅ |
| 공통 셰이더/머티리얼 | ✅ | ❌ |
| 캐릭터별 이펙트 | ❌ | ✅ |
| 기본 폰트 | ✅ | ❌ |
| SD 캐릭터/몹 | ❌ | ✅ |
| 가챠 이펙트 | ❌ | ✅ |
| UI 프리팹 | 일부 | 대부분 |
| 스테이지 리소스 | ❌ | ✅ |
| VFX | ❌ | ✅ |

### 배치 원칙

1. **BuiltIn**: 앱 시작 시 즉시 필요한 최소한의 에셋만 포함
2. **Remote**: 게임 플레이 중 필요할 수 있는 모든 에셋
3. **번들 최적화**: 관련성 높은 에셋끼리 같은 폴더에 배치

---

## GenerateResources 폴더 규칙

`GenerateResources` 폴더는 자동 생성 시스템에 의해 관리됩니다.

### 자동 생성 파일 목록

1. **{ID}.spriteatlas**: 모든 프레임을 포함하는 스프라이트 아틀라스
2. **{ID}_AnimationController.controller**: 애니메이션 상태 머신
3. **CharacterView_{ID}.prefab**: 게임에서 사용되는 최종 프리팹
4. **{Direction}_{State}.anim**: 각 방향과 상태별 애니메이션 클립

### 주의사항

- GenerateResources 폴더의 파일은 직접 수정하지 않습니다
- 원본 이미지를 수정한 후 재생성 시스템을 실행합니다
- .meta 파일도 자동 생성되므로 Git에 포함해야 합니다

---

## 새 에셋 추가 가이드

### 1. 새 캐릭터 추가

```
1. Assets/_Project/Addressables/Remote/SD/Characters/{NewCharacterID}/ 폴더 생성
2. Back/ 및 Front/ 폴더 생성
3. 각 폴더 내에 IDLE, MOVE, ATK, SKL, DEAD 폴더 생성
4. 스프라이트 프레임을 각 상태 폴더에 배치
5. 자동 생성 시스템 실행 (GenerateResources 폴더 자동 생성)
6. Addressables 그룹에 추가
```

### 2. 새 이펙트 추가

```
1. 캐릭터별 이펙트: Remote/Prefabs/Fx/{CharacterName}/
2. 공통 이펙트: Remote/Prefabs/Fx/Common/
3. UI 이펙트: Remote/Prefabs/Fx/UI/
```

### 3. 새 UI 프리팹 추가

```
1. 메인 화면: Remote/Prefabs/UI/00_Main/
2. 팝업: Remote/Prefabs/UI/01_Pops/
3. UI 요소: Remote/Prefabs/UI/02_Elements/
```

---

## Addressables 그룹 설정

### 권장 그룹 구조

1. **Built In Data**: BuiltIn 폴더의 모든 에셋
2. **Characters**: Remote/SD/Characters/
3. **Mob**: Remote/SD/Mob/
4. **Obstacle**: Remote/SD/Obstacle/
5. **Data**: 게임 데이터 및 설정
6. **UI**: UI 프리팹 및 리소스
7. **VFX**: 이펙트 리소스
8. **Scenes**: 씬 파일

### 번들 전략

- 각 캐릭터는 개별 번들로 관리 (온디맨드 로딩)
- 공통 리소스는 BuiltIn에 포함
- 스테이지별 리소스는 그룹화하여 번들링

---

## 버전 관리 규칙

### Git 관리

- `.meta` 파일은 모두 Git에 포함
- `GenerateResources` 폴더도 Git에 포함
- 바이너리 에셋은 Git LFS 사용 권장

### 제외 항목

- `Addressables/aa/`: 빌드 결과물 (제외)
- `Addressables/AssetBundles/`: 번들 파일 (제외)

---

## 문서 버전

- **작성일**: 2025-12-16
- **버전**: 1.0
- **작성자**: 프로젝트 팀
