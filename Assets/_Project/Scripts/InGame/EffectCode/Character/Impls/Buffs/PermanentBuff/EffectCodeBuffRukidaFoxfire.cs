using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine.Pool;

/// <summary>
/// 아이콘을 위해 buff로 처리
/// 무조건 한개의 버프 스택만 유지한다.
/// </summary>.
/// 테토라
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeBuffRukidaFoxfire : EffectCodeBuffBase
{
    private const int CodeId = (int)EffectCodeNameType.BUFF_SPECIAL_RUKIDA_FOXFIRE;

    private const BuffDebuffType buffDebuffType = BuffDebuffType.RukidaFoxfire;
    public override bool IsNeedToShowIcon => true;


    private float _successRatePercent; // 성공 확률
    private float _damageRatePercent; // 추가 피해 비율
    private float _fireBuffTime; // 지속 시간
    private EffectCodeSkill217263103 _skillEffectCode;// 루키다 패시브

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _successRatePercent = codeInfo.GetCodeStatToFloat(1);
        _damageRatePercent = codeInfo.GetCodeStatToFloat(2);
        _fireBuffTime = codeInfo.GetCodeStatToFloat(3);
        _skillEffectCode = owner.GetEffectCodeContainer().GetEffectCode(codeInfo.GetCodeStatToInt(4)) as EffectCodeSkill217263103;
        _skillEffectCode.SetFoxFireDuration(_fireBuffTime);
// buffStats[0] = CodeId;
//             buffStats[1] = codeInfo.GetCodeStatToFloat(0);
//             buffStats[2] = codeInfo.GetCodeStatToFloat(1) * 0.01f;
//             buffStats[3] = codeInfo.GetCodeStatToFloat(2);
//             buffStats[4] = skillEffectCodeId;

        _stackDatas = ListPool<BuffStackData>.Get();
        var buffStackData = GenericPool<BuffStackData>.Get();

        buffStackData.SetData(
        sourceCodeId: codeInfo.GetCodeStatToInt(0),
        duration: 999f,
        value: 0,
        source: source,
        isShowValue: true,
        showPosition: BuffStackData.BuffShowPosition.SIDE
        );

        _stackDatas.Add(buffStackData);

        owner.AddBuffDebuffType(buffDebuffType);
        owner.AddBuffStackData(CodeId, buffStackData);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _successRatePercent = codeInfo.GetCodeStatToFloat(1);
        _damageRatePercent = codeInfo.GetCodeStatToFloat(2);
        _fireBuffTime = codeInfo.GetCodeStatToFloat(3);
        _skillEffectCode = owner.GetEffectCodeContainer().GetEffectCode(codeInfo.GetCodeStatToInt(4)) as EffectCodeSkill217263103;
        _skillEffectCode.SetFoxFireDuration(_fireBuffTime);
        
        int newSourceCodeId = codeInfo.GetCodeStatToInt(0);

        // 같은 source가 있는지 확인
        for (int i = 0; i < _stackDatas.Count; i++)
        {
            if (_stackDatas[i].sourceCodeId == newSourceCodeId)
            {
                // 같은 source가 있으면 덮어쓰기
                var stackData = _stackDatas[i];
                stackData.duration = 999f;
                stackData.value = 0;
                stackData.elapsedTime = 0f;
                stackData.isShowValue = true;
                return;
            }
        }

        // 같은 source가 없으면 기존 것을 모두 제거하고 새로 하나만 추가
        // 항상 한 개만 유지하기 위해
        for (int i = _stackDatas.Count - 1; i >= 0; i--)
        {
            owner.RemoveBuffStackData(_stackDatas[i]);
            GenericPool<BuffStackData>.Release(_stackDatas[i]);
            _stackDatas.RemoveAt(i);
        }

        var buffStackData = GenericPool<BuffStackData>.Get();
        buffStackData.SetData(
            newSourceCodeId,
            999f,
            0,
            source,
            isShowValue: true
        );
        _stackDatas.Add(buffStackData);
        owner.AddBuffStackData(CodeId, buffStackData);
    }

    public override void OnAttack()
    {
        base.OnAttack();
        if (InGameRandomManager.GetUniversalRandomValue(0, 100) <= _successRatePercent)
        {
            _skillEffectCode.AddFoxFire(1);
            _stackDatas[0].value = _skillEffectCode.GetCurrentFoxFireCount();
            owner.SetBuffStackDataValue(CodeId, _stackDatas[0].value);
        }
    }

    public override double ModifyDamageAmount(double damageAmount)
    {
        var foxFireCount = _skillEffectCode.GetCurrentFoxFireCount();
        if (foxFireCount > 0)
        {
            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01, owner.SkillRootTransformFollowable);
            return damageAmount * (1f + _damageRatePercent * foxFireCount);
        }
        return damageAmount;
    }

    public override void OnPreRemoved()
    {
        owner.RemoveBuffDebuffType(buffDebuffType);
        owner.RemoveBuffStackData(codeId);
        base.OnPreRemoved();
        ListPool<BuffStackData>.Release(_stackDatas);
    }


}
