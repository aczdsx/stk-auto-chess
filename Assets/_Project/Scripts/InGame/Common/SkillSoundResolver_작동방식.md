# SkillSoundResolver 작동 방식 설명

## 핵심 동작 원리

### ❌ 파일 시스템을 직접 조회하지 않습니다
- 실제 파일(.wav, .mp3 등)을 디스크에서 읽지 않습니다
- 파일 시스템 API를 사용하지 않습니다

### ✅ Unity AudioController의 등록된 사운드 Dictionary를 조회합니다

## 작동 흐름

### 1. 게임 시작 시 (초기화)
```
Unity AudioController
  └── InitializeAudioItems()
      └── Unity 에디터에서 설정한 모든 AudioItem들을 Dictionary에 등록
          └── Dictionary<string, AudioItem> _audioItems
              예: {
                    "snd_sfx_skill_a_3401": AudioItem,
                    "snd_sfx_skill_a_3401_01": AudioItem,
                    "snd_sfx_skill_a_3401_02": AudioItem,
                    ...
                  }
```

### 2. 스킬 사운드 조회 시 (런타임)
```
SkillSoundResolver.GetCachedSoundNames()
  └── ComputeSoundNames()
      └── for (version 1 to 99)
          └── "snd_sfx_skill_a_3401_01" 생성
              └── AudioController.IsValidAudioID("snd_sfx_skill_a_3401_01")
                  └── AudioController._GetAudioItem("snd_sfx_skill_a_3401_01")
                      └── _audioItems.TryGetValue("snd_sfx_skill_a_3401_01", out item)
                          └── Dictionary에서 찾음 → true 반환
                          └── Dictionary에 없음 → false 반환
```

## 중요한 포인트

### ✅ 장점
1. **빠른 조회**: Dictionary 조회는 O(1) 시간 복잡도
2. **파일 I/O 없음**: 디스크 접근 없이 메모리에서만 조회
3. **캐싱 가능**: 한 번 조회한 결과를 메모리에 캐싱

### ⚠️ 주의사항

#### 1. **AudioController에 등록이 필수입니다**
   - ❌ **Addressables에만 등록하면 작동하지 않습니다**
   - ✅ **AudioController의 AudioCategory에 AudioItem으로 등록해야 합니다**
   - 사운드 파일은 Addressables에 포함 가능하지만, AudioController에도 등록 필요

#### 2. **등록 방법**

   **방법 A: 에디터에서 등록 (권장)**
   - Unity 에디터에서 AudioController의 AudioCategory에 AudioItem으로 등록
   - 게임 시작 시 `InitializeAudioItems()`에서 Dictionary에 자동 등록
   - 가장 간단하고 안정적

   **방법 B: 런타임에 등록**
   ```csharp
   // Addressables로 AudioClip 로드
   var handle = Addressables.LoadAssetAsync<AudioClip>("snd_sfx_skill_a_3401_01");
   var audioClip = await handle;
   
   // AudioController에 등록
   var category = AudioController.GetCategory("SFX"); // 또는 적절한 카테고리
   AudioController.AddToCategory(category, audioClip, "snd_sfx_skill_a_3401_01");
   
   // Dictionary 갱신 (중요!)
   AudioController.Instance.InitializeAudioItems();
   
   // ⚠️ 중요: Addressables Handle은 Release하지 마세요!
   // AudioController가 AudioClip을 참조하고 있으므로, 
   // Handle을 Release하면 AudioClip이 유효하지 않게 됩니다.
   // Handle은 AudioController에서 제거할 때 함께 Release해야 합니다.
   ```
   - 런타임에 등록한 후 `InitializeAudioItems()`를 호출해야 Dictionary에 반영됨
   - 등록 후 SkillSoundResolver 캐시를 초기화해야 할 수 있음
   - **Addressables Handle 관리**: Handle을 저장해두고, AudioController에서 제거할 때 Release해야 함

