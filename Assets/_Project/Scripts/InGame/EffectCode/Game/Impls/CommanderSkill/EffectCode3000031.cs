using CookApps.BattleSystem;

public partial class EffectCode3000021 : EffectCodeCharacterBase
{
    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);

    }

    public override void OnPreRemoved()
    {
        owner.RemoveCrowdControl(CrowdControlType.Airborne);
        base.OnPreRemoved();
    }
}
