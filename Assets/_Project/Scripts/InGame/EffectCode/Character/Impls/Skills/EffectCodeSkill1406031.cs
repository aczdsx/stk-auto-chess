using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 아란
// "타겟 : 현재 체력이 가장 낮은 아군 1명
// 효과 : 아란 공격력 {0}%만큼 체력을 회복시키고, 물리, 마법 방어력을 {1}만큼 추가한다. "
/// </summary>
[UseEffectCodeIds(1406031)]
public class EffectCodeSkill1406031 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _healRate;
    private ObfuscatorFloat _buffTime;
    private ObfuscatorFloat _buffRate;
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
        _buffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
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
        _buffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
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
        //
        // var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByAllianceType(owner.AllianceType, 10);
        // InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0],
        //     owner.GetCharacterView().CachedTr.position);
        // foreach (var tile in inGameTiles)
        // {
        //     InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,
        //         tile.View.CachedTr.position);
        // }
        //
        // {
        //     Span<double> buffStats = stackalloc double[3];
        //     buffStats.Clear();
        //     buffStats[0] = codeId;
        //     buffStats[1] = 999f;
        //     buffStats[2] = _statValue;
        //     var effectCodeID = new EffectCodeInfo((long)EffectCodeNameType.BUFF_RES_PERCENT_UP, 0, buffStats);
        //     owner.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeID, source);
        // }
        // {
        //     Span<double> buffStats = stackalloc double[3];
        //     buffStats.Clear();
        //     buffStats[0] = codeId;
        //     buffStats[1] = 999f;
        //     buffStats[2] = _statValue;
        //     var effectCodeID = new EffectCodeInfo((long)EffectCodeNameType.BUFF_RES_PERCENT_UP, 0, buffStats);
        //     owner.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeID, source);
        // }
        

        IsSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
}
