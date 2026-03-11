# GameFlowState 시스템 개발 가이드

## 🎯 시스템 개요
GameFlowState는 게임의 다양한 모드(스테이지, PVP, 던전 등)와 규칙을 관리하는 핵심 시스템입니다. **새로운 모드나 규칙을 만들 때 이 가이드대로 구현하면 됩니다.**

## 🚀 빠른 시작: 새로운 모드 구현하기

### 1단계: 모드 타입 결정
```csharp
// 모드별 상속 구조
StateBase
├── StateReadyBase      // 준비 단계 (캐릭터 배치, UI 초기화)
├── StateCombatBase     // 전투 단계 (실제 게임플레이)
└── StateBase          // 결과 단계 (클리어/실패 처리)
```

### 2단계: FlowState 클래스 생성
```csharp
// 새로운 모드 예시: 보스 레이드
public class FlowStateBossRaidReady : StateReadyBase
{
    private SpecBossRaid _specBossRaid;
    
    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        _specBossRaid = data as SpecBossRaid;
        
        // 모드별 초기 설정
        SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_boss);
        InGameMain.GetInGameMain().SetVignette(3);
    }
    
    public override async void StateInit(object target)
    {
        // 보스 레이드 준비 로직 구현
        await SetupBossRaid();
    }
}
```

### 3단계: 모드별 로직 구현
```csharp
public class FlowStateBossRaidCombat : StateCombatBase
{
    public override void StateInit(object target)
    {
        // 전투 초기화
        SetupBossCombat();
    }
    
    public override void StateStart()
    {
        // 전투 시작 로직
        StartBossCombat();
    }
    
    public override void StateRunning(float dt)
    {
        // 전투 진행 로직
        UpdateBossCombat(dt);
    }
}
```

## 📋 모드 명세서 → 코드 변환 가이드

### 모드 명세서 예시
```
모드명: 보스 레이드
타입: 준비 → 전투 → 결과
특징:
- 보스 1마리 vs 플레이어 팀
- 보스는 단계별 패턴 변화
- 시간 제한: 5분
- 특별 규칙: 보스가 50% 체력 이하시 2단계
```

### 코드 변환
```csharp
public class FlowStateBossRaidReady : StateReadyBase
{
    private SpecBossRaid _specBossRaid;
    
    public override async void StateInit(object target)
    {
        // 1. 보스 생성
        var bossData = new CharacterStatData(
            _specBossRaid.boss_id, 
            _specBossRaid.boss_level, 
            _specBossRaid.boss_atk_multiplier, 
            _specBossRaid.boss_hp_multiplier
        );
        
        var bossPosition = new int2(4, 4); // 중앙 배치
        await InGameObjectManager.Instance.AddCharacterToField(
            bossData, bossPosition, AllianceType.Enemy,
            typeof(CharacterStateReady), true, HpBarType.Boss
        );
        
        // 2. 플레이어 팀 배치
        await SetupPlayerTeam();
        
        // 3. 특별 규칙 적용
        ApplyBossRaidRules();
    }
    
    private void ApplyBossRaidRules()
    {
        // 보스 레이드 전용 규칙 적용
        var timeLimit = new EffectCodeInfo(
            (long)EffectCodeNameType.BOSS_RAID_TIME_LIMIT, 
            0, 
            _specBossRaid.time_limit
        );
        InGameManager.Instance.EffectCodeContainer.AddOrMergeEffectCode(timeLimit, null);
    }
}
```

## 🛠️ 모드 타입별 구현 패턴

