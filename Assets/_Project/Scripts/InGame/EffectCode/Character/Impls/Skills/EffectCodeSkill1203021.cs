using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 5챕터 독 두꺼비
// "스킬 대상 : 가장 멀리 위치한 적
// 스킬 범위 : 적 중심 3*3방향
// 스킬 공격 : 공격력 {0}%의 대미지를 주고 초당 {1}%의 독 대미지를 준다. 
// 효과 : 피격된 적의 공격속도가 {2}초 동안 {3}% 감소한다. "
/// </summary>
[UseEffectCodeIds(1203021)]
public class EffectCodeSkill1203021 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _additionalDamageRate;

    private bool _isReadyToActivate;

    private List<InGameVfx> _vfxList = new List<InGameVfx>();

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _additionalDamageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _additionalDamageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
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
        _vfxList.Clear();
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
        
        var targetCharacterList =
            InGameObjectManager.Instance.GetCharacterListSortedByDistanceDescending(owner, false);
        
        if (targetCharacterList.Count > 0)
        {
            var target = targetCharacterList[0];
            
            var targetTile = target.CurrentTile;
            var tileFx = InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,
                targetTile.View.CachedTr.position);

            Vector3 direction = (targetTile.View.CachedTr.position - vfxProjectile.CachedTr.position).normalized;
            vfxProjectile.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

            movement.SetData(vfxProjectile.CachedTr.position, targetTile.View.CachedTr.position, 15);
            vfxProjectile.Initialize(false, movement);

            movement.OnReachedTarget += OnReachedTargetHandler;
            
            void OnReachedTargetHandler()
            {
                vfxProjectile.Remove();

                var tiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(targetTile, 1);
                foreach (var tile in tiles)
                {
                    tile.CheckValidTile(owner.AllianceType, false, () =>
                    {
                        var tileFx = InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,
                            tile.View.CachedTr.position);
                    
                        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], tile.View.CachedTr.position);
                        float calculatedDamageRate = _additionalDamageRate;

                        var damage = owner.PrecalculateDamageAmount(owner.AD * 0, owner.AP * calculatedDamageRate,
                            tile.OccupiedCharacter, codeId, true);
                        owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                        tile.OccupiedCharacter.GetDamaged(damage, owner);
                    });
                }
            }
        }

        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
}
