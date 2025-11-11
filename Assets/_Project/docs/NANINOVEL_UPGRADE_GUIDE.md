# Naninovel 1.18 → 1.20 업그레이드 가이드

## 개요
이 문서는 Naninovel 1.18에서 1.20로 업그레이드할 때 필요한 주요 변경사항을 정리한 가이드입니다.

## 주요 Breaking Changes

### 1. Command 클래스 변경사항

#### 1.18 이전
```csharp
[CommandAlias("end")]
public class NaninovelEndCommand : Command
{
    public override UniTask Execute(ExecutionContext ctx)
    {
        // 구현
        return UniTask.CompletedTask;
    }
}
```

#### 1.20 현재
```csharp
public class NaninovelEndCommand : Command
{
    public override UniTask Execute(AsyncToken token = default)
    {
        // 구현
        return UniTask.CompletedTask;
    }
}
```

**변경사항:**
- `[CommandAlias]` 속성 제거 (클래스 이름으로 자동 인식)
- `Execute(ExecutionContext ctx)` → `Execute(AsyncToken token = default)`
- `override` 키워드 유지

### 2. 텍스트 시스템 변경 (1.19)

#### uGUI → TMPro 전환
- **모든 커스텀 UI의 uGUI 텍스트를 TMPro로 변경 필요**
- 기본 UI들이 모두 TMPro로 업데이트됨
- 폰트 설정 방식 변경

**영향받는 부분:**
- 커스텀 텍스트 프린터
- 커스텀 UI의 텍스트 컴포넌트
- 폰트 설정

### 3. 로컬라이제이션 시스템 변경 (1.19)

#### 스크립트 로컬라이제이션
- **기존 로컬라이제이션 문서 삭제 후 재생성 필요**
- `.nani` → `.txt` 형식으로 변경
- 로컬라이제이션 문서에는 **로컬라이제 가능한 텍스트만** 포함
- 인라인 커맨드, 스크립트 표현식, 작가 이름 등은 더 이상 보존하지 않음

**마이그레이션:**
1. 기존 로컬라이제이션 문서 백업
2. 새로운 시스템으로 재생성
3. 텍스트만 다시 입력

#### Managed Text 변경
- 줄바꿈: `\n` → `<br>` 태그 사용
- Tips 문서 형식이 멀티라인으로 변경 (기본값)
- Generic Text Line의 작가 표현식 제거 → Display Name 사용

### 4. Auto Voicing 변경 (1.19)

#### Voice Map 재매핑 필요
- **기존 Voice Map 삭제 후 재생성 필요**
- `Content Hash` 모드 → `Text ID` 모드로 변경 (기본값)
- 클립이 스크립트 라인 콘텐츠 해시가 아닌 **로컬라이제이션 텍스트 ID**와 연관됨

### 5. Input System 변경 (1.19)

#### 새로운 Input Binding 추가 필요
새로운 "Adapt to Input Mode" 기능 사용 시 다음 바인딩 추가:
- `Page`
- `Tab`
- `Delete`
- `NavigateX`
- `NavigateY`
- `ScrollY`
- `ToggleConsole`

모두 `Always Process` 옵션 활성화 필요

**제거된 옵션:**
- "Toggle Console Key" 옵션 제거 → `ToggleConsole` 입력 바인딩으로 대체

### 6. UI 변경사항

#### Title UI
- "Show Title UI" 옵션 제거
- Title UI 프리팹에 `Visible On Awake` 활성화 필요
- `Visible On Awake`가 활성화된 모든 UI는 엔진 초기화 후 자동 표시

#### Script Navigator
- Script Navigator UI 프리팹이 Default UIs로 이동
- `IScriptNavigatorUI` 인터페이스로 제어
- "Enable Navigator", "Navigator Order" 옵션 제거
- "Show Navigator On Init" → "Show Script Navigator"로 이름 변경

### 7. 폰트 시스템 변경 (1.19)

