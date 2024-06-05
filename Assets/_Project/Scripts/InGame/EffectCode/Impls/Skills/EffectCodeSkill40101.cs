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

        var vfx = InGameVfxManager.Instance.AddInGameVfx("Skill_40101", InGameObjectManager.Instance.Playground);
        vfx.CachedTr.position = owner.GetCharacterView().SkillRootTransform.position;
        var movement = InGameVfxMovementPool.Create<InGameVfxMovementLinear>();
        vfx.Initialize(false, movement);
        movement.SetData(vfx.CachedTr.position, owner.Target.GetCharacterView().CachedTr.position, 1);
        //vfx.OnCollision2D ;
        isSkillActivated = false;
    }

    private void OnCollision2DEnter(InGameVfx vfx, InGameTile tile)
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
        isSkillActivated = false;
    }
}
