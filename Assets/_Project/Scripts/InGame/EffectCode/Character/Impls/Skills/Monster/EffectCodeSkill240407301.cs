using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 4챕터 사막 전갈
// "좌 / 정면 / 우측 각각에 독침을 3발 발사한다. 
// 효과 : 공격력 {0}%의 대미지를 주고, 피격된 적에게 {1}초 동안 치유력 {2}%감소
//     독침은 적에게 피격될 시 후방으로 다시 분열되어 날라가 {3}%의 대미지를 준다. "
/// </summary>
[UseEffectCodeIds(240407301)]
public partial class EffectCodeSkill240407301 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _debuffTime;
    private ObfuscatorFloat _healDebuffRate;
    private ObfuscatorFloat _additionalDamageRate;

    private bool _isReadyToActivate;

    private List<InGameVfx> _vfxList = new List<InGameVfx>();

    private SkillActive _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(2);
        _healDebuffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _additionalDamageRate = codeInfo.GetCodeStatToFloat(5) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(2);
        _healDebuffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _additionalDamageRate = codeInfo.GetCodeStatToFloat(5) * 0.01f;
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
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

public override void OnSkillExecute(int executeIndex, int totalLength)
{
    base.OnSkillExecute(executeIndex, totalLength);

    var targetCharacters = GetClosestCharacters(owner, false, 3);

    foreach (var targetCharacter in targetCharacters)
    {
        ApplyVfxAndDamage(targetCharacter, _specSkill.skill_vfxs[0], _specSkill.skill_vfxs[1], _damageRate, true);
    }

    CoolTimeElapsedTime = 0;
    IsSkillActivated = false;
}

private void ApplyVfxAndDamage(CharacterController targetCharacter, InGameVfxNameType vfxProjectileType,
    InGameVfxNameType vfxHitType, ObfuscatorFloat damageRate, bool applyAdditionalDamage)
{
    //[TODO] 디버프 처리는 아직 안함.
    var targetTile = targetCharacter.CurrentTile;
    InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, targetTile);

    var vfxProjectile =
        InGameVfxManager.Instance.AddInGameVfx(vfxProjectileType, owner.CurrentTile.View.CachedTr.position);
    Vector3 direction = (targetTile.View.CachedTr.position - targetTile.View.CachedTr.position)
        .normalized;
    vfxProjectile.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

    var movement = InGameVfxMovementPool.Get<InGameVfxMovementBezier>();
    movement.SetData(vfxProjectile.transform, vfxProjectile.CachedTr.position, targetTile.View.CachedTr.position, 7);
    vfxProjectile.Initialize(false, movement);
    
    
    movement.SetData(vfxProjectile.CachedTr.position, targetTile.View.CachedTr.position, 10);
    vfxProjectile.Initialize(false, movement);

    void OnReachedTargetHandler()
    {
        vfxProjectile.Remove();

        if (targetCharacter != null || owner.IsAlive || owner is not null)
        {
            // 타겟 히트
            InGameVfxManager.Instance.AddInGameVfx(vfxHitType, targetCharacter.CurrentTile.View.CachedTr.position);
            
            var damage = owner.CalculateDamageAmount(owner.AD * damageRate, 0, targetCharacter, codeId, true);
            // var damage = owner.PrecalculateDamageAmount(owner.AD * damageRate, 0, targetCharacter, codeId, true);
                // owner.PostCalculateDamageAmount(ref damage, targetCharacter);
            targetCharacter.GetDamaged(damage, owner);
            
            {
                Span<double> eccStats = stackalloc double[3];
                eccStats.Clear();
                eccStats[0] = codeId;
                eccStats[1] = _debuffTime;
                eccStats[2] = _healDebuffRate;

                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_HEAL_RATE_DOWN, targetCharacter, eccStats, source);
            }

            if (applyAdditionalDamage)
            {
                var candidateTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(targetCharacter.CurrentTile, 1);
                candidateTiles.Remove(targetCharacter.CurrentTile);

                foreach (var tile in candidateTiles)
                {
                    if (tile.CheckValidTile(owner.AllianceType, false))
                    {
                        ApplyVfxAndDamage(tile.OccupiedCharacter, _specSkill.skill_vfxs[2], _specSkill.skill_vfxs[3],
                            _damageRate, false);
                    }
                }
            }
        }
    }

    movement.OnReachedTarget += OnReachedTargetHandler;
}

    private List<CharacterController> GetClosestCharacters(CharacterController owner, bool isOwnCharacter, int count)
    {
        var targetCharactersCandidates = InGameObjectManager.Instance.GetCharacterListSortedByDistance(owner, isOwnCharacter);
        var targetCharacters = new List<CharacterController>();
        if (targetCharactersCandidates.Count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                if (targetCharactersCandidates.Count > i)
                {
                    targetCharacters.Add(targetCharactersCandidates[i]);
                }
                else
                {
                    targetCharacters.Add(targetCharactersCandidates[0]);
                }
            }
        }

        return targetCharacters;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }
}