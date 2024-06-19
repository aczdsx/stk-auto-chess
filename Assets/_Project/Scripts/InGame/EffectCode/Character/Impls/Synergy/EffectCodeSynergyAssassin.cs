using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///암살자 타입 캐릭터 적 처치 시 스킬 쿨타임 감소
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergyAssassin : EffectCodeCharacterBase
{
    public const int CodeId = 210401;
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
