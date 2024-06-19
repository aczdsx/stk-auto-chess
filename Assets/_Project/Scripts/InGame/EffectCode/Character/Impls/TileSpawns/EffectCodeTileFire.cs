using CookApps.Obfuscator;
using CookApps.AutoBattler;
using CookApps.BattleSystem;

public class EffectCodeTileFire : EffectCodeCharacterBase
{
    public override bool IsRemoveWithSource { get => false; }
    public override EffectCodeType Type { get => EffectCodeType.Game; }

    private ObfuscatorFloat _elapsedTime;
    private ObfuscatorFloat _duration;
    private ObfuscatorFloat _damageRate;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _elapsedTime = 0;
        _duration = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1);
        owner.AddCrowdControl(CrowdControlType.Stun);
    }

    public override void OnPreRemoved()
    {
        owner.RemoveCrowdControl(CrowdControlType.Stun);
        base.OnPreRemoved();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        this.source = source;
        _duration = codeInfo.GetCodeStatToFloat(0);
        _elapsedTime = 0;
    }

    public override void OnUpdate(float dt)
    {
        _elapsedTime += dt;
        if (_duration < _elapsedTime)
        {
            RemoveFromContainer();
        }
    }
}
