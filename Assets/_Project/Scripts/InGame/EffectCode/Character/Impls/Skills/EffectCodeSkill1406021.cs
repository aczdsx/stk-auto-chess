using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 라키유
// "대상 : 물리 또는 마법 방어력이 가장 높은 적의 3*3 범위
// 효과 : 약병을 투하해 {0}초 동안 범위 내에 위치한 적군의 치유 효과를 {1}%, 
// 물리/마법 방어력을 {2}% 감소시킨다."
/// </summary>
[UseEffectCodeIds(1406021)]
public class EffectCodeSkill1406021 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _healRate;
    private ObfuscatorFloat _buffTime;
    private ObfuscatorFloat _buffRate;
    private bool _isReadyToActivate;
    private SpecSkill _specSkill;

    private InGameVfx _vfxObj;
    private InGameTile _targetTile;

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

        _vfxObj = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.CurrentTile.View.CachedTr.position);

        var target = InGameObjectManager.Instance.GetNearestTargetByManhattanDistance(owner);
        if (target != null)
        {
            _targetTile = target.CurrentTile;
            Vector3 direction = (target.CurrentTile.View.CachedTr.position - _vfxObj.CachedTr.position).normalized;
            _vfxObj.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

            var movement = InGameVfxMovementPool.Get<InGameVfxMovementBezier>();
            movement.SetData(_vfxObj.CachedTr.position, target.CurrentTile.View.CachedTr.position, 10);
            _vfxObj.Initialize(false, movement);

            void OnReachedTargetHandler()
            {
                _vfxObj.Remove();
                SkillAction(_targetTile);
            }

            movement.OnReachedTarget += OnReachedTargetHandler;
        }

        IsSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
    
    private void SkillAction(InGameTile tile)
    {
        InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile.View.CachedTr.position);
        tile.CheckValidTile(owner.AllianceType, false, () =>
        {
            var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(owner.CurrentTile, 1);
            foreach (var inGameTile in inGameTiles)
            {
                inGameTile.CheckValidTile(owner.AllianceType, false, () =>
                {
                    var tileFx = InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,
                        inGameTile.View.CachedTr.position);

                    {
                        Span<double> buffStats = stackalloc double[3];
                        buffStats.Clear();
                        buffStats[0] = codeId;
                        buffStats[1] = _buffTime;
                        buffStats[2] = _buffRate;
                        var effectCodeID = new EffectCodeInfo((long)EffectCodeNameType.DEBUFF_DEF_PERCENT_DOWN, 0, buffStats);
                        InGameManager.Instance.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, source);
                    }
            
                    {
                        Span<double> buffStats = stackalloc double[3];
                        buffStats.Clear();
                        buffStats[0] = codeId;
                        buffStats[1] = _buffTime;
                        buffStats[2] = _healRate;
                        var effectCodeID = new EffectCodeInfo((long)EffectCodeNameType.DEBUFF_HEAL_RATE_DOWN, 0, buffStats);
                        InGameManager.Instance.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, source);
                    }
                });
            }
        });
    }
}
