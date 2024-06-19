using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///저격수 타입 캐릭터 공격 시 스킬 쿨타임 감소
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergyPositionRanger : EffectCodeCharacterBase
{
    public const int CodeId = 210301;
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

    // [TODO] 공격 시 스킬 쿨타임 감소
    public override void OnAttack()
    {
        base.OnAttack();

    }
}
