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
// 효과 : 체력을 공격력 {0}%만큼 회복시킨다.
/// </summary>
[UseEffectCodeIds(1406011)]
public class EffectCodeSkill1406011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _coolTime;
    private ObfuscatorFloat _damageRate;

    private ObfuscatorFloat _elapsedTime;

    private bool _isReadyToActivate;
    private bool _isSkillActivated;

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _coolTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _elapsedTime = 0f;
        _isReadyToActivate = false;
        _isSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _coolTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
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
            _elapsedTime = _coolTime;
        }
    }

    public override void OnCooltime(float dt)
    {
        if (_isReadyToActivate || _isSkillActivated)
            return;
        _elapsedTime += dt;
        if (_elapsedTime >= _coolTime)
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

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByAllianceType(owner.AllianceType, 10);
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.Target.CurrentTile.View.CachedTr.position);
        foreach (var tile in inGameTiles)
        {
            if (tile.OccupiedCharacter != owner)
                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,
                    tile.View.CachedTr.position);
        }

        foreach (var tile in inGameTiles)
        {
            if (tile.OccupiedCharacter != null)
            {
                if (tile.OccupiedCharacter != owner)
                {
                    // [TODO] 힐 계산은 어떻게 하나요...?
                    double damage = owner.PostCalculateHealAmount(_damageRate, tile.OccupiedCharacter);
                    tile.OccupiedCharacter.GetHealed(damage, owner, codeId, true);
                }
            }
        }

        _isSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        _elapsedTime = 0;
        _isSkillActivated = false;
    }
}
