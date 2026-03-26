# Skill FaceTarget Design

## 목적

스킬 시전 시 캐스터가 타겟 방향을 바라볼지 여부를 SkillTargetType 기반으로 자동 분류. Self/Ally 타겟 스킬은 방향 유지, 적 타겟 스킬은 적 방향 전환.

## 현재 문제

`SkillSystem.TryCast()`에서 모든 스킬이 `unit.CurrentTargetId = targetId`를 설정. 뷰에서 CurrentTargetId 위치를 바라보므로:
- Self 타겟: delta=0 → 우연히 방향 유지 (불안정)
- LowestHPAlly: 아군(후방) 방향을 바라봄 (부자연스러움)

## 변경

### 1. SkillParams에 FaceTarget 필드 추가

```csharp
public bool FaceTarget;
```

### 2. SkillSpecAdapter.BuildParams()에서 자동 설정

```csharp
p.FaceTarget = p.TargetType != SkillTargetType.Self
    && p.TargetType != SkillTargetType.LowestHPAlly;
```

### 3. SkillSystem.TryCast()에서 조건부 CurrentTargetId 설정

기존: `unit.CurrentTargetId = targetId` (무조건)
변경: `if (skillParams.FaceTarget) unit.CurrentTargetId = targetId;`

## 분류표

| TargetType | FaceTarget | 동작 |
|-----------|-----------|------|
| NearestEnemy | true | 적 방향 |
| FarthestEnemy | true | 적 방향 |
| HighestAttackEnemy | true | 적 방향 |
| LowestHPEnemy | true | 적 방향 |
| BestAoETarget | true | 적 방향 |
| Self | false | 방향 유지 |
| LowestHPAlly | false | 방향 유지 |

## 제약조건

- 상속 구조 변경 없음 (퀀텀 대비)
- struct 데이터 기반
- 뷰 레이어 변경 없음
