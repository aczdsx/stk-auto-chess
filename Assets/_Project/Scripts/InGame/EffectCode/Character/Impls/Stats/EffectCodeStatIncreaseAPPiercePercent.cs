using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;

[UseEffectCodeIds((int)EffectCodeNameType.AP_PIERCE_PERCENT_UP)]
// [UseEffectCodeIds(14)]
public partial class EffectCodeStatIncreaseAPPiercePercent : EffectCodeStatBase
{
    public override int CalcOrder { get => calcOrder; }

    private ObfuscatorDouble increment;
    private int calcOrder;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        increment = codeInfo.GetCodeStat(0);
        if (codeInfo.HasCodeStat(1))
        {
            calcOrder = codeInfo.GetCodeStatToInt(1);
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
        // increment = codeInfo.GetCodeStat(0);
        if (codeInfo.HasCodeStat(1))
        {
            calcOrder = codeInfo.GetCodeStatToInt(1);
        }
        else
        {
            calcOrder = 0;
        }
        // 더하고 싶을 때
        increment += codeInfo.GetCodeStat(0);
    }

    public override double GetIncrementPercentAPPierce()
    {
        return increment;
    }
}
