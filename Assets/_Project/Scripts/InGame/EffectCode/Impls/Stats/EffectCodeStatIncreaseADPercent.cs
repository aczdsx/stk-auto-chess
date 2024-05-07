using CookApps.Obfuscator;
using CookApps.AutoBattler;
using CookApps.BattleSystem;

[UseEffectCodeIds((int)CharacterEffect.AD_PERCENT_UP)]
public class EffectCodeStatIncreaseADPercent : EffectCodeStatBase
{
    public override int CalcOrder { get => calcOrder; }

    private ObfuscatorDouble increment;
    private int calcOrder;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        increment = codeInfo.GetCodeStat(1);
        if (codeInfo.HasCodeStat(2))
        {
            calcOrder = codeInfo.GetCodeStatToInt(2);
        }
        else
        {
            calcOrder = 0;
        }
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        // 덮어 씌우고 싶을 때
        increment = codeInfo.GetCodeStat(1);
        if (codeInfo.HasCodeStat(2))
        {
            calcOrder = codeInfo.GetCodeStatToInt(2);
        }
        else
        {
            calcOrder = 0;
        }
        // 더하고 싶을 때
        // increment += codeInfo.GetCodeStat(1);
    }

    public override double GetIncrementPercentAD()
    {
        return increment;
    }
}
