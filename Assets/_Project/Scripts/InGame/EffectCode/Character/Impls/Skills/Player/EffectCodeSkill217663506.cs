using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 시라유키
/// 대상: 체력이 가장 낮은 적
/// 재사용 시간: {0}초
/// 효과: {1}초 동안 지정 불가 상태가 되며, 3명의 대상에게 순차적으로 이동하여 참격을 날립니다.  
/// 각 타격 마다 공격력의 {2}% 피해를 입힙니다. 참격이 모두 완료될 경우 지정 불가 상태가 해지되며 {3}초간 회피율이 {4}% 상승합니다.
/// </summary>

[UseEffectCodeIds(217663506)]
public partial class EffectCodeSkill217663506 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _targetImpossibleDuration;
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _avoidProbIncreaseDuration;
    private ObfuscatorFloat _avoidProbIncreaseRate;

    // 스킬 상태
    private bool _isReadyToActivate;
    private SkillActive _specSkill;
    private const int TARGET_COUNT = 3; // 공격할 대상 수
    private List<CharacterController> _targets; // 공격할 타겟 리스트
    private InGameVfx _slashVfx; // 슬래시 VFX 인스턴스

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;

        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _targetImpossibleDuration = codeInfo.GetCodeStatToFloat(1);
        _damageRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _avoidProbIncreaseDuration = codeInfo.GetCodeStatToFloat(3);
        _avoidProbIncreaseRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;


        _isReadyToActivate = false;
        IsSkillActivated = false;
        _slashVfx = null;

        var skillDataList = SpecDataManager.Instance.GetSkillDataList(codeId);
        _specSkill = skillDataList != null && skillDataList.Count > 0 ? skillDataList[0] : null;
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _targetImpossibleDuration = codeInfo.GetCodeStatToFloat(1);
        _damageRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _avoidProbIncreaseDuration = codeInfo.GetCodeStatToFloat(3);
        _avoidProbIncreaseRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;
    }

    public override void OnCooltime(float dt)
    {
        if (_isReadyToActivate || IsSkillActivated)
            return;
        CoolTimeElapsedTime += dt;
        if (CoolTimeElapsedTime >= CoolTimeDurationTime)
        {
            _isReadyToActivate = true;
        }
    }

    public override bool IsReadyToActivate()
    {
        return _isReadyToActivate;
    }

    public override void Activate()
    {
        base.Activate();

        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);

        // 타겟 불가능 상태 적용
        ApplyTargetImpossible();
        
        // 공격할 타겟 리스트 미리 저장
        _targets = GetLowestHpTargets(TARGET_COUNT);
        
        // VFX 초기화
        _slashVfx = null;
        
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner == null)
            return;

        // 첫 번째 공격에서 VFX 생성
        if (executeIndex == 0)
        {
            CreateSlashVfx();
        }

        // executeIndex에 따라 해당 타겟으로 이동하여 공격
        if (executeIndex < TARGET_COUNT)
        {
            ExecuteSlashAttack(executeIndex);
        }

        // 마지막 공격 후 버프 적용 및 VFX 삭제
        if (executeIndex == totalLength)
        {
            ApplyAvoidProbBuff();
            // RemoveSlashVfx();
        }
    }

    private void ApplyTargetImpossible()
    {
        if (owner == null)
            return;

        Span<double> eccStats = stackalloc double[3];
        eccStats[0] = codeId;
        eccStats[1] = _targetImpossibleDuration;
        eccStats[2] = 1.0f;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_TARGET_IMPOSSIBLE, owner, eccStats, source);
    }

    private List<CharacterController> GetLowestHpTargets(int count)
    {
        if (owner == null)
            return new List<CharacterController>();

        var allTargets = InGameObjectManager.Instance?.GetCharacterListSortedByHPRateDescending(owner.AllianceType, false);
        if (allTargets == null || allTargets.Count == 0)
            return new List<CharacterController>();

        var result = new List<CharacterController>();

        // 정렬된 리스트에서 유효한 타겟만 필터링하고 count개만 가져오기
        for (int i = 0; i < allTargets.Count && result.Count < count; i++)
        {
            var target = allTargets[i];
            if (target == null || !target.IsAlive)
                continue;

            result.Add(target);
        }

        return result;
    }

    private void ExecuteSlashAttack(int attackIndex)
    {
        if (!IsOwnerValid())
            return;

        // 타겟 리스트 갱신
        _targets = RefreshTargets(_targets);
        if (_targets.Count == 0)
        {
            // 타겟이 없으면 새로 가져오기
            _targets = GetLowestHpTargets(TARGET_COUNT);
            if (_targets.Count == 0)
                return;
        }

        // attackIndex에 해당하는 타겟 선택
        int targetIndex = attackIndex < _targets.Count ? attackIndex : attackIndex % _targets.Count;
        var target = _targets[targetIndex];

        // 유효한 타겟이 아니면 다른 타겟 찾기
        if (!IsTargetValid(target))
        {
            // 유효한 타겟 찾기
            for (int i = 0; i < _targets.Count; i++)
            {
                int checkIndex = (targetIndex + i) % _targets.Count;
                var checkTarget = _targets[checkIndex];
                if (IsTargetValid(checkTarget))
                {
                    target = checkTarget;
                    break;
                }
            }

            if (!IsTargetValid(target))
                return;
        }

        // 8방향(인접 타일)에서 이동 가능한 타일 찾기
        var targetTile = GetTileIn8Directions(target.CurrentTile);
        if (targetTile != null)
        {
            // 이동 및 공격 실행
            MoveToTile(targetTile, target);
            ApplyDamage(target);
        }
        else
        {
            // 이동할 타일이 없으면 현재 위치에서 공격
            ApplyDamage(target);
        }
    }

    private bool IsOwnerValid()
    {
        return owner != null && owner.IsAlive && owner.CurrentTile != null;
    }


    private List<CharacterController> RefreshTargets(List<CharacterController> currentTargets)
    {
        // 현재 타겟 리스트에서 유효한 타겟만 필터링
        var validTargets = new List<CharacterController>();
        for (int i = 0; i < currentTargets.Count; i++)
        {
            var target = currentTargets[i];
            if (IsTargetValid(target))
            {
                validTargets.Add(target);
            }
        }

        // 유효한 타겟이 부족하면 새로 가져오기
        if (validTargets.Count < TARGET_COUNT)
        {
            var newTargets = GetLowestHpTargets(TARGET_COUNT);
            if (newTargets.Count > 0)
            {
                return newTargets;
            }
        }

        return validTargets.Count > 0 ? validTargets : currentTargets;
    }


    private bool IsTargetValid(CharacterController target)
    {
        return target != null && target.IsAlive && target.CurrentTile != null;
    }

    private void MoveToTile(InGameTile targetTile, CharacterController target)
    {
        owner.ChangeOccupiedTile(targetTile);
        owner.Position3D = targetTile.View.Position;

        var characterView = owner.GetCharacterView();
        if (characterView?.CachedTr != null)
        {
            characterView.CachedTr.localPosition = targetTile.View.Position;
            characterView.LookAt(targetTile, target.CurrentTile);
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, target.CurrentTile);
        }

        owner.Target = target;
    }

    private void CreateSlashVfx()
    {
        if (_specSkill?.skill_vfxs == null || _specSkill.skill_vfxs.Length == 0)
            return;

        if (_slashVfx == null)
        {
            _slashVfx = InGameVfxManager.Instance?.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillMiddleFXTransformFollowable);
        }
    }

    private void ApplyDamage(CharacterController target)
    {
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], target.SkillMiddleFXTransformFollowable.GetPosition());
        var damage = owner.CalculateDamageAmount(owner.AD * _damageRate, 0, target, codeId, true);
        target.GetDamaged(damage, owner);
    }

    private InGameTile GetTileIn8Directions(InGameTile targetTile)
    {
        if (targetTile == null)
            return null;

        var grid = InGameObjectManager.Instance?.InGameGrid;
        if (grid == null)
            return null;

        // 8방향(인접 타일)만 탐색 (맨하탄 거리 1)
        var adjacentTiles = grid.GetTileListByManhattanDistance(targetTile, 1);
        if (adjacentTiles == null)
            return null;

        // 타겟 타일이 아니고 비어있는 타일 찾기
        for (int i = 0; i < adjacentTiles.Count; i++)
        {
            var tile = adjacentTiles[i];
            if (tile != null && tile != targetTile && tile.OccupiedCharacter == null)
            {
                return tile;
            }
        }

        return null;
    }

    private void ApplyAvoidProbBuff()
    {
        if (owner == null)
            return;

        Span<double> buffStats = stackalloc double[3];
        buffStats[0] = codeId;
        buffStats[1] = _avoidProbIncreaseDuration;
        buffStats[2] = _avoidProbIncreaseRate;
        
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.AVOID_PROB_PERCENT_UP, owner, buffStats, source);
    }

    private void RemoveSlashVfx()
    {
        if (_slashVfx != null)
        {
            _slashVfx.Remove();
            _slashVfx = null;
        }
    }
    
    public override void OnSkillAnimationEnd()
    {
        RemoveSlashVfx();
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }
}
