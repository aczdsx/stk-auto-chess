# EffectCode 시스템 개발 가이드

## 🎯 시스템 개요
EffectCode는 게임의 모든 효과(스킬, 버프, 디버프, CC기 등)를 관리하는 핵심 시스템입니다. **스킬 명세서를 받으면 이 가이드대로 구현하면 됩니다.**

## 🚀 빠른 시작: 스킬 구현하기

### 1단계: 스킬 타입 결정
```csharp
// 스킬 종류별 상속 구조
EffectCodeBase
├── EffectCodeCharacterBase     // 캐릭터 스킬
│   ├── EffectCodeBuffBase      // 버프 스킬
│   ├── EffectCodeDebuffBase    // 디버프 스킬
│   └── EffectCodeCrowdControlBase // CC 스킬
├── EffectCodeStatBase          // 스탯 변경 스킬
└── EffectCodeGameBase          // 게임 전역 스킬
```

### 2단계: 스킬 클래스 생성
```csharp
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSkill1101001 : EffectCodeCharacterBase
{
    private const int CodeId = 1101001; // 스킬 ID
    
    public override EffectCodeType Type => EffectCodeType.Character;
    
    // 스킬 구현...
}
```

### 3단계: 스킬 로직 구현
```csharp
public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
{
    base.Initialize(codeInfo, container, source);
    
    // 스킬 데이터 파싱
    float damage = codeInfo.GetCodeStatToFloat(0);  // 데미지
    float range = codeInfo.GetCodeStatToFloat(1);   // 범위
    int duration = codeInfo.GetCodeStatToInt(2);    // 지속시간
    
    // 스킬 실행 로직
    ExecuteSkill(damage, range, duration);
}
```

## 📋 스킬 명세서 → 코드 변환 가이드

### 스킬 명세서 예시
```
스킬명: 화염구
ID: 1101001
타입: 데미지 스킬
효과: 
- 데미지: 150% 공격력
- 범위: 3m 반경
- 지속시간: 즉시
- 쿨타임: 5초
```

### 코드 변환
```csharp
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSkill1101001 : EffectCodeCharacterBase
{
    private const int CodeId = 1101001;
    
    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        
        // 명세서 → 코드 변환
        float damageMultiplier = codeInfo.GetCodeStatToFloat(0); // 1.5 (150%)
        float range = codeInfo.GetCodeStatToFloat(1);            // 3.0 (3m)
        float cooldown = codeInfo.GetCodeStatToFloat(2);         // 5.0 (5초)
        
        ExecuteFireball(damageMultiplier, range, cooldown);
    }
    
    private void ExecuteFireball(float damageMultiplier, float range, float cooldown)
    {
        // 화염구 실행 로직
        var targets = GetTargetsInRange(range);
        foreach (var target in targets)
        {
            float damage = owner.AttackPower * damageMultiplier;
            target.TakeDamage(damage);
        }
    }
}
```

## 🛠️ 스킬 타입별 구현 패턴

### 1. 데미지 스킬
```csharp
// 즉시 데미지
public class EffectCodeSkillDamage : EffectCodeCharacterBase
{
    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        
        float damage = codeInfo.GetCodeStatToFloat(0);
        float range = codeInfo.GetCodeStatToFloat(1);
        
        // 즉시 실행
        DealDamage(damage, range);
    }
}
```

### 2. 버프 스킬
```csharp
// 지속시간 있는 버프
public class EffectCodeSkillBuff : EffectCodeBuffBase
{
    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        
        float statIncrease = codeInfo.GetCodeStatToFloat(0);
        float duration = codeInfo.GetCodeStatToFloat(1);
        
        // 버프 적용
        ApplyBuff(statIncrease, duration);
    }
}
```

### 3. CC 스킬
```csharp
// 스턴, 슬로우 등
public class EffectCodeSkillCC : EffectCodeCrowdControlBase
{
    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        
        float duration = codeInfo.GetCodeStatToFloat(0);
        float range = codeInfo.GetCodeStatToFloat(1);
        
        // CC 적용
        ApplyCrowdControl(duration, range);
    }
}
```

## 📊 EffectCodeInfo 데이터 구조

### 스탯 인덱스 활용법
```csharp
// 스킬별로 스탯 인덱스 정의
public class EffectCodeSkill1101001 : EffectCodeCharacterBase
{
    // 인덱스 0: 데미지 배율
    // 인덱스 1: 범위
    // 인덱스 2: 지속시간
    // 인덱스 3: 쿨타임
    // 인덱스 4: 추가 효과 값
    // ...
    
    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        
        // 명확한 변수명으로 데이터 파싱
        float damageMultiplier = codeInfo.GetCodeStatToFloat(0);
        float skillRange = codeInfo.GetCodeStatToFloat(1);
        float duration = codeInfo.GetCodeStatToFloat(2);
        float cooldown = codeInfo.GetCodeStatToFloat(3);
        
        // 스킬 로직 구현...
    }
}
```

## 🎯 스킬 실행 플로우

### 1. 스킬 발동 과정
```csharp
// 1. 스킬 명세서 데이터로 EffectCodeInfo 생성
var codeInfo = new EffectCodeInfo(
    codeId: 1101001,
    priority: 100,
    stat1: 1.5f,  // 데미지 배율
    stat2: 3.0f,  // 범위
    stat3: 5.0f   // 쿨타임
);

// 2. 컨테이너에 스킬 추가
var skill = container.AddOrMergeEffectCode(codeInfo, source);

// 3. Initialize() 자동 호출 → 스킬 실행
```

### 2. 스킬 병합 시스템
```csharp
// 같은 스킬이 이미 있으면 Merge() 호출
public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
{
    base.Merge(codeInfo, source);
    
    // 스택 증가, 지속시간 갱신 등
    UpdateSkillStack(codeInfo);
}
```

