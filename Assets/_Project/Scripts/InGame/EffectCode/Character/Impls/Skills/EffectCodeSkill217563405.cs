using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using PrimeTween;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;
using UnityEngine.Localization.SmartFormat.Utilities;

/// <summary>
/// 마리에
/// 대상: 공격력이 가장 높은 적
/// 재사용 시간: {0}초
/// 효과: 대상의 뒤로 이동하여 {1}회 만큼 공격합니다. 타격 시 마다 마리에의 공격력의 {2}% 만큼 피해를 입힙니다. 
/// 만약 적에게 표식-아라크네가 남아있다면 {3}초간 적의 공격력과 방어력을 {4}%만큼 낮춥니다.
/// </summary>
/// 
[UseEffectCodeIds(217563405)]
public partial class EffectCodeSkill217563405 : EffectCodeCharacterBase
{
    // 스킬 상태
    private bool _isReadyToActivate;
    private SkillActive _specSkill;
    private int _attackCount;
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _debuffDuration;
    private ObfuscatorFloat _debuffRate;
    private CharacterController _targetCharacter;
    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;

        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _attackCount = codeInfo.GetCodeStatToInt(1);
        _damageRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _debuffDuration = codeInfo.GetCodeStatToFloat(3);
        _debuffRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;

        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeElapsedTime = 0f;

        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _attackCount = codeInfo.GetCodeStatToInt(1);
        _damageRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _debuffDuration = codeInfo.GetCodeStatToFloat(3);
        _debuffRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;
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
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner == null)
            return;

        // 이동 가능한 타겟 찾기
        _targetCharacter = FindValidTarget();
        if (_targetCharacter == null)
            return;

        // 타겟의 뒤로 이동할 타일 찾기
        InGameTile targetBackTile = GetTileBehindTarget(owner.CurrentTile, _targetCharacter.CurrentTile);
        if (targetBackTile == null || targetBackTile == _targetCharacter.CurrentTile)
        {
            // 이동 불가능한 경우 스킬 취소
            return;
        }

        InGameVfxNameType skillFxType = (owner.AllianceType == AllianceType.Player)
            ? InGameVfxNameType.fx_common_assassin_awful
            : InGameVfxNameType.fx_common_assassin_enemy;

        // 이동 전 자리에 이펙트 추가
        InGameVfxManager.Instance.AddInGameVfx(skillFxType, owner.GetCharacterView().CachedTr.position);

        // 타일 변경 및 위치 설정
        owner.ChangeOccupiedTile(targetBackTile);
        owner.Position3D = targetBackTile.View.Position;
        owner.GetCharacterView().CachedTr.localPosition = targetBackTile.View.Position;
        owner.GetCharacterView().LookAt(targetBackTile, _targetCharacter.CurrentTile);
        owner.Target = _targetCharacter;

        // 이동 완료 후 공격 시작
        StartAttackSequence().Forget();
    }

    private CharacterController FindValidTarget()
    {
        if (owner == null || owner.CurrentTile == null)
            return null;
            
        var allTargets = InGameObjectManager.Instance?.GetCharacterListSortedByADDescending(owner.AllianceType, false);
        if (allTargets == null || allTargets.Count == 0)
            return null;
        
        foreach (var target in allTargets)
        {
            if (target == null || !target.IsAlive || target.CurrentTile == null)
                continue;

            // 타겟의 뒤로 이동할 타일 찾기
            InGameTile targetBackTile = GetTileBehindTarget(owner.CurrentTile, target.CurrentTile);
            
            // 이동 가능한 타일이 있고 타겟 타일이 아니면 유효한 타겟
            if (targetBackTile != null && targetBackTile != target.CurrentTile && targetBackTile.OccupiedCharacter == null)
            {
                return target;
            }
        }

        return null; // 이동 가능한 타겟이 없음
    }

    private InGameTile GetTileBehindTarget(InGameTile attackerTile, InGameTile targetTile)
    {
        if (attackerTile == null || targetTile == null)
            return null;
            
        var grid = InGameObjectManager.Instance.InGameGrid;
        if (grid == null)
            return null;
        
        // 바로 뒤 타일(1칸) 찾기 - 파라미터 순서: (attackerTile, targetTile, count)
        InGameTile behindTile = grid.GetTileForKnockBack(attackerTile, targetTile, 1);
        
        // GetTileForKnockBack은 차단되면 targetTile을 반환할 수 있으므로 명시적으로 체크
        // 바로 뒤 타일이 유효하고 타겟 타일이 아니고 비어있으면 반환
        if (behindTile != null && behindTile != targetTile && behindTile.OccupiedCharacter == null)
        {
            return behindTile;
        }
        
        // 바로 뒤 타일이 없거나 이동 불가능하면 근처 빈 타일 찾기
        for (int distance = 1; distance <= 3; distance++)
        {
            var nearbyTiles = grid.GetTileListByManhattanDistance(targetTile, distance);
            if (nearbyTiles == null || nearbyTiles.Count == 0)
                continue;
                
            var emptyTile = nearbyTiles.FirstOrDefault(t => t != null && t != targetTile && t.OccupiedCharacter == null);
            
            if (emptyTile != null)
            {
                return emptyTile;
            }
        }
        
        // 근처 타일도 없으면 null 반환 (이동 불가능)
        return null;
    }

    private async UniTask StartAttackSequence()
    {
        if (owner == null || _targetCharacter == null || !_targetCharacter.IsAlive)
            return;

        // 공격 횟수만큼 공격
        for (int i = 0; i < _attackCount; i++)
        {
            if (owner == null || _targetCharacter == null || !_targetCharacter.IsAlive)
                break;

            // 피해 계산 및 적용
            var damage = owner.CalculateDamageAmount(owner.AD * _damageRate, 0, _targetCharacter, codeId, true);
            damage.damageAmount = Math.Floor(damage.damageAmount.Value);
            _targetCharacter.GetDamaged(damage, owner);

            // 표식-아라크네 체크 및 디버프 적용
            //CheckAndApplyDebuff();

            // 공격 간격
            if (i < _attackCount - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.3f));
            }
        }
    }

    private void CheckAndApplyDebuff()
    {
        if (_targetCharacter == null || owner == null)
            return;

        // 표식-아라크네 체크 (EffectCodeNameType을 확인해야 함)
        var ecc = _targetCharacter.GetEffectCodeContainer();
        // TODO: 표식-아라크네 이펙트 코드 타입 확인 필요
        // if (ecc.GetEffectCode((int)EffectCodeNameType.MARK_ARACHNE) != null)
        {
            // 디버프 적용
            Span<double> debuffStats = stackalloc double[3];
            debuffStats[0] = codeId;
            debuffStats[1] = _debuffDuration;
            debuffStats[2] = _debuffRate;

            // 공격력 감소 디버프
            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_AD_PERCENT_DOWN, _targetCharacter, debuffStats, source);

            debuffStats[2] = _debuffRate * 100f;

            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_DEF_PERCENT_DOWN, _targetCharacter, debuffStats, source);
        }
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
