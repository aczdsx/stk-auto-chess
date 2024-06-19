using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
/// 불 속성 캐릭터 추가 타격 발동
///공격력 {0}% 추가 타격
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergyElementFire : EffectCodeCharacterBase
{
    public const int CodeId = 220001;
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

    public override void OnAttack()
    {
        base.OnAttack();

        if (owner.Target != null)
        {
            // [TODO] 불 이펙트 필요 + 어떻게 관리할까요?
            var damage = owner.PrecalculateDamageAmount(owner.AD * statValue, 0, owner.Target, codeId, true);
            owner.PostCalculateDamageAmount(ref damage, owner.Target);
            owner.Target.GetDamaged(damage, owner);
        }
    }
}
