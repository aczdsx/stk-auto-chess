using CookApps.Obfuscator;
using CookApps.BattleSystem;

[UseEffectCodeIds(14010310)]
public class EffectCodeBuffAtkUp : EffectCodeCharacterBase
{
    private ObfuscatorFloat duration;
    private ObfuscatorFloat increaseRate;

    private float elapsedTime;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        duration = codeInfo.GetCodeStatToFloat(1);
        increaseRate = codeInfo.GetCodeStatToFloat(2); // 이미 0.01f 곱해져서 들어옴
        elapsedTime = 0f;
        owner.AddBuffDebuffType(BuffDebuffType.AttackUp);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        // 덮어 씌울 경우
        duration = codeInfo.GetCodeStatToFloat(1);
        increaseRate = codeInfo.GetCodeStatToFloat(2); // 이미 0.01f 곱해져서 들어옴
        elapsedTime = 0f;
        // 더할 경우
        // duration += codeInfo.GetCodeStatToFloat(1);
        // decreaseRate = Mathf.Max(decreaseRate, codeInfo.GetCodeStatToFloat(2));
    }

    public override void OnUpdate(float dt)
    {
        elapsedTime += dt;
        if (elapsedTime >= duration)
        {
            elapsedTime = 0f;
            RemoveFromContainer();
        }
    }

    public override void OnPreRemoved()
    {
        base.OnPreRemoved();
        owner.RemoveBuffDebuffType(BuffDebuffType.AttackUp);
    }

    public override double GetIncrementPercentAD()
    {
        return increaseRate;
    }
}
