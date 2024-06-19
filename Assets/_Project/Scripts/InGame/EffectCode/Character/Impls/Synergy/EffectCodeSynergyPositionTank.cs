using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///탱커 타입 캐릭터 피격 시 스킬 쿨타임 감소
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergyPositionTank : EffectCodeCharacterBase
{
    public const int CodeId = 210101;
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

    // [TODO] 피격 당했을 시
}
