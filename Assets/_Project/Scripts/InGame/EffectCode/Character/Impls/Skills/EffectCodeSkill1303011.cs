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

    private SpecSkill _specSkill;
    private List<CharacterController> _hitCharacters = new List<CharacterController>();

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _additionalDamageRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

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

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByManhattanDistanceInRange(owner.Target.CurrentTile, 1);
        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile);
        }

        AfterAction(inGameTiles, 0.8f).Forget();
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    private async UniTask AfterAction(List<InGameTile> inGameTiles, float second)
    {
        foreach (var tile in inGameTiles)
        {
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], tile.View.CachedTr.position);
            }
        }

        await UniTask.Delay(TimeSpan.FromSeconds(second));

        if (owner != null)
        {
            List<int> targetCharacterList = new();
            foreach (var tile in inGameTiles)
            {
                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile);

                if (tile.CheckValidTile(owner.AllianceType, false))
                {
                    if (!targetCharacterList.Contains(tile.OccupiedCharacter.CharacterUId))
                    {
                        targetCharacterList.Add(tile.OccupiedCharacter.CharacterUId);
                        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                            tile.OccupiedCharacter.SkillRootTransformFollowable);
                
                        float calculatedDamageRate = _damageRate;
                        if (tile.OccupiedCharacter.GetCharacterStat().Spec.element_type == ElementType.FIRE)
                            calculatedDamageRate += _additionalDamageRate;

                        var damage = owner.PrecalculateDamageAmount(owner.AD * 0, owner.AP * calculatedDamageRate,
                            tile.OccupiedCharacter, codeId, true);
                        owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                        tile.OccupiedCharacter.GetDamaged(damage, owner);
                    }
                }
            }
        }

        IsSkillActivated = false;
    }
}
