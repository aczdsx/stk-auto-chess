using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
/// 아군 전체 보호막 생성
///평균 공격력 {0}%의 보호막 생성
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergyElementWater : EffectCodeCharacterBase
{
    public const int CodeId = 220201;
    private ObfuscatorFloat _statValue;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _statValue = codeInfo.GetCodeStatToFloat(0);


        double averageAD = 0;
        var list = InGameObjectManager.Instance.GetCharacterList(owner.AllianceType);
        foreach (var character in list)
        {
            averageAD += character.AD;
        }
        averageAD /= list.Count;


        var shieldAmount = owner.PrecalculateDamageAmount(averageAD * _statValue, 0, owner,
            codeId, true);

        Span<double> eccStats = stackalloc double[2];
        eccStats.Clear();
        eccStats[0] = 10.0f;
        eccStats[1] = shieldAmount.damageAmount;
        
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.SHIELD, owner, eccStats, source);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _statValue = codeInfo.GetCodeStatToFloat(0);
    }
}