## 🔧 실제 구현 예시

### 화염구 스킬 (즉시 데미지)
```csharp
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSkill1101001 : EffectCodeCharacterBase
{
    private const int CodeId = 1101001;
    
    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        
        float damageMultiplier = codeInfo.GetCodeStatToFloat(0);
        float range = codeInfo.GetCodeStatToFloat(1);
        
        // 범위 내 적 찾기
        var targets = FindTargetsInRange(range);
        
        // 데미지 계산 및 적용
        foreach (var target in targets)
        {
            float damage = owner.AttackPower * damageMultiplier;
            target.TakeDamage(damage, DamageType.Fire);
        }
        
        // 이펙트 재생
        PlayFireballEffect(range);
    }
}
```

### 힐링 스킬 (즉시 회복)
```csharp
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSkill1101002 : EffectCodeCharacterBase
{
    private const int CodeId = 1101002;
    
    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        
        float healAmount = codeInfo.GetCodeStatToFloat(0);
        float range = codeInfo.GetCodeStatToFloat(1);
        
        // 범위 내 아군 찾기
        var allies = FindAlliesInRange(range);
        
        // 힐링 적용
        foreach (var ally in allies)
        {
            ally.Heal(healAmount);
        }
        
        // 힐링 이펙트 재생
        PlayHealingEffect(range);
    }
}
```

### 버프 스킬 (지속시간 있음)
```csharp
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSkill1101003 : EffectCodeBuffBase
{
    private const int CodeId = 1101003;
    
    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        
        float attackIncrease = codeInfo.GetCodeStatToFloat(0);
        float duration = codeInfo.GetCodeStatToFloat(1);
        
        // 버프 적용
        owner.AddStatModifier(StatType.AttackPower, attackIncrease);
        
        // 지속시간 후 제거 예약
        StartCoroutine(RemoveAfterDuration(duration));
    }
    
    private IEnumerator RemoveAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        RemoveFromContainer();
    }
}
```

## 📝 스킬 구현 체크리스트

### ✅ 스킬 구현 전 확인사항
1. **스킬 타입 결정**: 데미지/버프/디버프/CC/힐링?
2. **상속 클래스 선택**: `EffectCodeCharacterBase` / `EffectCodeBuffBase` 등
3. **스탯 인덱스 정의**: 각 인덱스가 무엇을 의미하는지 명확히
4. **스킬 ID 확인**: 중복되지 않는 고유 ID 사용

### ✅ 구현해야 할 메서드
```csharp
public class EffectCodeSkill1101001 : EffectCodeCharacterBase
{
    private const int CodeId = 1101001;
    
    // 필수: 스킬 타입 정의
    public override EffectCodeType Type => EffectCodeType.Character;
    
    // 필수: 스킬 초기화 (실행 로직)
    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        // 스킬 로직 구현
    }
    
    // 선택: 스킬 병합 (스택, 갱신 등)
    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        // 병합 로직 구현
    }
    
    // 선택: 제거 전 정리
    public override void OnPreRemoved()
    {
        // 정리 로직 구현
        base.OnPreRemoved();
    }
}
```

## 🚨 자주 하는 실수들

### ❌ 잘못된 예시
```csharp
// 1. CodeId를 const로 선언하지 않음
public int CodeId = 1101001; // ❌

// 2. Initialize에서 base.Initialize() 호출 안함
public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
{
    // base.Initialize(codeInfo, container, source); // ❌
    // 스킬 로직만 구현
}

// 3. 스탯 인덱스 범위 체크 안함
float damage = codeInfo.GetCodeStatToFloat(10); // ❌ (인덱스 10은 없음)
```

### ✅ 올바른 예시
```csharp
// 1. CodeId를 const로 선언
private const int CodeId = 1101001; // ✅

// 2. base.Initialize() 먼저 호출
public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
{
    base.Initialize(codeInfo, container, source); // ✅
    // 스킬 로직 구현
}

// 3. 스탯 인덱스 범위 체크
if (codeInfo.HasCodeStat(0)) // ✅
{
    float damage = codeInfo.GetCodeStatToFloat(0);
}
```

## 🔍 디버깅 팁

### 스킬이 실행되지 않을 때
1. **CodeId 확인**: `EffectCodePoolManager`에 등록되어 있는지
2. **Initialize 호출 확인**: `base.Initialize()` 호출했는지
3. **스탯 데이터 확인**: `codeInfo.GetStats()`로 데이터 확인
4. **우선순위 확인**: `priority` 값이 올바른지

### 스킬이 중복 실행될 때
1. **Merge 로직 확인**: 같은 스킬이 이미 있는지 체크
2. **IsRemoveWithSource 확인**: 소스 제거 시 함께 제거되는지
3. **컨테이너 상태 확인**: 스킬이 여러 번 추가되었는지

## 📚 참고 자료

### 기존 스킬 코드 참고
- `EffectCodeSkill1101011.cs`: 기본 데미지 스킬
- `EffectCodeSkill1101031.cs`: 복합 효과 스킬
- `EffectCodeBuffDefUp.cs`: 버프 스킬
- `EffectCodeCrowdControlStun.cs`: CC 스킬

### 유용한 헬퍼 메서드
```csharp
// 범위 내 타겟 찾기
var targets = FindTargetsInRange(range);

// 아군 찾기
var allies = FindAlliesInRange(range);

// 데미지 적용
target.TakeDamage(damage, DamageType.Fire);

// 힐링 적용
ally.Heal(healAmount);

// 이펙트 재생
PlayEffect(effectName, position);
```

이제 스킬 명세서를 받으면 이 가이드대로 구현하면 됩니다! 🎯