#### 3. **런타임에 추가된 사운드**
   - 게임 시작 시점에 등록된 사운드만 기본적으로 조회 가능
   - 런타임에 추가한 경우 `InitializeAudioItems()` 호출 후 조회 가능
   - SkillSoundResolver 캐시가 있으면 `ClearCache()` 또는 `RemoveCache(characterId)` 호출 필요

#### 4. **사운드 파일이 있어도 등록 안 되면 안 됨**
   - 파일이 존재해도 AudioController에 등록되지 않으면 `IsValidAudioID()`는 false 반환
   - Addressables에만 있어도 AudioController Dictionary에 없으면 감지 안 됨

#### 5. **Addressables Handle 관리 (런타임 등록 시)**
   - ⚠️ **Addressables로 로드한 AudioClip의 Handle을 즉시 Release하면 안 됩니다**
   - AudioController가 AudioClip을 참조하고 있으므로, Handle을 Release하면 AudioClip이 유효하지 않게 됩니다
   - **올바른 방법**:
     ```csharp
     // Handle 저장
     private Dictionary<string, AsyncOperationHandle<AudioClip>> _audioHandles = new();
     
     // 로드 및 등록
     var handle = Addressables.LoadAssetAsync<AudioClip>("snd_sfx_skill_a_3401_01");
     _audioHandles["snd_sfx_skill_a_3401_01"] = handle;
     var audioClip = await handle;
     AudioController.AddToCategory(category, audioClip, "snd_sfx_skill_a_3401_01");
     
     // 제거 시 Release
     AudioController.RemoveAudioItem("snd_sfx_skill_a_3401_01");
     if (_audioHandles.TryGetValue("snd_sfx_skill_a_3401_01", out var storedHandle))
     {
         Addressables.Release(storedHandle);
         _audioHandles.Remove("snd_sfx_skill_a_3401_01");
     }
     ```
   - **에디터에서 등록한 경우**: Addressables Handle 관리가 필요 없음 (AudioController가 자동 관리)

## 실제 예시

### 시나리오: 캐릭터 ID 3401의 스킬 사운드 조회

```csharp
var resolver = new SkillSoundResolver(3401);
var sounds = resolver.GetCachedSoundNames();
```

**1단계: 캐시 확인**
- `_soundCache.TryGetValue(3401)` → 없음

**2단계: 사운드 이름 계산**
- `ComputeSoundNames()` 호출
- "snd_sfx_skill_a_3401" 생성 → `IsValidAudioID()` → Dictionary 조회
- "snd_sfx_skill_a_3401_01" 생성 → `IsValidAudioID()` → Dictionary 조회
- "snd_sfx_skill_a_3401_02" 생성 → `IsValidAudioID()` → Dictionary 조회
- "snd_sfx_skill_a_3401_03" 생성 → `IsValidAudioID()` → Dictionary에 없음 → false

**3단계: 결과 캐싱**
- `["snd_sfx_skill_a_3401_01", "snd_sfx_skill_a_3401_02"]` 배열 생성
- `_soundCache[3401] = 배열` 저장

**4단계: 다음 조회 시**
- `_soundCache.TryGetValue(3401)` → 캐시된 배열 반환 (Dictionary 조회 없음)

## 성능 특성

### Dictionary 조회 비용
- **시간**: O(1) - 매우 빠름
- **메모리**: Dictionary는 이미 메모리에 로드됨
- **디스크 I/O**: 없음

### 캐싱 효과
- **첫 번째 조회**: Dictionary 조회 1~99번 (최대)
- **두 번째 조회 이후**: Dictionary 조회 0번 (캐시 사용)

## 결론

**SkillSoundResolver는 파일을 조회하는 것이 아니라, Unity AudioController에 미리 등록된 사운드 목록을 Dictionary에서 조회하는 방식입니다.**

따라서:
- ✅ 빠르고 효율적
- ✅ 파일 I/O 오버헤드 없음
- ⚠️ Unity 에디터에서 사운드를 등록해야 함
