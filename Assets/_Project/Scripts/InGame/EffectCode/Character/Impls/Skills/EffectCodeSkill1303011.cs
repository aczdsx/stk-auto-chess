using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 앨리스
// 대상 : 가장 가까운 적
// 대미지 : 공격력 {0}%의 마법 대미지를 준다.
//     특수 효과 : 적이 디버프 상태인 경우, 공격력 {1}%의 추가 대미지를 준다.
/// </summary>
[UseEffectCodeIds(1303011)]
public class EffectCodeSkill1303011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _additionalDamageRate;

    private bool _isReadyToActivate;
    private bool _isSkillActivated;

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _additionalDamageRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _isReadyToActivate = false;
        _isSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _additionalDamageRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
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

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetManhattanDistanceTiles(owner.Target.CurrentTile, 1);
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.Target.CurrentTile.View.CachedTr.position);
        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile.View.CachedTr.position);
        }

        AfterAction(inGameTiles, 1).Forget();
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        CoolTimeElapsedTime = 0;
        _isSkillActivated = false;
    }

    private async UniTask AfterAction(InGameTile[] inGameTiles, int second)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second));

        foreach (var tile in inGameTiles)
        {
            if (tile.OccupiedCharacter != null)
            {
                float calculatedDamageRate = _damageRate;
                if (tile.OccupiedCharacter.HasDebuffType())
                    calculatedDamageRate += _additionalDamageRate;

                var damage = owner.PrecalculateDamageAmount(owner.AD * 0, calculatedDamageRate, tile.OccupiedCharacter, codeId, true);
                owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                tile.OccupiedCharacter.GetDamaged(damage, owner);
            }
        }

        _isSkillActivated = false;
    }
}
