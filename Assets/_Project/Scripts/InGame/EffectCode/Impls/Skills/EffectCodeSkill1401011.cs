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

        var specSkill = SpecDataManager.Instance.SpecSkill.Get(codeId);
        _vfx = InGameVfxManager.Instance.AddInGameVfx(specSkill.skill_vfxs[0], InGameObjectManager.Instance.Playground);
        _vfx.CachedTr.position = owner.GetCharacterView().SkillRootTransform.position;
        var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();
        _vfx.Initialize(false, movement);
        movement.SetData(_vfx.CachedTr.position, owner.Target.GetCharacterView().CachedTr.position, 1);
        _vfx.OnCollisionWithTile += OnCollision2DEnter;
        isSkillActivated = false;
    }

    private void OnCollision2DEnter(InGameVfx.CollisionType type, InGameTile tile, InGameVfx vfx)
    {
        if (tile.OccupiedCharacter == null)
            return;

        var damage = owner.PrecalculateDamageAmount(owner.AD * power, 0, tile.OccupiedCharacter, codeId, true);
        owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
        tile.OccupiedCharacter.GetDamaged(damage, owner);

        // var vfx = InGameVfxManager.Instance.AddInGameVfx("Skill_40101", tile.OccupiedCharacter.GetCharacterView().SkillRootTransform);
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        // _vfx.OnCollisionWithTile -= OnCollision2DEnter;
        isSkillActivated = false;
    }
}
