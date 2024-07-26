using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 3챕터 레어 가디언
// 범위 : 자신의 전방 1x2
// 대미지 : 검기를 날려 범위 내 적들에게 공격력 {0}%의 대미지를 준다.
/// </summary>
[UseEffectCodeIds(1201011)]
public class EffectCodeSkill1201011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;

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
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
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
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0],
            owner.CurrentTile.View.CachedTr.position);
        Vector3 direction = (owner.Target.CurrentTile.View.CachedTr.position - vfx.CachedTr.position).normalized;
        vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

        IsSkillActivated = false;

        List<InGameTile> inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner, 2);
        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile.View.CachedTr.position);
            tile.CheckValidTile(owner.AllianceType, false, () =>
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                    tile.OccupiedCharacter.SkillRootTransformFollowable);

                var damage = owner.PrecalculateDamageAmount(owner.AD * _powerRate, 0, tile.OccupiedCharacter, codeId, true);
                owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                tile.OccupiedCharacter.GetDamaged(damage, owner);

                _hitCharacters.Add(tile.OccupiedCharacter);
            });
        }
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
}
