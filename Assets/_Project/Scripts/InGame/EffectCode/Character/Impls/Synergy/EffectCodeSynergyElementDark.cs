using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///마법사 타입 캐릭터 스킬 쿨타임 감소 속도 증가
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergyElementDark : EffectCodeCharacterBase
{
    public const int CodeId = 220401;
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

    // [TODO] 스킬 쿨타임 감소 속도 증가
}
