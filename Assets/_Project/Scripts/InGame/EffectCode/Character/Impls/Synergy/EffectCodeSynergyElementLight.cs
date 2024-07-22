using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///빛 시너지 캐릭터 특정 시간 동안 모든 상태 이상 면역
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergyElementLight : EffectCodeCharacterBase
{
    public const int CodeId = 220301;
    private ObfuscatorFloat statValue;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        statValue = codeInfo.GetCodeStatToFloat(0);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        statValue = codeInfo.GetCodeStatToFloat(0);
    }

}
