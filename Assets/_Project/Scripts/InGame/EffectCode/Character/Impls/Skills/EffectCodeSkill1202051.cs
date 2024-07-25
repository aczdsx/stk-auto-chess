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
/// 4챕터 마법사
// 샌드웜이 바닥으로 {0}초 동안 들어간다, 이 때 타겟 불가능 상태가 된다. 
//  현재 DPS 가장 높은 적에게 큰 가시를 소환해 공격력 {1}%의 대미지를 준다. 
//  그 주변 범위에는 작은 가시를 소환해 {2}%의 대미지를 준다. 
/// </summary>
[UseEffectCodeIds(1103021)]
public class EffectCodeSkill1202051 : EffectCodeCharacterBase
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

        var characterControllers = InGameObjectManager.Instance.GetCharacterListSortedByAD(owner.AllianceType, false);
        if (characterControllers.Count > 0)
        {
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], characterControllers[0].CurrentTile.View.CachedTr.position);
            float calculatedDamageRate = _damageRate;
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, characterControllers[0].CurrentTile.View.CachedTr.position);

            var damage = owner.PrecalculateDamageAmount(owner.AD * 0, owner.AP * calculatedDamageRate,
                characterControllers[0], codeId, true);
            owner.PostCalculateDamageAmount(ref damage, characterControllers[0]);
            characterControllers[0].GetDamaged(damage, owner);
            
            // 주변 타겟
            var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByManhattanDistanceInRange(owner.Target.CurrentTile, 2);
            inGameTiles.Remove(characterControllers[0].CurrentTile);
            foreach (var tile in inGameTiles)
            {
                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile.View.CachedTr.position);
            }

            AfterAction(inGameTiles, 0.2f).Forget();
        }
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    private async UniTask AfterAction(List<InGameTile> inGameTiles, float second)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second));
        
        foreach (var tile in inGameTiles)
        {
            if (tile.OccupiedCharacter != null)
            {
                if (tile.OccupiedCharacter.AllianceType != AllianceType.Wall)
                {
                    if (tile.OccupiedCharacter.AllianceType != owner.AllianceType)
                    {
                        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], tile.View.CachedTr.position);
                        float calculatedDamageRate = _damageRate;

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
