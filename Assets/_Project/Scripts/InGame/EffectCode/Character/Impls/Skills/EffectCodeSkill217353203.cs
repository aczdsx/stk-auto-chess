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
[UseEffectCodeIds(217353203)]
public partial class EffectCodeSkill217353203 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _healRate;
    private ObfuscatorFloat _buffTime;
    private ObfuscatorFloat _buffRate;
    private bool _isReadyToActivate;
    private SkillActive _specSkill;

    private InGameVfx _vfxObj;
    private InGameTile _targetTile;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _buffTime = codeInfo.GetCodeStatToFloat(1);
        _healRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
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
        _buffTime = codeInfo.GetCodeStatToFloat(1);
        _healRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
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
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
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
            movement.SetData(_vfxObj.CachedTr.position, target.CurrentTile.View.CachedTr.position, 5);
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
    

        

    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }

    

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
    
    private void SkillAction(InGameTile pivotTile)
    {
        InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, pivotTile);
        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(pivotTile, 1);
        List<int> targetCharacterList = new();
        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], tile.View.CachedTr.position);
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                if (!targetCharacterList.Contains(tile.OccupiedCharacter.CharacterUId))
                {
                    targetCharacterList.Add(tile.OccupiedCharacter.CharacterUId);
                    {
                        Span<double> eccStats = stackalloc double[3];
                        eccStats.Clear();
                        eccStats[0] = codeId;
                        eccStats[1] = _buffTime;
                        eccStats[2] = _buffRate;

                        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_AD_REDUCE_PERCENT_DOWN, tile.OccupiedCharacter, eccStats, source);
                    }
                
                    {
                        Span<double> eccStats = stackalloc double[3];
                        eccStats.Clear();
                        eccStats[0] = codeId;
                        eccStats[1] = _buffTime;
                        eccStats[2] = _healRate;

                        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_HEAL_RATE_DOWN, tile.OccupiedCharacter, eccStats, source);
                    }
                    {
                        Span<double> eccStats = stackalloc double[3];
                        eccStats.Clear();
                        eccStats[0] = codeId;
                        eccStats[1] = _buffTime;
                        eccStats[2] = _buffRate;

                        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_AD_REDUCE_PERCENT_DOWN, tile.OccupiedCharacter, eccStats, source);
                    }
                
                    {
                        Span<double> eccStats = stackalloc double[3];
                        eccStats.Clear();
                        eccStats[0] = codeId;
                        eccStats[1] = _buffTime;
                        eccStats[2] = _healRate;

                        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_HEAL_RATE_DOWN, tile.OccupiedCharacter, eccStats, source);
                    }
                }
            }
        }
    }
}
