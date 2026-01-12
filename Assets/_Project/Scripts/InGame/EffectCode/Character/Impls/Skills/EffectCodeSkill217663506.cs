using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
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
        
        // InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
        //     owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner == null)
            return;

        // 체력이 가장 낮은 적 3명 찾기 및 순차 공격
        ExecuteSlashSequence().Forget();
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

    private async UniTask ExecuteSlashSequence()
    {
        if (!IsOwnerValid())
            return;

        var targets = GetLowestHpTargets(TARGET_COUNT);
        if (targets.Count == 0)
        {
            ApplyAvoidProbBuff();
            return;
        }

        int successfulAttacks = 0;
        for (int i = 0; i < TARGET_COUNT; i++)
        {
            if (!IsOwnerValid())
                break;

            // 공격 시도 (반드시 성공해야 함)
            if (TryExecuteAttackGuaranteed(targets, i, ref successfulAttacks))
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
            }
        }

        if (successfulAttacks > 0)
        {
            ApplyAvoidProbBuff();
        }
    }

    private bool IsOwnerValid()
    {
        return owner != null && owner.IsAlive && owner.CurrentTile != null;
    }

    private bool TryExecuteAttackGuaranteed(List<CharacterController> targets, int attackIndex, ref int successfulAttacks)
    {
        if (!IsOwnerValid())
            return false;

        // 타겟 리스트 갱신 (죽은 타겟 제거)
        targets = RefreshTargets(targets);
        if (targets.Count == 0)
            return false;

        // 이미 시도한 타겟 추적
        var triedTargets = new HashSet<CharacterController>();
        const int MAX_ATTEMPTS = TARGET_COUNT * 3;

        // 시작 인덱스 계산
        int startIndex = targets.Count > 0 ? attackIndex % targets.Count : 0;

        // 모든 타겟을 순회하면서 공격 가능한 타겟 찾기
        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            if (!IsOwnerValid())
                return false;

            // 타겟 리스트 갱신 (죽은 타겟 제거)
            if (attempt > 0 && attempt % 5 == 0) // 5번 시도마다 타겟 리스트 갱신
            {
                targets = RefreshTargets(targets);
                if (targets.Count == 0)
                    break;
                triedTargets.Clear(); // 리스트가 갱신되면 시도 기록 초기화
            }

            // 순환 인덱스로 타겟 선택
            int currentIndex = (startIndex + attempt) % targets.Count;
            var target = targets[currentIndex];

            // 이미 시도한 타겟이면 스킵
            if (triedTargets.Contains(target))
            {
                // 모든 타겟을 시도했는지 확인
                if (triedTargets.Count >= targets.Count)
                    break;
                continue;
            }

            triedTargets.Add(target);

            if (!IsTargetValid(target))
                continue;

            // 8방향(인접 타일)에서 이동 가능한 타일 찾기
            var targetTile = GetTileIn8Directions(target.CurrentTile);
            if (targetTile != null)
            {
                // 공격 실행
                MoveToTile(targetTile, target);
                PlaySlashVfx(target);
                ApplyDamage(target);
                successfulAttacks++;
                return true;
            }
        }

        // 모든 타겟을 시도해도 타일을 찾지 못했으면, 첫 번째 유효한 타겟에게 현재 위치에서 공격
        targets = RefreshTargets(targets);
        for (int i = 0; i < targets.Count; i++)
        {
            var fallbackTarget = targets[i];
            if (IsTargetValid(fallbackTarget))
            {
                // 현재 위치에서 공격 (이동 없이)
                PlaySlashVfx(fallbackTarget);
                ApplyDamage(fallbackTarget);
                successfulAttacks++;
                return true;
            }
        }

        return false;
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

    private CharacterController GetTargetByIndex(List<CharacterController> targets, int index)
    {
        if (targets == null || targets.Count == 0)
            return null;

        int targetIndex = index % targets.Count;
        return targets[targetIndex];
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
        }

        owner.Target = target;
    }

    private void PlaySlashVfx(CharacterController target)
    {
        if (_specSkill?.skill_vfxs == null || _specSkill.skill_vfxs.Length == 0)
            return;

        var targetView = target.GetCharacterView();
        if (targetView?.CachedTr != null)
        {
            InGameVfxManager.Instance?.AddInGameVfx(_specSkill.skill_vfxs[0], targetView.CachedTr.position);
        }
    }

    private void ApplyDamage(CharacterController target)
    {
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
    
    public override void OnSkillAnimationEnd()
    {
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
