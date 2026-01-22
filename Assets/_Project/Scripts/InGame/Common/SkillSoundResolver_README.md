# SkillSoundResolver 사용 가이드

## 개요
`SkillSoundResolver`는 스킬 사운드의 여러 버전(01, 02 등)을 자동으로 감지하고 함께 재생할 수 있도록 도와주는 클래스입니다.

## 사운드 네이밍 규칙

### 기본 형식
```
snd_sfx_skill_a_{characterId}_{version}
```

### 예시
- 캐릭터 ID가 3401인 경우:
  - `snd_sfx_skill_a_3401` (기본 사운드, 버전 없음)
  - `snd_sfx_skill_a_3401_01` (첫 번째 버전)
  - `snd_sfx_skill_a_3401_02` (두 번째 버전)
  - `snd_sfx_skill_a_3401_03` (세 번째 버전)
  - ... (최대 99까지)

### 규칙
1. **버전 번호는 01부터 시작**하며, 두 자리 숫자 형식입니다 (01, 02, ..., 99)
2. **버전이 없는 기본 사운드**도 지원합니다 (예: `snd_sfx_skill_a_3401`)
3. **여러 버전이 있으면 모두 함께 재생**됩니다
4. 버전은 **연속적일 필요는 없습니다** (01, 03만 있어도 가능)

## 사용 방법

### 기본 사용 (자동)
```csharp
// EffectCodeCharacterBase를 상속한 클래스에서는 자동으로 처리됩니다
public override void Activate()
{
    // 내부적으로 SkillSoundResolver를 사용하여 모든 버전을 찾아 재생
    base.Activate();
}
```

### 수동 사용
```csharp
// 1. 리졸버 생성
var resolver = new SkillSoundResolver(characterId);

// 2. 사용 가능한 모든 사운드 이름 가져오기
var soundNames = resolver.GetAvailableSoundNames();
// 결과 예: ["snd_sfx_skill_a_3401_01", "snd_sfx_skill_a_3401_02"]

// 3. 모든 사운드 재생
SoundManager.Instance.PlaySkillSounds(soundNames);
```

### 첫 번째 사운드만 가져오기
```csharp
var resolver = new SkillSoundResolver(characterId);
string firstSound = resolver.GetFirstAvailableSoundName();
if (firstSound != null)
{
    SoundManager.Instance.PlaySFX(firstSound);
}
```

## 사운드 엔지니어 작업 가이드

### 파일 구조
```
Sounds/
  └── Skills/
      └── Character/
          ├── snd_sfx_skill_a_3401.wav          (기본 사운드, 선택사항)
          ├── snd_sfx_skill_a_3401_01.wav      (첫 번째 버전)
          ├── snd_sfx_skill_a_3401_02.wav      (두 번째 버전)
          └── ...
```

### 작업 순서
1. **캐릭터 ID 확인**: 개발자에게 캐릭터 ID를 확인하세요
2. **사운드 파일 생성**: 
   - 파일명: `snd_sfx_skill_a_{characterId}_{version}.wav`
   - 예: `snd_sfx_skill_a_3401_01.wav`
3. **AudioController에 등록**: Unity의 AudioController에 사운드를 등록하세요
4. **테스트**: 게임에서 스킬을 사용하여 모든 버전이 재생되는지 확인하세요

### 주의사항
- ✅ 파일명은 정확히 일치해야 합니다 (대소문자 구분)
- ✅ 버전 번호는 01부터 시작하며, 두 자리 숫자 형식입니다
- ✅ 여러 버전이 있으면 모두 함께 재생되므로, 동시 재생을 고려하여 믹싱하세요
- ❌ 버전 번호를 건너뛰면 안 됩니다 (01 다음에 03이 오면 02는 체크되지 않습니다)

### 예시 시나리오

#### 시나리오 1: 단일 사운드
- 파일: `snd_sfx_skill_a_3505.wav`
- 결과: 하나의 사운드만 재생됩니다

#### 시나리오 2: 여러 버전 사운드
- 파일: 
  - `snd_sfx_skill_a_3404_01.wav`
  - `snd_sfx_skill_a_3404_02.wav`
- 결과: 두 사운드가 동시에 재생됩니다

#### 시나리오 3: 기본 + 버전 사운드
- 파일:
  - `snd_sfx_skill_a_3401.wav` (기본)
  - `snd_sfx_skill_a_3401_01.wav` (버전 01)
- 결과: 두 사운드가 동시에 재생됩니다

## 기술 세부사항

### 최적화
- 사운드 존재 여부는 `AudioController.IsValidAudioID()`를 통해 런타임에 확인됩니다
- 연속된 버전이 없으면 조기 종료하여 불필요한 체크를 방지합니다
- 최대 버전 수는 99로 제한되어 있습니다

### 성능
- 사운드 체크는 메모리 캐싱 없이 매번 수행됩니다 (AudioController가 내부적으로 캐싱)
- 일반적으로 스킬 사용 빈도가 높지 않으므로 성능 영향은 미미합니다

## 문제 해결

### Q: 사운드가 재생되지 않아요
1. 파일명이 정확한지 확인하세요
2. AudioController에 사운드가 등록되어 있는지 확인하세요
3. `AudioController.IsValidAudioID()`로 사운드 존재 여부를 확인하세요

### Q: 여러 버전이 있는데 하나만 재생돼요
- `PlaySkillSounds()` 메서드를 사용하고 있는지 확인하세요
- `GetAvailableSoundNames()`로 모든 사운드가 감지되는지 확인하세요

### Q: 버전 번호를 1, 2로 사용할 수 있나요?
- 아니요. 반드시 두 자리 숫자 형식(01, 02)을 사용해야 합니다.
