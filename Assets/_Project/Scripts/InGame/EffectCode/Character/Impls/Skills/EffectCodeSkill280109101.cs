using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 라플라스마녀 스페셜2
/// 대상: 5x5 십자 범위
// 효과: 범위 내에 {0}%만큼 피해를 입힌다.
/// </summary>
[UseEffectCodeIds(280109101)]
public partial class EffectCodeSkill280109101 : EffectCodeCharacterBase
{
    private bool _isReadyToActivate;
    private SkillActive _specSkill;
    private float _damageRate;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);

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
        var targetCharacterList = InGameObjectManager.Instance.GetCharacterListSortedByDistanceDescending(owner, false);
        if (targetCharacterList.Count == 0)
            return;

        CharacterController targetCharacter = null;

        foreach (var player in targetCharacterList)
        {
            if (player.IsAlive && player.CurrentTile != null)
            {
                targetCharacter = player;
                break;
            }
        }

        if (targetCharacter == null)
            return;

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(targetCharacter.CurrentTile, 1);

        foreach (var tile in inGameTiles)
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);

        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], tile.View.CachedTr.position);
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                var damageValue = owner.SpecCharacter.atk_type is AtkType.AD ? owner.AD : owner.AP;
                var damage = owner.CalculateDamageAmount(damageValue * _damageRate, 0, tile.OccupiedCharacter, codeId, true);
                tile.OccupiedCharacter.GetDamaged(damage, owner);
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

    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }
}//280109101
