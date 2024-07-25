using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 미노
// 타겟 범위 : 맵 전체 
// 타겟 : 현재 체력이 가장 낮은 적 3명 
// 대미지 : 적에게 유탄을 발사해 공격력 {0}%의 대미지를 준다. 유탄을 적을 타겟한 후 폭발해 주변에 {1}%의 추가 피해를 준다. 
// 추가 효과 : 범위 내 적이 1명인 경우 적에게 모든 포탄을 발사한다. 
/// </summary>
[UseEffectCodeIds(1404031)]
public class EffectCodeSkill1404031 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _additionalDamageRate;

    private bool _isReadyToActivate;

    private List<InGameVfx> _vfxList = new List<InGameVfx>();

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _additionalDamageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _additionalDamageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
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
        // TODO: Target Check
        _vfxList.Clear();
        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);

        var vfxProjectile = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.CurrentTile.View.CachedTr.position);
        var movement = InGameVfxMovementPool.Get<InGameVfxMovementBezier>();
        
        AllianceType targetAllianceType;
        if (owner.AllianceType == AllianceType.Player)
            targetAllianceType = AllianceType.Enemy;
        else
            targetAllianceType = AllianceType.Player;

        var inGameCharacterListSortedByHpRate =
            InGameObjectManager.Instance.GetCharacterListSortedByHpRate(targetAllianceType, false);
        List<CharacterController> targetCharacters = new();
        
        if (inGameCharacterListSortedByHpRate.Count > 0)
        {
            for (int i = 0; i < 3; i++)
            {
                if (inGameCharacterListSortedByHpRate.Count > i)
                {
                    targetCharacters.Add(inGameCharacterListSortedByHpRate[i]);
                }
                else
                {
                    targetCharacters.Add(inGameCharacterListSortedByHpRate[0]);
                }
            }
        }

        foreach (var targetCharacter in targetCharacters)
        {
            var targetTile = targetCharacter.CurrentTile;
            var tileFx = InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,
                targetTile.View.CachedTr.position);

            Vector3 direction = (targetTile.View.CachedTr.position - vfxProjectile.CachedTr.position).normalized;
            vfxProjectile.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

            movement.SetData(vfxProjectile.CachedTr.position, targetTile.View.CachedTr.position, 15);
            vfxProjectile.Initialize(false, movement);
            vfxProjectile.OnCollisionWithTile += OnCollision2DEnter;
        }

        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
    }

    private void OnCollision2DEnter(InGameVfx.CollisionType type, InGameTile tile, InGameVfx vfx)
    {
        if (tile.OccupiedCharacter == null)
            return;

        if (tile.OccupiedCharacter.AllianceType == AllianceType.Wall)
            return;

        if (_vfxList.Contains(vfx))
            return;

        if(owner.AllianceType == tile.OccupiedCharacter.AllianceType)
            return;

        if (tile.OccupiedCharacter != null && tile.OccupiedCharacter.AllianceType != owner.AllianceType)
        {
            if (tile.OccupiedCharacter.AllianceType != AllianceType.Wall)
            {
                var tileFx = InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,
                    tile.View.CachedTr.position);
            
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                    tile.OccupiedCharacter.SkillRootTransformFollowable);

                var damage = owner.PrecalculateDamageAmount(owner.AD * _damageRate, 0, tile.OccupiedCharacter, codeId, true);
                owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                tile.OccupiedCharacter.GetDamaged(damage, owner);
            }
        }

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeX(owner.CurrentTile, 1);
        foreach (var inGameTile in inGameTiles)
        {
            if (inGameTile.OccupiedCharacter == null)
                continue;

            if (inGameTile.OccupiedCharacter.AllianceType == AllianceType.Wall)
                continue;

            if (inGameTile.OccupiedCharacter.AllianceType == owner.AllianceType)
                continue;

            var tileFx = InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,
                inGameTile.View.CachedTr.position);
            
            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                tile.OccupiedCharacter.SkillRootTransformFollowable);

            var damage = owner.PrecalculateDamageAmount(owner.AD * _additionalDamageRate, 0, tile.OccupiedCharacter, codeId, true);
            owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
            tile.OccupiedCharacter.GetDamaged(damage, owner);
        }
        
        _vfxList.Add(vfx);
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
}
