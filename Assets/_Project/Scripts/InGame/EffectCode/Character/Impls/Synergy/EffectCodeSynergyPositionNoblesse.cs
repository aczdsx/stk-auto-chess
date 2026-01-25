using System;
using CookApps.AutoBattler;
using CookApps.BattleSystem;

/// <summary>
/// 노블레스 클래스 시너지 타입
/// 1: 전체 아군 유닛 크기 {_statValue_1}% 상승 + 위력(공격력) {_statValue_2}% 증가
/// 2: 전체 체력에 비례한 쉴드 {_statValue_1}% 생성
/// 3: 전체 전투 시작 후 {_statValue_1}초 동안 모든 상태 이상 면역
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyPositionNoblesse : EffectCodeSynergyBase
{
    private enum NoblesseGrade
    {
        NONE = 0,
        SCALE_PERCENT_UP_AD_PERCENT_UP = 1,
        SHIELD_GENERATION = 2,
        IMMUNE_ALL_DEBUFF = 3,
    }
    public const int CodeId = 200101;
    private float _statValue_1;
    private float _statValue_2;
    private NoblesseGrade _synergyGrade;
    private InGameVfx _crownVfx;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _statValue_1 = codeInfo.GetCodeStatToFloat(0);
        _statValue_2 = codeInfo.GetCodeStatToFloat(1);
        _synergyGrade = (NoblesseGrade)codeInfo.GetCodeStatToInt(2);
        ApplyNoblesseEffect(source);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _statValue_1 = codeInfo.GetCodeStatToFloat(0);
        _statValue_2 = codeInfo.GetCodeStatToFloat(1);
        _synergyGrade = (NoblesseGrade)codeInfo.GetCodeStatToInt(2);
        ApplyNoblesseEffect(source);
    }

    private void ApplyNoblesseEffect(IEffectCodeSource source)
    {
        Debug.LogColor($"ApplyNoblesseEffect: {_synergyGrade}", "green");
        switch (_synergyGrade)
        {
            case NoblesseGrade.SCALE_PERCENT_UP_AD_PERCENT_UP:
                ScaleUpAttackPowerUp(source);
                break;
            case NoblesseGrade.SHIELD_GENERATION:
                ShieldGeneration(source);
                break;
            case NoblesseGrade.IMMUNE_ALL_DEBUFF:
                ImmuneAllDebuff(source);
                break;
        }
    }
    private void ScaleUpAttackPowerUp(IEffectCodeSource source)
    {
        Span<double> stats = stackalloc double[1];

        stats.Clear();
        stats[0] = _statValue_1 * 0.01f;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.VIEW_SCALE_UP, owner, stats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.VIEW_SCALE_UP);

        stats.Clear();
        stats[0] = _statValue_2 * 0.01f;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.AD_PERCENT_UP, owner, stats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.AD_PERCENT_UP);
    }

    private void ShieldGeneration(IEffectCodeSource source)
    {
        AllianceType allianceType = owner.AllianceType;
        Span<double> stats = stackalloc double[2];

        stats.Clear();
        stats[0] = 99999f;
        stats[1] = owner.HP * _statValue_1 * 0.01f;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.SHIELD, owner, stats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.SHIELD);
        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_nb_shield_01, owner.SkillMiddleFXTransformFollowable);
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_shield);
    }
    private void ImmuneAllDebuff(IEffectCodeSource source)
    {
        if (_crownVfx != null)
        {
            _crownVfx.Remove();
        }
        _crownVfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_nb_crown_01, owner.SkillTopFXTransformFollowable);
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_buff);
        Span<double> buffStats = stackalloc double[3];


        buffStats.Clear();
        buffStats[0] = codeId;
        buffStats[1] = InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase ? _statValue_1 : 999f;//duration
        buffStats[2] = 1;//value?

        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_IMMUNE, owner, buffStats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.BUFF_IMMUNE);
    }
    
    public override void OnPreRemoved()
    {
        if (_crownVfx != null)
        {
            _crownVfx.Remove();

        }
        _crownVfx = null;
        base.OnPreRemoved();
    }

}