### 1. 준비 단계 (StateReadyBase)
```csharp
public class FlowStateNewModeReady : StateReadyBase
{
    private SpecNewMode _specData;
    
    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        _specData = data as SpecNewMode;
        
        // 모드별 초기 설정
        SetupModeEnvironment();
    }
    
    public override async void StateInit(object target)
    {
        // 1. 캐릭터 배치
        await SetupCharacters();
        
        // 2. 장애물/환경 설정
        await SetupEnvironment();
        
        // 3. UI 초기화
        InitModeUI();
        
        // 4. 특별 규칙 적용
        ApplyModeRules();
    }
    
    private async UniTask SetupCharacters()
    {
        var addCharacterTasks = new List<UniTask<CharacterController>>();
        
        // 적 캐릭터 배치
        foreach (var enemyData in _specData.enemy_list)
        {
            var statData = new CharacterStatData(
                enemyData.id, enemyData.level, 
                enemyData.atk_multiplier, enemyData.hp_multiplier
            );
            
            var position = new int2(enemyData.x, enemyData.y);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(
                statData, position, AllianceType.Enemy,
                typeof(CharacterStateReady), true, HpBarType.Synergy
            ));
        }
        
        // 플레이어 캐릭터 배치
        var playerDeck = UserDataManager.Instance.GetUserCharacterBattleDeckList(InGameType.NEW_MODE);
        foreach (var character in playerDeck)
        {
            var characterData = UserDataManager.Instance.GetUserCharacter(character.CharacterId);
            var characterStat = new CharacterStatData(
                characterData.CharacterId, characterData.Level,
                GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes()
            );
            
            var position = new int2(character.PositionTileX, character.PositionTileY);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(
                characterStat, position, AllianceType.Player,
                typeof(CharacterStateReady), true, HpBarType.Synergy
            ));
        }
        
        await UniTask.WhenAll(addCharacterTasks);
    }
}
```

### 2. 전투 단계 (StateCombatBase)
```csharp
public class FlowStateNewModeCombat : StateCombatBase
{
    private List<CharacterController> _characters;
    private bool _isEndCombat;
    private bool _isWin;
    
    public override void StateInit(object target)
    {
        _characters = ListPool<CharacterController>.Get();
        
        // 전투 초기화
        InGameObjectManager.Instance.ClearSynergyFx();
        InGameMain.GetInGameMain().InitCombatStateUI();
        InGameObjectManager.Instance.SaveStartingPlayerCharacter();
        
        // 카메라 설정
        InGameCommanderManager.Instance.InGameCamera.SetCameraSize(7.0f, new Vector3(0, 2.5f, -10), 1.0f).Forget();
    }
    
    public override void StateStart()
    {
        // 시너지 효과 적용
        ApplySynergyEffects();
        
        // 전투 시작 이펙트 코드 실행
        ExecuteCombatStartEffects();
        
        // 전투 시작
        StartCombat().Forget();
    }
    
    public override void StateRunning(float dt)
    {
        if (_isEndCombat) return;
        
        // 승리 조건 체크
        CheckWinCondition();
        
        // 패배 조건 체크
        CheckLoseCondition();
        
        // 시간 제한 체크
        CheckTimeLimit();
        
        if (_isEndCombat)
        {
            InGameManager.Instance.IsInGameCombat = false;
            ChangeNextState(_isWin).Forget();
        }
    }
    
    private void CheckWinCondition()
    {
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Enemy, _characters);
        if (_characters.Count == 0)
        {
            _isEndCombat = true;
            _isWin = true;
            InGameManager.Instance.AppEventResult = "clear";
            InGameManager.Instance.AppEventReason = "clear";
        }
    }
    
    private void CheckLoseCondition()
    {
        InGameObjectManager.Instance.GetAllAliveOnlyCharacters(AllianceType.Player, _characters);
        if (_characters.Count == 0)
        {
            _isEndCombat = true;
            _isWin = false;
            InGameManager.Instance.AppEventResult = "fail";
            InGameManager.Instance.AppEventReason = "dead";
        }
    }
}
```

