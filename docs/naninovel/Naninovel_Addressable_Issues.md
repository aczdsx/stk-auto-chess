# Naninovel Addressable 리소스 로딩 이슈 정리

## 개요

Naninovel + Addressables 조합에서 **에디터에서는 정상 작동하지만 빌드에서 리소스 로딩 실패**하는 이슈들을 정리합니다.

---

## 1. 스크립트 이중 프리픽스 문제 (Scripts Double Prefix)

### 증상
```
Failed to load 'Scripts/Prologue_01' resource
```

### 원인
Naninovel의 `ScriptRootResolver`가 모든 `.nani` 파일의 **공통 조상 경로**를 ScriptsRoot로 설정합니다.

**문제 발생 폴더 구조:**
```
NaninovelRes/
├── Scripts/
│   └── Prologue_01.nani
└── Localization/ja/Scripts/    ← 이 폴더 때문에 공통 조상이 NaninovelRes가 됨
    └── *.nani
```

이 경우:
- ScriptsRoot = `NaninovelRes` (Scripts 폴더가 아님)
- 스크립트 경로가 `Scripts/Prologue_01`로 저장됨
- ScriptLoader의 PathPrefix도 `Scripts`
- 로딩 시 `Scripts/Scripts/Prologue_01`로 검색 → 실패

### 해결책
**로컬라이제이션 스크립트를 메인 스크립트 폴더 밖으로 이동:**
```
NaninovelRes/Scripts/       ← 메인 스크립트만 (ScriptsRoot가 됨)
Localization/               ← NaninovelRes 밖으로 이동
```

또는 Localization 폴더 삭제 후 스크립트 Reimport.

---

## 2. Spawn/Prefab 경로 누락 문제

### 증상
```
Failed to load 'cbg_00_05' resource of type 'UnityEngine.GameObject'
```

### 원인
Addressable 주소에 서브폴더가 포함되어 있지만, 나니노벨 스크립트에서 서브폴더 없이 호출.

**Addressable 설정:**
```
주소: Naninovel/Spawn/CutScene/cbg_00_05
캐시 경로: Spawn/CutScene/cbg_00_05
```

**잘못된 호출:**
```nani
@spawn cbg_00_05
; → Spawn/cbg_00_05 검색 → 실패
```

### 해결책
**서브폴더 경로 포함하여 호출:**
```nani
@spawn CutScene/cbg_00_05
; → Spawn/CutScene/cbg_00_05 검색 → 성공
```

---

## 3. 에디터 vs 빌드 동작 차이

### 핵심 포인트
> **에디터에서는 항상 Editor 프로바이더가 우선 사용됨. Addressable 프로바이더 테스트는 반드시 빌드된 게임에서 수행해야 함.**

### 프로바이더 우선순위
- **에디터**: Editor Provider → Project Provider → Addressable Provider
- **빌드**: Addressable Provider → Project Provider

### 권장 디버깅 방법
1. `ResourceProviderConfiguration`에서 `Allow Addressable In Editor` 활성화
2. 또는 실제 빌드에서 테스트

---

## 4. Addressable 경로 규칙

### 주소 형식
```
Naninovel/{Category}/{SubFolder}/{ResourceName}
```

### 예시
| 리소스 타입 | Addressable 주소 | 캐시 경로 | 호출 방법 |
|------------|-----------------|----------|----------|
| Script | `Naninovel/Scripts/Prologue_01` | `Scripts/Prologue_01` | 자동 (Script.Path) |
| Background | `Naninovel/Backgrounds/MainBackground/bg_01` | `Backgrounds/MainBackground/bg_01` | `@back bg_01` |
| Spawn | `Naninovel/Spawn/CutScene/cbg_01` | `Spawn/CutScene/cbg_01` | `@spawn CutScene/cbg_01` |
| Character | `Naninovel/Characters/Sora/Default` | `Characters/Sora/Default` | `@char Sora` |

### 필수 레이블
모든 Naninovel Addressable 에셋에는 `Naninovel` 레이블이 필요합니다.

---

## 5. 디버깅 체크리스트

### 리소스 로딩 실패 시 확인사항

- [ ] Addressable Groups에 해당 에셋이 등록되어 있는가?
- [ ] 주소가 `Naninovel/`로 시작하는가?
- [ ] `Naninovel` 레이블이 할당되어 있는가?
- [ ] 서브폴더 경로가 올바르게 포함되어 있는가?
- [ ] ScriptsRoot가 올바른 폴더를 가리키는가? (Scripts 폴더 외부에 .nani 파일 없는지 확인)
- [ ] Addressables 빌드가 최신 상태인가?

### 로그 확인
빌드에서 아래와 같은 로그가 나오면 경로 불일치:
```
[ResourceLoader] Load called with path='XXX', PathPrefix='YYY'
[ResourceLoader] fullPath='YYY/XXX'  ← 이 경로가 캐시와 일치하는지 확인
```

---

## 참고 링크

- [Naninovel Resource Providers 공식 문서](https://naninovel.com/guide/resource-providers)
- [Unity Addressables 문서](https://docs.unity3d.com/Packages/com.unity.addressables@latest)
