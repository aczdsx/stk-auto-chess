using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///
/// </summary>
[UseEffectCodeIds()]
public partial class EffectCodeSkillTemplate : EffectCodeCharacterBase
{
    private ObfuscatorFloat cooltime;

    private ObfuscatorFloat elapsedTime;

    private bool isReadyToActivate;
    private bool isSkillActivated;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        cooltime = codeInfo.GetCodeStatToFloat(0);
        elapsedTime = 0f;
        isReadyToActivate = false;
        isSkillActivated = false;
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        cooltime = codeInfo.GetCodeStatToFloat(0);
    }

    public override void OnUpdate(float dt)
    {
        if (isReadyToActivate || isSkillActivated)
            return;

        elapsedTime += dt;
        if (elapsedTime >= cooltime)
        {
            elapsedTime = 0f;
            isSkillActivated = true;
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
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        isSkillActivated = false;
    }
}
