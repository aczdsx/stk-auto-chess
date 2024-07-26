using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 엔키
// 대상 : 아군 전체
// 효과 : 아군 전체의 체력을 엔키 공격력 {0}%만큼 회복시키고 {1}초 동안 공격 속도를 {2}% 만큼 증가시킨다.
/// </summary>
[UseEffectCodeIds(1406011)]
public class EffectCodeSkill1406011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _healRate;
    private ObfuscatorFloat _buffTime;
    private ObfuscatorFloat _atkBuffRate;
    private bool _isReadyToActivate;
    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _healRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _buffTime = codeInfo.GetCodeStatToFloat(2);
        _atkBuffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        SkillIndex = 0;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _healRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _buffTime = codeInfo.GetCodeStatToFloat(2);
        _atkBuffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
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
        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByAllianceType(owner.AllianceType, 10);
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0],
            owner.GetCharacterView().CachedTr.position);
        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,
                tile.View.CachedTr.position);
        }

        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile.View.CachedTr.position);
            tile.CheckValidTile(owner.AllianceType, true, () =>
            {
                double damage = owner.PostCalculateHealAmount(_healRate * owner.AP, tile.OccupiedCharacter);
                tile.OccupiedCharacter.GetHealed(damage, owner, codeId, true);

                Span<double> eccStats = stackalloc double[3];
                eccStats.Clear();
                eccStats[0] = codeId;
                eccStats[1] = _buffTime;
                eccStats[2] = _atkBuffRate;
                
                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_ATK_SPEED_UP, tile.OccupiedCharacter, eccStats, source);
            });
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
