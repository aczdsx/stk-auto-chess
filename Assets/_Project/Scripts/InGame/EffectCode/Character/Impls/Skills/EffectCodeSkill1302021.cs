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
/// 메이
// "범위 : 자기 중심 십자 범위
// 대미지 : 주변 적에게 공격력 {0}%의 대미지를 주며 넉백 시킨다.
//     효과 : {1}초 동안 자기에게 {2}%의 방어력 증가 버프를 건다. 
//     추가 효과 : 바람 시너지 배치 인원 1명당 물리 방어력 {3}% 추가 증가
// (최대 3명까지만 적용) "
/// </summary>
[UseEffectCodeIds(1302021)]
public class EffectCodeSkill1302021 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _buffTime;
    private ObfuscatorFloat _buffRate;
    private ObfuscatorFloat _additionalBuffRate;

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
        _buffTime = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _buffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _additionalBuffRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;
        isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _buffTime = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _buffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _additionalBuffRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;
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
        
        var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillRootTransformFollowable);
        var directionTile = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner);
        Vector3 direction = (directionTile[0].View.CachedTr.position - vfx.CachedTr.position).normalized;
        vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);
        
        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByManhattanDistanceInRange(owner.CurrentTile, 1);
        List<int> targetCharacterList = new();
        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile);
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                if (!targetCharacterList.Contains(tile.OccupiedCharacter.CharacterUId))
                {
                    targetCharacterList.Add(tile.OccupiedCharacter.CharacterUId);
                    
                    InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                        tile.OccupiedCharacter.SkillRootTransformFollowable);

                    var damage = owner.PrecalculateDamageAmount(owner.AD * _damageRate, 0, tile.OccupiedCharacter, codeId, true);
                    owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                    tile.OccupiedCharacter.GetDamaged(damage, owner);
        
                    var inGameTile =
                        InGameObjectManager.Instance.InGameGrid.GetTileForKnockBack(owner.CurrentTile, tile.OccupiedCharacter.CurrentTile,
                            1);
                    
                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = 0.3f;
                    eccStats[1] = 0.3f;
                    eccStats[2] = inGameTile.View.ID;

                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.KNOCKBACK, tile.OccupiedCharacter, eccStats, source);
                }
            }
        }

        IsSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
}