#### Font Assets
- UI 설정에서 폰트 에셋 직접 할당 불가
- **폰트 리소스 경로** 사용 (Addressables 사용 시 중복 방지)
- `Font Sizes` 에셋 추가 (에셋 컨텍스트 메뉴: `Create -> Naninovel -> Font Sizes`)
- 여러 UI 간 폰트 크기 공유 가능

#### Font Change Configuration
- `Include Children` 옵션 추가
- 폰트 변경이 자식 게임 오브젝트의 모든 텍스트 컴포넌트에 영향

### 8. 텍스트 리빌 시스템 변경 (1.19)

#### Revealable Text
- `Slack Opacity` 옵션 추가 (마지막 append 이전 텍스트 투명도 조절)
- `Reveal Paginator` 컴포넌트 추가 (오버플로우 시 가상 페이지 분할)
- 이벤트 태그 추가 (텍스트 리빌 시 임의 액션 실행)

#### Text Printer
- `Reveal Instantly` 옵션 추가 (항상 전체 메시지 즉시 표시)
- Generic Line에 출력할 텍스트가 없으면 입력 대기하지 않음

### 9. 커맨드 변경사항

#### 새로운 커맨드
- `@blur` - 블러 효과
- `@bokeh` - 보케 효과
- `@glitch` - 글리치 효과
- `@rain` - 비 효과
- `@shake` - 흔들림 효과
- `@snow` - 눈 효과
- `@sun` - 태양 효과

#### 커맨드 파라미터 변경
- `@resetText`: `resetAuthor` 파라미터 추가
- `@animate`: 기본 키프레임 구분자가 `,` (콤마)로 변경, `|`는 하위 호환성 유지

### 10. 기타 변경사항

#### Spreadsheet Tool
- `.xlsx` → `.csv` 형식으로 변경
- 로컬라이제이션 가능한 텍스트만 포함
- Import 시 소스 스크립트 수정하지 않음

#### Bridging (VS Code Extension)
- **Newtonsoft Json 패키지 필요** (Unity Package Manager에서 설치)
- Editor 전용, 빌드에는 불필요

#### Character Display Names
- Managed Text에서 가져오기 (소스 로케일에서도)
- Actor 설정의 값보다 우선

#### Audio Routing
- Video 배경 오디오 → BGM 오디오 믹서 그룹
- Video 캐릭터 오디오 → Voice 그룹
- 이전: 둘 다 Master 그룹

## 업그레이드 체크리스트

### 필수 작업
- [ ] `Naninovel` 폴더 삭제 후 새 버전 임포트
- [ ] 커스텀 Command 클래스의 `Execute(ExecutionContext ctx)` → `Execute(AsyncToken token = default)` 변경
- [ ] 커스텀 UI의 uGUI 텍스트 → TMPro 변경
- [ ] 스크립트 로컬라이제이션 문서 재생성
- [ ] Voice Map 재매핑
- [ ] Managed Text의 `\n` → `<br>` 변경
- [ ] Title UI에 `Visible On Awake` 활성화

### 선택 작업
- [ ] Extension 패키지 업데이트 (Live2D, Spine, Inventory 등)
- [ ] Input Binding 추가 (Adapt to Input Mode 사용 시)
- [ ] Font Sizes 에셋 생성 및 적용
- [ ] Newtonsoft Json 패키지 설치 (VS Code Extension 사용 시)
- [ ] 커스텀 UI 폰트 설정을 경로 기반으로 변경

### 확인 사항
- [ ] 기존 세이브 파일 호환성 확인
- [ ] 커스텀 UI 동작 확인
- [ ] 로컬라이제이션 동작 확인
- [ ] Voice 재생 확인

## 코드 예시

### Command 클래스 업그레이드 예시

**Before (1.18):**
```csharp
[CommandAlias("end")]
public class NaninovelEndCommand : Command
{
    public override UniTask Execute(ExecutionContext ctx)
    {
        // 구현
        return UniTask.CompletedTask;
    }
}
```

