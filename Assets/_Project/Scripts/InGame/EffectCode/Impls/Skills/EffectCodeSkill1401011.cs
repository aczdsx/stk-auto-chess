using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 아트레시아
///범위 : 전방 X축 2칸
// 대미지 : 검기를 날려, 적에게 공격력 {0}%의 대미지를 준다.
//     특수 효과 : 검기는 맵 끝까지 지속된다.
/// </summary>
[UseEffectCodeIds(1401011)]
public class EffectCodeSkill1401011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _cooltime;
    private ObfuscatorFloat _power;

    private ObfuscatorFloat _elapsedTime;

    private bool _isReadyToActivate;
    private bool _isSkillActivated;

    private List<CharacterController> _hitCharacters = new List<CharacterController>();

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _cooltime = codeInfo.GetCodeStatToFloat(0);
        _power = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _elapsedTime = 0f;
        _isReadyToActivate = false;
        _isSkillActivated = false;
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _cooltime = codeInfo.GetCodeStatToFloat(0);
        _power = codeInfo.GetCodeStatToFloat(1) * 0.01f;
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
            _elapsedTime = _cooltime;
        }
    }

    public override void OnCooltime(float dt)
    {
        if (_isReadyToActivate || _isSkillActivated)
            return;
        _elapsedTime += dt;
        if (_elapsedTime >= _cooltime)
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
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        var specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();

        // 검기 VFX
        var vfx = InGameVfxManager.Instance.AddInGameVfx(specSkill.skill_vfxs[0], InGameObjectManager.Instance.Playground);
        vfx.CachedTr.position = owner.GetCharacterView().SkillRootTransform.position;

        // 발사체 VFX
        var vfxProjectile = InGameVfxManager.Instance.AddInGameVfx(specSkill.skill_vfxs[1], InGameObjectManager.Instance.Playground);
        vfxProjectile.CachedTr.position = owner.CurrentTile.View.CachedTr.position;
        var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();

        var inGameTile = InGameObjectManager.Instance.InGameGrid.GetDirectionalTile(owner);
        if (inGameTile != null)
        {
            Vector3 direction = (inGameTile.View.CachedTr.position - vfxProjectile.CachedTr.position).normalized;
            vfxProjectile.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

            movement.SetData(vfxProjectile.CachedTr.position, inGameTile.View.CachedTr.position, 15);
            vfxProjectile.Initialize(false, movement);
            vfxProjectile.OnCollisionWithTile += OnCollision2DEnter;
        }

        _isSkillActivated = false;
    }

    private void OnCollision2DEnter(InGameVfx.CollisionType type, InGameTile tile, InGameVfx vfx)
    {
        var tileFx = InGameVfxManager.Instance.AddInGameTIleFx(owner.SpecCharacter.element_type,tile.View.CachedTr);
        tileFx.CachedTr.position = tile.View.CachedTr.position;

        if (tile.OccupiedCharacter == null)
            return;

        if (_hitCharacters.Contains(tile.OccupiedCharacter))
            return;

        // var hitFx = InGameVfxManager.Instance.AddInGameVfx(vfxtypefx, tile.OccupiedCharacter);

        var damage = owner.PrecalculateDamageAmount(owner.AD * _power, 0, tile.OccupiedCharacter, codeId, true);
        owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
        tile.OccupiedCharacter.GetDamaged(damage, owner);

        _hitCharacters.Add(tile.OccupiedCharacter);
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        // _vfx.OnCollisionWithTile -= OnCollision2DEnter;
        _isSkillActivated = false;
    }
}
