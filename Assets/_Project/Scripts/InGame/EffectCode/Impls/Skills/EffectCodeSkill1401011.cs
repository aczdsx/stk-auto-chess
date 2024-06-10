using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///
/// </summary>
[UseEffectCodeIds(1401011)]
public class EffectCodeSkill1401011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat cooltime;
    private ObfuscatorFloat power;

    private ObfuscatorFloat elapsedTime;

    private bool isReadyToActivate;
    private bool isSkillActivated;

    private InGameVfx _vfx;
    private InGameVfx _vfxProjectile;

    private List<CharacterController> _hitCharacters = new List<CharacterController>();

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        cooltime = codeInfo.GetCodeStatToFloat(0);
        power = codeInfo.GetCodeStatToFloat(1);
        elapsedTime = 0f;
        isReadyToActivate = false;
        isSkillActivated = false;
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        cooltime = codeInfo.GetCodeStatToFloat(0);
        power = codeInfo.GetCodeStatToFloat(1);
    }

    public override void OnUpdate(float dt)
    {
        if (!isSkillActivated)
        {
            return;
        }

        // target check
        if (false)
        {
            owner.AddNextState<CharacterStateIdle>();
            elapsedTime = cooltime;
        }
    }

    public override void OnCooltime(float dt)
    {
        if (isReadyToActivate || isSkillActivated)
            return;
        elapsedTime += dt;
        if (elapsedTime >= cooltime)
        {
            isReadyToActivate = true;
        }
    }

    public override bool IsReadyToActivate()
    {
        return isReadyToActivate;
    }

    public override void Activate()
    {
        base.Activate();
        // TODO: Target Check
        isReadyToActivate = false;
        isSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        var specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();

        // 검기 VFX
        _vfx = InGameVfxManager.Instance.AddInGameVfx(specSkill.skill_vfxs[0], InGameObjectManager.Instance.Playground);
        _vfx.CachedTr.position = owner.GetCharacterView().SkillRootTransform.position;

        // 발사체 VFX
        _vfxProjectile = InGameVfxManager.Instance.AddInGameVfx(specSkill.skill_vfxs[1], InGameObjectManager.Instance.Playground);
        _vfxProjectile.CachedTr.position = owner.CurrentTile.View.CachedTr.position;
        var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();

        var inGameTile = InGameObjectManager.Instance.InGameGrid.GetDirectionalTile(owner);
        if (inGameTile != null)
        {
            movement.SetData(_vfxProjectile.CachedTr.position, inGameTile.View.CachedTr.position, 15);
            _vfxProjectile.Initialize(false, movement);
            _vfxProjectile.OnCollisionWithTile += OnCollision2DEnter;
        }

        isSkillActivated = false;
    }

    private void OnCollision2DEnter(InGameVfx.CollisionType type, InGameTile tile, InGameVfx vfx)
    {
        var tileFx = InGameVfxManager.Instance.AddInGameTIleFx(owner.SpecCharacter.element_type, InGameObjectManager.Instance.Playground);
        tileFx.CachedTr.position = tile.View.CachedTr.position;

        if (tile.OccupiedCharacter == null)
            return;

        if (_hitCharacters.Contains(tile.OccupiedCharacter))
            return;

        var damage = owner.PrecalculateDamageAmount(owner.AD * power, 0, tile.OccupiedCharacter, codeId, true);
        owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
        tile.OccupiedCharacter.GetDamaged(damage, owner);

        _hitCharacters.Add(tile.OccupiedCharacter);
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        // _vfx.OnCollisionWithTile -= OnCollision2DEnter;
        isSkillActivated = false;
    }
}
