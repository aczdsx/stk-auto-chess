using System;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine.Pool;

/// <summary>
/// 아이콘을 위해 DEBUFF로 처리
/// 무조건 한개의 버프 스택만 유지한다.
/// </summary>.
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeDebuffOdetteCold : EffectCodeDebuffBase
{
    private const int CodeId = (int)EffectCodeNameType.DEBUFF_SPECIAL_ODETTE_COLD;
    private const BuffDebuffType buffDebuffType = BuffDebuffType.OdetteCold;
    private int _overlapCount;
    private float _debuffDuration;
    private bool _isNeedToShowIcon = true;
    public override bool IsNeedToShowIcon => _isNeedToShowIcon;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);

        _stackDatas = ListPool<BuffStackData>.Get();
        var buffStackData = GenericPool<BuffStackData>.Get();

        buffStackData.SetData(
        sourceCodeId: codeInfo.GetCodeStatToInt(0),
        duration: codeInfo.GetCodeStatToFloat(1),
        value: 1,
        source: source,
        isShowValue: true
        );
        _overlapCount = codeInfo.GetCodeStatToInt(2);
        _debuffDuration = codeInfo.GetCodeStatToFloat(3);

        _stackDatas.Add(buffStackData);

        owner.AddBuffDebuffType(buffDebuffType);
        owner.AddBuffStackData(CodeId, buffStackData);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);

        int newSourceCodeId = codeInfo.GetCodeStatToInt(0);
        _overlapCount = codeInfo.GetCodeStatToInt(2);
        _debuffDuration = codeInfo.GetCodeStatToFloat(3);
        // 같은 source가 있는지 확인
        for (int i = 0; i < _stackDatas.Count; i++)
        {
            if (_stackDatas[i].sourceCodeId == newSourceCodeId)
            {
                // 같은 source가 있으면 덮어쓰기
                var stackData = _stackDatas[i];
                stackData.duration = codeInfo.GetCodeStatToFloat(1);
                stackData.value += 1;
                stackData.elapsedTime = 0f;
                stackData.isShowValue = true;
                owner.SetBuffStackDataValue(CodeId, stackData.value);
                CheckOverlapCount();
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
            codeInfo.GetCodeStatToFloat(1),
            codeInfo.GetCodeStat(2),
            source,
            isShowValue: true
        );
        _stackDatas.Add(buffStackData);
        owner.AddBuffStackData(CodeId, buffStackData);

        CheckOverlapCount();

    }


    private void CheckOverlapCount()
    {
        if (_stackDatas[0].value >= _overlapCount)
        {
            Span<double> eccStats = stackalloc double[1];
            eccStats.Clear();
            eccStats[0] = _debuffDuration;

            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_STUN, owner, eccStats, source);

            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_ice_02, owner.SkillRootTransformFollowable.GetPosition());
            _isNeedToShowIcon = false;
            RemoveFromContainer();
        }
    }

    public override void OnPreRemoved()
    {
        owner.RemoveBuffDebuffType(buffDebuffType);
        owner.RemoveBuffStackData(codeId);
        base.OnPreRemoved();
        ListPool<BuffStackData>.Release(_stackDatas);
    }


}