**After (1.20):**
```csharp
public class NaninovelEndCommand : Command
{
    public override UniTask Execute(AsyncToken token = default)
    {
        // 구현
        return UniTask.CompletedTask;
    }
}
```

### Managed Text 줄바꿈 변경

**Before:**
```
Item1Path: Line 1\nLine 2
```

**After:**
```
Item1Path: Line 1<br>Line 2
```

### Generic Text Line 작가 표현식 변경

**Before:**
```
{mc}: Hello
```

**After:**
```
MainCharacter: Hello  // Display Name 사용
```

## 참고 링크
- [Naninovel 1.19 Release Notes](https://pre.naninovel.com/releases/1.19)
- [Naninovel 1.20 Release Notes](https://pre.naninovel.com/releases/1.20)

## 프로젝트별 수정 가이드

### 현재 프로젝트 확인사항

#### 1. 로컬라이제이션 파일 형식 변경 ⚠️ **중요**
**현재 상태:** `Assets/StellaKnights/Dialogue/Localization/*/Scripts/*.nani`  
**변경 필요:** `.nani` → `.txt` 형식으로 변환

**기존 형식 (1.18):**
```
; Korean <ko> to Chinese (T) <zh-TW> localization document
# 5d4c553d
; (어느 버려진 연구소의, 데이터 전산실 안)
（在某個廢棄研究所的數據電算室內）

# 70456779
; cley_00: 어, 어쩌지… 저질러 버렸어.….
cley_00: 怎，怎麼辦…糟了…。
```

**새 형식 (1.20):**
```
; 로컬라이제이션 가능한 텍스트만 포함
（在某個廢棄研究所的數據電算室內）
怎，怎麼辦…糟了…。
我…
我…好像回不去了……。
```

**주요 변경점:**
- 해시 ID (`# 5d4c553d`) 제거
- 주석 (`;`) 제거
- 작가 이름 (`cley_00:`) 제거
- **텍스트만** 포함

**작업 순서:**
1. 기존 `.nani` 로컬라이제이션 파일 전체 백업
2. Naninovel Editor에서 `Tools > Localization > Generate Localization Documents` 실행
3. 새로 생성된 `.txt` 파일에 텍스트만 복사하여 재입력
4. 기존 `.nani` 파일 삭제 (백업은 보관)

#### 2. 커스텀 Command 확인
**확인된 파일:** `Assets/_Project/Extension/NaninovelExtension/NaninovelEndCommand.cs`
- ✅ 이미 `Execute(AsyncToken token = default)` 형태로 수정됨
- ✅ `override` 키워드 사용
- ✅ `[CommandAlias]` 제거됨

#### 3. 로컬라이제이션 사용 코드
**확인된 파일:** `Assets/_Project/Scripts/Scene/SceneDialog.cs`
```csharp
// 현재 코드 (정상 동작)
await localizationManager.SelectLocale(locale);
```
- ✅ `SelectLocale` 메서드는 정상 동작
- ⚠️ 로컬라이제이션 파일 형식만 `.txt`로 변경 필요

#### 4. 커스텀 UI 확인
**확인된 파일:**
- `DialogueSkipButton.cs`
- `DialogueAutoPlayButton.cs`

**확인 사항:**
- 텍스트 컴포넌트가 TMPro인지 확인
- uGUI Text 사용 시 TMPro로 변경 필요

## 주의사항
- **프로젝트 백업 필수** (VCS 사용 권장)
- 기존 세이브 파일은 새 버전에서 예상치 못한 동작 가능
- 배포된 프로젝트 패치 시 기존 세이브 호환성 확인 필수
- VS Code Extension 사용 시 Naninovel 패키지와 VS Code 모두 최신 버전으로 업데이트
- **로컬라이제이션 파일은 반드시 백업 후 재생성** (기존 `.nani` 파일은 삭제)

