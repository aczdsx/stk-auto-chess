using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 미노
// 타겟 범위 : 맵 전체 
// 타겟 : 현재 체력이 가장 낮은 적 3명 
// 대미지 : 적에게 유탄을 발사해 공격력 {0}%의 대미지를 준다. 유탄을 적을 타겟한 후 폭발해 주변에 {1}%의 추가 피해를 준다. 
// 추가 효과 : 범위 내 적이 1명인 경우 적에게 모든 포탄을 발사한다. 
/// </summary>
[UseEffectCodeIds(1404031)]
public class EffectCodeSkill1404031 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _additionalDamageRate;

    private bool _isReadyToActivate;

    private Dictionary<InGameVfx, InGameTile> _vfxDictionary = new();

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
        _vfxDictionary.Clear();
        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        var inGameCharacterListSortedByHpRate =
            InGameObjectManager.Instance.GetCharacterListSortedByHpRate(owner.AllianceType, false);
        List<CharacterController> targetCharacters = new();
        
        if (inGameCharacterListSortedByHpRate.Count > 0)
        {
            for (int i = 0; i < 3; i++)
            {
                if (inGameCharacterListSortedByHpRate.Count > i)
                {
                    targetCharacters.Add(inGameCharacterListSortedByHpRate[i]);
                }
                else
                {
                    targetCharacters.Add(inGameCharacterListSortedByHpRate[0]);
                }
            }
        }
        
        ProcessTarget(targetCharacters).Forget();

        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
    }
    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
    
    private async UniTask ProcessTarget(List<CharacterController> targetCharacters)
    {
        foreach (var target in targetCharacters)
        {
            if (target != null)
            {
                var vfxProjectile = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0],
                    owner.CurrentTile.View.CachedTr.position);
                var targetTile = target.CurrentTile;
                _vfxDictionary.Add(vfxProjectile, targetTile);
                Vector3 direction = (target.CurrentTile.View.CachedTr.position - targetTile.View.CachedTr.position)
                    .normalized;
                vfxProjectile.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

                var movement = InGameVfxMovementPool.Get<InGameVfxMovementBezier>();
                movement.SetData(vfxProjectile.transform, vfxProjectile.CachedTr.position, target.CurrentTile.View.CachedTr.position, 7);
                vfxProjectile.Initialize(false, movement);

                void OnReachedTargetHandler()
                {
                    vfxProjectile.Remove();
                    SkillAction(_vfxDictionary[vfxProjectile]);
                    _vfxDictionary.Remove(vfxProjectile);
                }

                movement.OnReachedTarget += OnReachedTargetHandler;
                await UniTask.Delay(TimeSpan.FromSeconds(0.3)); // 0.3초 간격으로 실행
            }
        }
    }

    private void SkillAction(InGameTile pivotTile)
    {
        InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, pivotTile);

        pivotTile.CheckValidTile(owner.AllianceType, false, () =>
        {
            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                pivotTile.OccupiedCharacter.SkillRootTransformFollowable);

            var damage = owner.PrecalculateDamageAmount(owner.AD * _damageRate, 0, pivotTile.OccupiedCharacter, codeId, true);
            owner.PostCalculateDamageAmount(ref damage, pivotTile.OccupiedCharacter);
            pivotTile.OccupiedCharacter.GetDamaged(damage, owner);
        });

        var vfxBoom = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], pivotTile.View.CachedTr.position);

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeX(pivotTile, 1);
        foreach (var inGameTile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, inGameTile);
            inGameTile.CheckValidTile(owner.AllianceType, false, () =>
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                    inGameTile.OccupiedCharacter.SkillRootTransformFollowable);

                var damage = owner.PrecalculateDamageAmount(owner.AD * _additionalDamageRate, 0, inGameTile.OccupiedCharacter, codeId, true);
                owner.PostCalculateDamageAmount(ref damage, inGameTile.OccupiedCharacter);
                inGameTile.OccupiedCharacter.GetDamaged(damage, owner);
            });
        }
    }
}
