using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using System;
using UnityEngine;
/// <summary>
/// 물리 공격력 {0}% 마법 공격력이 {0}% 상승합니다.
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyElementFire : EffectCodeCharacterBase
{
    public const int CodeId = 100101;
    private ObfuscatorFloat statValue;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        statValue = codeInfo.GetCodeStatToFloat(0);

        Span<double> IncreaseStat = stackalloc double[1];
        IncreaseStat.Clear();
        IncreaseStat[0] = statValue;
        if (owner.SpecCharacter.atk_type == AtkType.AD)
        {
            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.AD_PERCENT_UP, owner, IncreaseStat, source);
        }
        else
        {
            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.AP_PERCENT_UP, owner, IncreaseStat, source);
        }

        Debug.LogColor($"불시너지 물리 공격력 {statValue}% 마법 공격력 {statValue}% 상승", "green");
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        statValue = codeInfo.GetCodeStatToFloat(0);
    }

    
}
