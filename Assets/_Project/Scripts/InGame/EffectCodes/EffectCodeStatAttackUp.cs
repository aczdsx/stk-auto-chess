using CookApps.Obfuscator;
using CookApps.TeamBattle.BattleSystem;

[UseEffectCodeIds(101)]
public class EffectCodeStatFixedAD : EffectCodeStatBase
{
    private ObfuscatorDouble increment;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        increment = codeInfo.GetCodeStat(1);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        // 덮어 씌우고 싶을 때
        increment = codeInfo.GetCodeStat(1);
        // 더하고 싶을 때
        // increment += codeInfo.GetCodeStat(1);
    }

    public virtual double GetIncrementFixedAD()
    {
        return 0;
    }
}
