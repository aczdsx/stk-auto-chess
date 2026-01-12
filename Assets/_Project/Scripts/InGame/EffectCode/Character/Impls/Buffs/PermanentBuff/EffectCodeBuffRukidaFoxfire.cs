using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;
using System;



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
    private float _angerRatePercent;// 몇퍼센트 잃을때마다 분노 1 획득
    private float _attackRatePercent;// 분노 1 획득 시 공격력 증가 퍼센트
    private int _angerCount;// 분노 개수

    // 테토라의 체력을 _angerRatePercent% 잃을 때 마다, 분노를 1 획득합니다. 
    // #분노: 공격력이 _attackRatePercent% 상승합니다.

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _angerRatePercent = codeInfo.GetCodeStatToFloat(1);
        _attackRatePercent = codeInfo.GetCodeStatToFloat(2);

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
        _angerRatePercent = codeInfo.GetCodeStatToFloat(1);
        _attackRatePercent = codeInfo.GetCodeStatToFloat(2);
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

    public override void OnHpChange()
    {
        base.OnHpChange();
        if (owner == null || owner.HP <= 0d)
        {
            return;
        }

        // HP 비율 계산 (1.0 ~ 0.0)
        float hpRate = (float)(owner.CurrentHp / owner.HP) * 100f;
        
        // 잃은 HP 비율 계산 (0 ~ 100)
        float lostHpRate = 100f - hpRate;
        // hpRate가 90이라면?
        // lostHPRate는 10.0?

        //10을 잃었는데 _angerRatePercent가 3이라면?
        // _angerRate는 3

        // 분노 개수 계산: 잃은 HP 비율을 _angerRatePercent%로 나눔
        _angerCount = (int)Math.Round(lostHpRate / (_angerRatePercent));

        // 분노 개수를 스택 데이터에 반영
        if (_stackDatas.Count > 0)
        {
            _stackDatas[0].value = _angerCount;
            owner.SetBuffStackDataValue(CodeId, _stackDatas[0].value);
        }

        // 더티 플래그 설정하여 스탯 재계산
        owner.GetEffectCodeContainer().SetDirtyFlag(this);
    }

    /// <summary>
    /// 분노 개수에 따른 공격력 증가 반환
    /// </summary>
    public override double GetIncrementPercentAD()
    {
        return _angerCount * _attackRatePercent;
    }

    public override void OnPreRemoved()
    {
        owner.RemoveBuffDebuffType(buffDebuffType);
        owner.RemoveBuffStackData(codeId);
        base.OnPreRemoved();
        ListPool<BuffStackData>.Release(_stackDatas);
    }


}
