using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
// 적군 스킬 쿹타임 감소 속도 감소
// 적군 스킬 쿹타임 감소 속도 {0}%감소
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyElementDark : EffectCodeCharacterBase
{
    public const int CodeId = 220401;
    private ObfuscatorFloat _statValue;
    private ObfuscatorInt _enemyType;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _statValue = codeInfo.GetCodeStatToFloat(0);
        _enemyType = codeInfo.GetCodeStatToInt(1);

        AllianceType allianceType = (AllianceType) (int) _enemyType == AllianceType.Player
            ? AllianceType.Enemy
            : AllianceType.Player;

        var characterList = InGameObjectManager.Instance.GetCharacterList(allianceType);
        foreach (var character in characterList)
        {
            Span<double> eccStats = stackalloc double[3];
            eccStats.Clear();
            eccStats[0] = codeId;
            eccStats[1] = 10.0f;
            eccStats[2] = _statValue;
            
            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_COOL_DOWN_SPEED_PERCENT_DOWN, character, eccStats, source);
        }
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _statValue = codeInfo.GetCodeStatToFloat(0);
        _enemyType = codeInfo.GetCodeStatToInt(1);
    }
}