### 3. 결과 단계 (StateBase)
```csharp
public class FlowStateNewModeClear : StateBase
{
    public override void StateStart()
    {
        // 결과 처리
        ProcessModeResult();
        
        // 보상 지급
        GiveRewards();
        
        // UI 표시
        ShowResultUI();
        
        // 이벤트 처리
        ProcessEvents();
    }
    
    private void ProcessModeResult()
    {
        var mvpCharacterData = SpecDataManager.Instance.GetCharacterData(
            InGameStatistics.Instance.GetMvpID()
        );
        
        // 별점 계산
        bool star2 = InGameMain.GetInGameMain().InGameTime >= 30;
        bool star3 = InGameObjectManager.Instance.IsCheckAllPlayerCharacterAlive();
        
        // 결과 UI 표시
        SceneUILayerManager.Instance.PushUILayerAsync<InGameResultPopup>(
            (true, star2, star3, mvpCharacterData)
        );
    }
}
```

## 🎯 기존 모드 분석

### 스테이지 모드
- **FlowStateStageReady**: 일반 스테이지 준비
- **FlowStateStageCombat**: 스테이지 전투
- **FlowStateStageClear/Fail**: 스테이지 결과

### PVP 모드
- **FlowStatePvpReady**: PVP 준비 (상대 덱 로드)
- **FlowStatePvpCombat**: PVP 전투
- **FlowStatePvpClear/Fail**: PVP 결과

### 던전 모드
- **FlowStateTrialDungeonReady**: 시련 던전 준비
- **FlowStateTrialDungeonCombat**: 던전 전투
- **FlowStateTrialDungeonClear/Fail**: 던전 결과

### 로비 모드
- **FlowStateLobbyCombat**: 자동 전투 (몬스터 스폰)

## 📊 StateBase 생명주기

### 1. SetStateData(object data)
```csharp
// 모드별 데이터 설정
public override void SetStateData(object data)
{
    base.SetStateData(data);
    _modeData = data as SpecModeData;
    
    // 모드별 초기 설정
    SetupModeEnvironment();
}
```

### 2. StateInit(object target)
```csharp
// 상태 초기화 (비동기 가능)
public override async void StateInit(object target)
{
    // 캐릭터 배치, 환경 설정 등
    await SetupGameField();
}
```

### 3. StateStart()
```csharp
// 상태 시작 (동기)
public override void StateStart()
{
    // 전투 시작, UI 활성화 등
    StartGameplay();
}
```

### 4. StateRunning(float dt)
```csharp
// 상태 실행 (매 프레임 호출)
public override void StateRunning(float dt)
{
    // 게임 로직 업데이트
    UpdateGameplay(dt);
}
```

### 5. StateEnd(bool isForced)
```csharp
// 상태 종료 (정리 작업)
public override void StateEnd(bool isForced)
{
    // 리소스 해제, 정리 작업
    CleanupResources();
}
```

## 🔧 특별 규칙 구현

### EffectCode를 활용한 규칙
```csharp
private void ApplyModeRules()
{
    // 시간 제한 규칙
    if (_specData.time_limit > 0)
    {
        var timeLimitCode = new EffectCodeInfo(
            (long)EffectCodeNameType.TIME_LIMIT, 
            0, 
            _specData.time_limit
        );
        InGameManager.Instance.EffectCodeContainer.AddOrMergeEffectCode(timeLimitCode, null);
    }
    
    // 특별 디버프 규칙
    if (_specData.special_debuff.Length > 0)
    {
        var debuffCode = new EffectCodeInfo(
            (long)_specData.special_debuff[0], 
            0, 
            _specData.debuff_stats
        );
        InGameManager.Instance.EffectCodeContainer.AddOrMergeEffectCode(debuffCode, null);
    }
}
```

### 환경 설정
```csharp
private void SetupModeEnvironment()
{
    // 카메라 설정
    InGameCommanderManager.Instance.InGameCamera.SetCameraSize(
        _specData.camera_size, 
        _specData.camera_position, 
        1.0f
    ).Forget();
    
    // 배경음 설정
    SoundManager.Instance.PlayBGM(_specData.bgm);
    
    // 비네트 설정
    InGameMain.GetInGameMain().SetVignette(_specData.vignette_type);
    
    // 보드 색상 변경
    InGameObjectManager.Instance.InGameStage.GraduallyChangeBoardColor(
        _specData.board_color, 
        1.0f
    );
}
```

