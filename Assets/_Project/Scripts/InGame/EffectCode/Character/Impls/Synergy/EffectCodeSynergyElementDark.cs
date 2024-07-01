using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
// 적군 스킬 쿹타임 감소 속도 감소
// 적군 스킬 쿹타임 감소 속도 {0}%감소
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergyElementDark : EffectCodeCharacterBase
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
            Span<double> debuffStats = stackalloc double[3];
            debuffStats.Clear();
            debuffStats[0] = codeId;
            debuffStats[1] = 99.0f;
            debuffStats[2] = _statValue;
            var effectCodeID = new EffectCodeInfo((long)EffectCodeNameType.DEBUFF_COOL_DOWN_SPEED_PERCENT_DOWN, 0, debuffStats);
            character.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeID, owner);
        }
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _statValue = codeInfo.GetCodeStatToFloat(0);
        _enemyType = codeInfo.GetCodeStatToInt(1);
    }
}
