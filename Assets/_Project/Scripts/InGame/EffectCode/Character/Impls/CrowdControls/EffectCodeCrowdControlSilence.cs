using CookApps.Obfuscator;
using CookApps.AutoBattler;
using CookApps.BattleSystem;

[UseEffectCodeIds(CodeId)]
public partial class EffectCodeCrowdControlSilence : EffectCodeCharacterBase
{
    public const int CodeId = (int)EffectCodeNameType.SILENCE;
    public override bool IsRemoveWithSource { get => false; }
    public override EffectCodeType Type { get => EffectCodeType.CrowdControl; }

    private ObfuscatorFloat elapsedTime;
    private ObfuscatorFloat duration;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        duration = codeInfo.GetCodeStatToFloat(0);
        elapsedTime = 0;
        owner.AddCrowdControl(CrowdControlType.Silence);
    }

    public override void OnPreRemoved()
    {
        owner.RemoveCrowdControl(CrowdControlType.Silence);
        base.OnPreRemoved();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        this.source = source;
        duration = codeInfo.GetCodeStatToFloat(0);
        elapsedTime = 0;
    }

    public override void OnUpdate(float dt)
    {
        elapsedTime += dt;
        if (duration < elapsedTime)
        {
            RemoveFromContainer();
        }
    }
}