## 📝 모드 구현 체크리스트

### ✅ 모드 구현 전 확인사항
1. **모드 타입 결정**: 준비/전투/결과 중 어떤 단계?
2. **상속 클래스 선택**: `StateReadyBase` / `StateCombatBase` / `StateBase`
3. **데이터 구조 정의**: 모드별 필요한 데이터는?
4. **특별 규칙 정의**: 모드만의 고유 규칙은?

### ✅ 구현해야 할 메서드
```csharp
public class FlowStateNewMode : StateReadyBase
{
    private SpecNewMode _specData;
    
    // 필수: 데이터 설정
    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        _specData = data as SpecNewMode;
    }
    
    // 필수: 상태 초기화
    public override async void StateInit(object target)
    {
        // 모드별 초기화 로직
    }
    
    // 필수: 상태 시작
    public override void StateStart()
    {
        // 모드별 시작 로직
    }
    
    // 필수: 상태 실행
    public override void StateRunning(float dt)
    {
        // 모드별 업데이트 로직
    }
    
    // 필수: 상태 종료
    public override void StateEnd(bool isForced)
    {
        // 모드별 정리 로직
    }
}
```

## 🚨 자주 하는 실수들

### ❌ 잘못된 예시
```csharp
// 1. SetStateData에서 데이터 캐스팅 안함
public override void SetStateData(object data)
{
    base.SetStateData(data);
    // _specData = data as SpecModeData; // ❌
}

// 2. StateInit에서 비동기 처리 안함
public override void StateInit(object target)
{
    // await SetupCharacters(); // ❌ (async void가 아님)
}

// 3. StateEnd에서 리소스 해제 안함
public override void StateEnd(bool isForced)
{
    // ListPool<CharacterController>.Release(_characters); // ❌
}
```

### ✅ 올바른 예시
```csharp
// 1. 데이터 캐스팅
public override void SetStateData(object data)
{
    base.SetStateData(data);
    _specData = data as SpecModeData; // ✅
}

// 2. 비동기 처리
public override async void StateInit(object target)
{
    await SetupCharacters(); // ✅
}

// 3. 리소스 해제
public override void StateEnd(bool isForced)
{
    ListPool<CharacterController>.Release(_characters); // ✅
    _characters = null;
}
```

## 🔍 디버깅 팁

### 모드가 시작되지 않을 때
1. **데이터 확인**: `SetStateData`에서 올바른 데이터가 전달되는지
2. **비동기 처리 확인**: `StateInit`에서 `await` 사용했는지
3. **상태 전환 확인**: `InGameMainFlowManager`에서 올바른 상태로 전환되는지

### 모드가 중단될 때
1. **예외 처리**: `try-catch`로 예외 상황 처리
2. **리소스 확인**: 메모리 누수나 리소스 부족 확인
3. **상태 체크**: `StateRunning`에서 조건 체크 로직 확인

## 📚 참고 자료

### 기존 모드 코드 참고
- `FlowStateStageReady.cs`: 일반 스테이지 준비
- `FlowStatePvpReady.cs`: PVP 준비
- `FlowStateTrialDungeonReady.cs`: 던전 준비
- `FlowStateLobbyCombat.cs`: 자동 전투

### 유용한 헬퍼 메서드
```csharp
// 캐릭터 배치
await InGameObjectManager.Instance.AddCharacterToField(statData, position, alliance, stateType, showHpBar, hpBarType);

// 카메라 설정
InGameCommanderManager.Instance.InGameCamera.SetCameraSize(size, position, duration);

// 사운드 재생
SoundManager.Instance.PlayBGM(bgmType);

// UI 초기화
InGameMain.GetInGameMain().InitReadyStateUI(battleDeckList);

// 상태 전환
InGameMainFlowManager.Instance.AddNextState<FlowStateNextMode>();
```

이제 새로운 모드나 규칙을 만들 때 이 가이드대로 구현하면 됩니다! 🎯
