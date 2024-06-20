using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 유니
// 대상 : 공격력이 가장 높은 아군 2명
// 효과 : 공격력을 {0}초 동안 {1}% 증가시킨다.
/// </summary>
[UseEffectCodeIds(1306011)]
public class EffectCodeSkill1306011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _duration;
    private ObfuscatorFloat _atkUpRate;

    private bool _isReadyToActivate;
    private bool _isSkillActivated;

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _duration = codeInfo.GetCodeStatToFloat(1);
        _atkUpRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;;
        _isReadyToActivate = false;
        _isSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList((int)codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _duration = codeInfo.GetCodeStatToFloat(1);
        _atkUpRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;;
    }

    public override void OnUpdate(float dt)
    {
        if (!_isSkillActivated)
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
        if (_isReadyToActivate || _isSkillActivated)
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
        _isSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        // 나한테 붙은 vfx
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillRootTransformFollowable);

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByAllianceType(owner.AllianceType, 2);
        if (inGameTiles != null)
        {
            foreach (var tile in inGameTiles)
            {
                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile.View.CachedTr.position);
                InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], tile.OccupiedCharacter.SkillRootTransformFollowable);

                Span<double> eccStats = stackalloc double[3];
                eccStats.Clear();
                eccStats[0] = codeId;
                eccStats[1] = _duration;
                eccStats[2] = _atkUpRate;

                long effectCodeID = (long)EffectCodeNameType.BUFF_AD_PERCENT_UP;
                var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, eccStats);
                tile.OccupiedCharacter.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, owner);
            }
        }

        _isSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        CoolTimeElapsedTime = 0;
        _isSkillActivated = false;
    }
}
