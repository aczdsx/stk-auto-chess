using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;


/// <summary>
/// 아이콘을 위해 buff로 처리
/// 무조건 한개의 버프 스택만 유지한다.
/// </summary>.
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeBuffNoDamageShield : EffectCodeBuffBase
{
    private const int CodeId = (int)EffectCodeNameType.BUFF_NO_DAMAGE_SHIELD;
    private const BuffDebuffType buffDebuffType = BuffDebuffType.NoDamageShield;

    public override bool IsNeedToShowIcon => true;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);

        _stackDatas = ListPool<BuffStackData>.Get();
        var buffStackData = GenericPool<BuffStackData>.Get();

        buffStackData.SetData(
        sourceCodeId: codeInfo.GetCodeStatToInt(0),
        duration: codeInfo.GetCodeStatToFloat(1),
        value: codeInfo.GetCodeStat(2),
        source: source,
        isShowValue: true
        );

        _stackDatas.Add(buffStackData);

        owner.AddBuffDebuffType(buffDebuffType);
        owner.AddBuffStackData(CodeId, buffStackData);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);

        int newSourceCodeId = codeInfo.GetCodeStatToInt(0);

        // 같은 source가 있는지 확인
        for (int i = 0; i < _stackDatas.Count; i++)
        {
            if (_stackDatas[i].sourceCodeId == newSourceCodeId)
            {
                // 같은 source가 있으면 덮어쓰기
                var stackData = _stackDatas[i];
                stackData.duration = codeInfo.GetCodeStatToFloat(1);
                stackData.value = codeInfo.GetCodeStat(2);
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
            codeInfo.GetCodeStatToFloat(1),
            codeInfo.GetCodeStat(2),
            source,
            isShowValue: true
        );
        _stackDatas.Add(buffStackData);
        owner.AddBuffStackData(CodeId, buffStackData);
    }

    public override double ModifyDamageAmount(double damageAmount)
    {

        damageAmount = 0d;
        --_stackDatas[0].value;
        if (_stackDatas[0].value <= 0)
        {
            RemoveFromContainer();
            return damageAmount;
        }
        else
        {
            owner.SetBuffStackDataValue(CodeId, _stackDatas[0].value);
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
