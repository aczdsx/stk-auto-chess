using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 테토라
// "범위 : 가장 가까이에 위치한 적 1명 
// 효과 : 대검을 휘둘러 적 1명에게 대미지를 주고 4칸 넉백시킨다.
//     대미지 : 테토라 공격력 {0}% + 마법 방어력 비례 추가 대미지
// 개발용 대미지 계산식 : 테토라 공격력*{0}*(1+마법 방어력/{1})
// 특수 효과 : 넉백된 적이 구조물 또는 캐릭터에 부딪힐 시, 3*3범위로 {2}초 동안 스턴을 일으키며
// 공격력 {3}%의 대미지를 준다. 
//
//     개발 참고 사항 
//     피격된 적이 아군에게 부딪힐 경우, 적군은 충돌 중지 + 스턴 적용 
//     아군에게는 대미지와 스턴이 적용되지 않음. "
/// </summary>
[UseEffectCodeIds(1401021)]
public class EffectCodeSkill1401021 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _resRate;
    private ObfuscatorFloat _stunTime;
    private ObfuscatorFloat _afterDamageRate;

    private bool isReadyToActivate;

    private SpecSkill _specSkill;

    private CharacterController _targetCharacter;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _resRate = codeInfo.GetCodeStatToFloat(2);
        _stunTime = codeInfo.GetCodeStatToFloat(3);
        _afterDamageRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;
        isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _resRate = codeInfo.GetCodeStatToFloat(2);
        _stunTime = codeInfo.GetCodeStatToFloat(3);
        _afterDamageRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;
    }

    public override void OnUpdate(float dt)
    {
        if (!IsSkillActivated)
        {
            return;
        }

        // target check
        if (false)
        {
            owner.AddNextState<CharacterStateIdle>();
            CoolTimeElapsedTime = CoolTimeDurationTime;
        }
    }

    public override void OnCooltime(float dt)
    {
        if (isReadyToActivate || IsSkillActivated)
            return;
        CoolTimeElapsedTime += dt;
        if (CoolTimeElapsedTime >= CoolTimeDurationTime)
        {
            isReadyToActivate = true;
        }
    }

    public override bool IsReadyToActivate()
    {
        return isReadyToActivate;
    }

    public override void Activate()
    {
        base.Activate();

        var isInRange = InGameObjectManager.Instance.IsInRange(owner, owner.Target);
        if (!isInRange)
        {
            if (owner.Target != null)
            {
                InGameTile bestTile = InGameObjectManager.Instance.GetNextMovableTile(owner.CurrentTile,
                    owner.Target.CurrentTile);
                owner.MoveTile(bestTile);
            }
            return;
        }

        isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);

        _targetCharacter = owner.Target;
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);

        if (_targetCharacter == null)
            return;

        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
            _targetCharacter.SkillRootTransformFollowable);

        var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillRootTransformFollowable);
        var directionTile = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner);
        Vector3 direction = (directionTile[0].View.CachedTr.position - vfx.CachedTr.position).normalized;
        vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);
        
        float damageRate = (float)(owner.AD * _damageRate) * (1.0f + (float)owner.RES / _resRate);
        var damage = owner.PrecalculateDamageAmount(damageRate, 0, _targetCharacter, codeId, true);
        owner.PostCalculateDamageAmount(ref damage, _targetCharacter);
        _targetCharacter.GetDamaged(damage, owner);
        
        var inGameTile =
            InGameObjectManager.Instance.InGameGrid.GetTileForKnockBack(owner.CurrentTile, _targetCharacter.CurrentTile,
                4);

        float knockBackTime = 0.3f;
        Span<double> eccStats = stackalloc double[3];
        eccStats.Clear();
        eccStats[0] = knockBackTime;
        eccStats[1] = 0.2f;
        eccStats[2] = inGameTile.View.ID;
        
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.KNOCKBACK, _targetCharacter, eccStats, source);
        
        // 스턴
        ApplyStunEffectAsync(inGameTile, knockBackTime).Forget();

        IsSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
    
    private async UniTaskVoid ApplyStunEffectAsync(InGameTile inGameTile, float second)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second));

        var tileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(inGameTile, 1);
        var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], inGameTile.View.Position);
        List<int> targetCharacterList = new();
        foreach (var tile in tileList)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile);
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                if (!targetCharacterList.Contains(tile.OccupiedCharacter.CharacterUId))
                {
                    targetCharacterList.Add(tile.OccupiedCharacter.CharacterUId);
                    StunCharacter(tile);
                }
            }
        }
    }
    
    
    private void StunCharacter(InGameTile tile)
    {
        float damageRate = (float)(owner.AD * _afterDamageRate) * (1.0f + (float)owner.RES / _resRate);
        var damage = owner.PrecalculateDamageAmount(damageRate, 0, tile.OccupiedCharacter, codeId, true);
        owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
        tile.OccupiedCharacter.GetDamaged(damage, owner);
        
        Span<double> eccStats = stackalloc double[1];
        eccStats.Clear();
        eccStats[0] = _stunTime;
        
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.STUN, tile.OccupiedCharacter, eccStats, source);
    }
}