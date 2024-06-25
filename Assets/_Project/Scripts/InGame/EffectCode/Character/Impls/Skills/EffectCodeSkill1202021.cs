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
/// 1챕터 보스 탱커
// 범위 : 자신 중심 십자가 전범위
// 대미지 : 공격력 {0}%의 대미지를 가한다.
//     특수 효과 : 피격된 적을 {1}초 동안 스턴시킨다.
/// </summary>
[UseEffectCodeIds(1202021)]
public class EffectCodeSkill1202021 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;

    private bool _isReadyToActivate;
    private bool _isSkillActivated;

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _isReadyToActivate = false;
        _isSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
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

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeX(owner.CurrentTile);
        inGameTiles.RemoveAll(l => l.OccupiedCharacter == owner);

        foreach (var tile in inGameTiles)
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile.View.CachedTr.position);


        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], tile.View.CachedTr.position);

            if (tile.OccupiedCharacter != null)
            {
                if (tile.OccupiedCharacter.AllianceType != owner.AllianceType)
                {
                    var damage = owner.PrecalculateDamageAmount(owner.AD * _powerRate, 0, tile.OccupiedCharacter,
                        codeId, true);
                    owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                    tile.OccupiedCharacter.GetDamaged(damage, owner);
                }
            }
        }

        _isSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        _isSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
}
