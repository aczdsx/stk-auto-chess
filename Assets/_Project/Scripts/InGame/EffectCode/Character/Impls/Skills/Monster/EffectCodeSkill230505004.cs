using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 5챕터 저격수
/// 범위 : 전방 일직선 (현재 위치에서 즉발)
// 대미지 : 관통하는 레이저 발사해 공격력 {0}%의 대미지를 준다.
/// </summary>
[UseEffectCodeIds(230505004)]
public partial class EffectCodeSkill230505004 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;

    private bool _isReadyToActivate;

    private List<CharacterController> _hitCharacters = new List<CharacterController>();

    private WeakReference<InGameVfx> _vfx;

    private SkillActive _specSkill;

    private SynergyType _elementType;

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
        // TODO: Target Check
        _isReadyToActivate = false;
        IsSkillActivated = true;
        _hitCharacters.Clear();
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
        _elementType = owner.SpecCharacter.character_element_type;
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);

        owner.Target = InGameObjectManager.Instance.GetOptimalAttackTarget(owner);

        if (owner.Target != null)
        {
            var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.CurrentTile.View.CachedTr.position);
            Vector3 direction = (owner.Target.CurrentTile.View.CachedTr.position - vfx.CachedTr.position).normalized;
            vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

            vfx.Initialize(false);
            // [InGame_New: removed] vfx.OnCollisionWithTile += OnCollision2DEnter;
        }

        IsSkillActivated = false;
    }

    // [InGame_New: removed] private void OnCollision2DEnter(InGameVfx.CollisionType type, InGameTile tile, InGameVfx vfx)
    // [InGame_New: removed] {
        // [InGame_New: removed] var tileFx = InGameVfxManager.Instance.AddInGameTileFx(_elementType, tile);
        // [InGame_New: removed] if (tileFx != null)
        // [InGame_New: removed] {
            // [InGame_New: removed] tileFx.CachedTr.position = tile.View.CachedTr.position;

            // [InGame_New: removed] if (owner != null)
            // [InGame_New: removed] {
                // [InGame_New: removed] if (tile.CheckValidTile(owner.AllianceType, false))
                // [InGame_New: removed] {
                    // [InGame_New: removed] InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
                    // [InGame_New: removed] if (_hitCharacters.Contains(tile.OccupiedCharacter))
                        // [InGame_New: removed] return;

                    // [InGame_New: removed] if (owner.AllianceType == tile.OccupiedCharacter.AllianceType)
                        // [InGame_New: removed] return;

                    // [InGame_New: removed] InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                        // [InGame_New: removed] tile.OccupiedCharacter.SkillRootTransformFollowable);

                    // [InGame_New: removed] var damage = owner.CalculateDamageAmount(owner.AD * _powerRate, 0, tile.OccupiedCharacter, codeId, true);
                    // [InGame_New: removed] // var damage = owner.PrecalculateDamageAmount(owner.AD * _powerRate, 0, tile.OccupiedCharacter,
                    // [InGame_New: removed] //     codeId, true);
                    // [InGame_New: removed] // owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                    // [InGame_New: removed] tile.OccupiedCharacter.GetDamaged(damage, owner);

                    // [InGame_New: removed] _hitCharacters.Add(tile.OccupiedCharacter);
                // [InGame_New: removed] }
            // [InGame_New: removed] }
        // [InGame_New: removed] }
    // [InGame_New: removed] }

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
