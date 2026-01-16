using CookApps.Obfuscator;
using CookApps.AutoBattler;
using CookApps.BattleSystem;

[UseEffectCodeIds((int)EffectCodeNameType.BLOCKING_PROB_PERCENT_UP)]
// [UseEffectCodeIds(28)]
public partial class EffectCodeStatIncreaseBlokingProbPercent : EffectCodeStatBase
{
    public override int CalcOrder { get => calcOrder; }

    private ObfuscatorFloat increment;
    private int calcOrder;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        increment = codeInfo.GetCodeStatToFloat(0);
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
        // increment = codeInfo.GetCodeStatToFloat(0);
        if (codeInfo.HasCodeStat(1))
        {
            calcOrder = codeInfo.GetCodeStatToInt(1);
        }
        else
        {
            calcOrder = 0;
        }
        // 더하고 싶을 때
        increment += codeInfo.GetCodeStatToFloat(0);
    }

    public override float GetIncrementPercentBlockingProb()
    {
        return increment;
    }
}
