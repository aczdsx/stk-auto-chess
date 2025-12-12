using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 블린
// 범위 : 가장 가까운 적 중심 3x3
// 대미지 : 화염 빔을 소환해, 공격력 {0}%의 대미지를 준다.
//     특수 효과 : 공격 범위에 잔열이 남아, {1}초 동안 초당 {2}%의 대미지를 준다.
/// </summary>
[UseEffectCodeIds(1142431021)]
public partial class EffectCodeSkill1142431021 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _durationTime;
    private ObfuscatorFloat _dotDamageRate;
    private ObfuscatorFloat _dotDamageElapsedTime;

    private float elapsedTime = 0f;
    private bool _isReadyToActivate;

    private SkillActive _specSkill;
    private List<InGameTile> _effectTiles = new List<InGameTile>();

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _durationTime = codeInfo.GetCodeStatToFloat(2);
        _dotDamageRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _effectTiles = new List<InGameTile>();
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _durationTime = codeInfo.GetCodeStatToFloat(2);
        _dotDamageRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _effectTiles = new List<InGameTile>();
    }

    public override void OnUpdate(float dt)
    {
        elapsedTime += dt;
        if (elapsedTime >= 1f)
        {
            _dotDamageElapsedTime += elapsedTime;
            foreach (var tile in _effectTiles)
            {
                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
                if (tile.OccupiedCharacter != null)
                {
                    if (tile.OccupiedCharacter.AllianceType != owner.AllianceType)
                    {
                        if (tile.OccupiedCharacter.AllianceType != AllianceType.Wall)
                        {
                            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                                tile.OccupiedCharacter.SkillRootTransformFollowable);
                            
                            var damage = owner.PrecalculateDamageAmount(0, owner.AP * _damageRate,
                                tile.OccupiedCharacter,
                                codeId, true);
                            owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                            tile.OccupiedCharacter.GetDamaged(damage, owner);
                        }
                    }
                }
            }

            if (_dotDamageElapsedTime > _durationTime)
            {
                _effectTiles.Clear();
                _dotDamageElapsedTime = 0;
            }
            elapsedTime -= 1f;
        }

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

        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        _dotDamageElapsedTime = 0;
        _effectTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(owner.Target.CurrentTile, 1);
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.Target.CurrentTile.View.CachedTr.position);
        foreach (var tile in _effectTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
        }

        foreach (var tile in _effectTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                    tile.OccupiedCharacter.SkillRootTransformFollowable);
                
                var damage = owner.PrecalculateDamageAmount(0, owner.AP * _damageRate, tile.OccupiedCharacter,
                    codeId, true);
                owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                tile.OccupiedCharacter.GetDamaged(damage, owner);
            }
            
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], tile.View.CachedTr.position);
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
