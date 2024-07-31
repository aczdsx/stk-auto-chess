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
/// 5챕터 마법사
// 대상 : 가장 가까운 적 
// 범위 : 타겟의 좌우 2칸 (총합 x축 5개 칸 범위)
// 대미지 : 바닥에서 가시를 소환해 공격력 {0}%의 대미지를 준다.
/// </summary>
[UseEffectCodeIds(1103031)]
public class EffectCodeSkill1103031 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _damageRate;

    private bool _isReadyToActivate;

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
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

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByRow(owner.Target.CurrentTile, 2);
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.Target.CurrentTile.View.CachedTr.position);
        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile);
        }

        AfterAction(inGameTiles, 1).Forget();
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    private async UniTask AfterAction(List<InGameTile> inGameTiles, int second)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second));

        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile);
            tile.CheckValidTile(owner.AllianceType, false, () =>
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                    tile.OccupiedCharacter.SkillRootTransformFollowable);
                
                InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], tile.View.CachedTr.position);
                float calculatedDamageRate = _damageRate;

                var damage = owner.PrecalculateDamageAmount(owner.AD * 0, owner.AP * calculatedDamageRate,
                    tile.OccupiedCharacter, codeId, true);
                owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                tile.OccupiedCharacter.GetDamaged(damage, owner);
            });
        }

        IsSkillActivated = false;
    }
}
