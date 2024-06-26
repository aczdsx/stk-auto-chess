using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 멘샤
// 범위 : 멘샤와 동일한 열
// 효과 : 아군에게 {0}초 동안 멘샤 공격력 {1}%의 실드를 부여한다.
/// </summary>
[UseEffectCodeIds(1302011)]
public class EffectCodeSkill1302011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _shieldRate;

    private bool _isReadyToActivate;

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        CoolTimeElapsedTime = codeInfo.GetCodeStatToFloat(1);
        _shieldRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        CoolTimeElapsedTime = codeInfo.GetCodeStatToFloat(1);
        _shieldRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
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

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByRow(owner.CurrentTile);
        if (inGameTiles != null)
        {
            foreach (var tile in inGameTiles)
            {
                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile.View.CachedTr.position);
                if (tile.OccupiedCharacter != null)
                {
                    if (tile.OccupiedCharacter.AllianceType == owner.AllianceType)
                    {
                        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0],
                            tile.OccupiedCharacter.SkillRootTransformFollowable);

                        var shieldAmount = owner.PrecalculateDamageAmount(owner.AD * _shieldRate, 0, tile.OccupiedCharacter,
                            codeId, true);

                        Span<double> eccStats = stackalloc double[2];
                        eccStats.Clear();
                        eccStats[0] = CoolTimeElapsedTime;
                        eccStats[1] = shieldAmount.damageAmount;

                        var effectCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.SHIELD, 0, eccStats);
                        tile.OccupiedCharacter.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, owner);
                    }
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
        // _vfx.OnCollisionWithTile -= OnCollision2DEnter;
    }
}
