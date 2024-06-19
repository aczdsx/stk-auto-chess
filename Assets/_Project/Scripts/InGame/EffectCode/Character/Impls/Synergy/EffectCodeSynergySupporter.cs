using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///서포터 타입 스킬 사용 공격력 가장 높은 아군 쿨타임 감소
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergySupporter : EffectCodeCharacterBase
{
    public const int CodeId = 210501;
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
