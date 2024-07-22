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
// 대상 : 물리 또는 마법 방어력이 가장 높은 적의 3*3 범위
// 효과 : 약병을 투하해 {0}초 동안 범위 내에 위치한 적군의 치유 효과를 {1}%, 
// 물리/마법 방어력을 {2}% 감소시킨다.
/// </summary>
[UseEffectCodeIds(1406021)]
public class EffectCodeSkill1406021 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _debuffTime;
    private ObfuscatorFloat _healDebuffRate;
    private ObfuscatorFloat _defDebuffRate;

    private bool _isReadyToActivate;

    private List<CharacterController> _hitCharacters = new List<CharacterController>();

    private WeakReference<InGameVfx> _vfx;

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _debuffTime = codeInfo.GetCodeStatToFloat(1);
        _healDebuffRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _defDebuffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _debuffTime = codeInfo.GetCodeStatToFloat(1);
        _healDebuffRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _defDebuffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
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
        _hitCharacters.Clear();
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
        var inGameTile = InGameObjectManager.Instance.InGameGrid.GetTileListByNearest(owner.CurrentTile);
        if (inGameTile.Count > 0)
        {
            var targetTile = inGameTile[0];
            var tileFx = InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,targetTile.View.CachedTr.position);
            
            Vector3 direction = (targetTile.View.CachedTr.position - vfxProjectile.CachedTr.position).normalized;
            vfxProjectile.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

            movement.SetData(vfxProjectile.CachedTr.position, inGameTile[0].View.CachedTr.position, 15);
            vfxProjectile.Initialize(false, movement);
            vfxProjectile.OnCollisionWithTile += OnCollision2DEnter;
            // movement.OnReachedTarget +=
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

        if (_hitCharacters.Contains(tile.OccupiedCharacter))
            return;

        if(owner.AllianceType == tile.OccupiedCharacter.AllianceType)
            return;

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(owner.CurrentTile, 1);
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

            {
                Span<double> buffStats = stackalloc double[3];
                buffStats.Clear();
                buffStats[0] = codeId;
                buffStats[1] = _debuffTime;
                buffStats[2] = _defDebuffRate;
                var effectCodeID = new EffectCodeInfo((long)EffectCodeNameType.DEBUFF_DEF_PERCENT_DOWN, 0, buffStats);
                InGameManager.Instance.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, source);
            }
            
            {
                Span<double> buffStats = stackalloc double[3];
                buffStats.Clear();
                buffStats[0] = codeId;
                buffStats[1] = _debuffTime;
                buffStats[2] = _healDebuffRate;
                var effectCodeID = new EffectCodeInfo((long)EffectCodeNameType.DEBUFF_HEAL_RATE_DOWN, 0, buffStats);
                InGameManager.Instance.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, source);
            }
        }
        
        _hitCharacters.Add(tile.OccupiedCharacter);
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
}
