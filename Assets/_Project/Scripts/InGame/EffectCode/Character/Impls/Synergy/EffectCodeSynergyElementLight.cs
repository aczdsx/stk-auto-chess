using System;
using CookApps.AutoBattler;
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
        
        Span<double> buffStats = stackalloc double[3];
        buffStats.Clear();
        buffStats[0] = codeId;
        buffStats[1] = statValue;
        buffStats[2] = 1;
        
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_IMMUNE, owner, buffStats, source);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        statValue = codeInfo.GetCodeStatToFloat(0);
    }

}
