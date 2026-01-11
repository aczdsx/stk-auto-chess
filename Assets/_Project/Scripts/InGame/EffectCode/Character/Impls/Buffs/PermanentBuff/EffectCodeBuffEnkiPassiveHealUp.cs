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
/// 엔키 패시브 치유량 증가
/// 
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeBuffEnkiPassiveHealUp : EffectCodeBuffBase
{
    private const int CodeId = (int)EffectCodeNameType.BUFF_ENKI_PASSIVE_HEALUP;
    private const BuffDebuffType buffDebuffType = BuffDebuffType.EnkiPassiveHealUp;
    public override bool IsNeedToShowIcon => true;


    private int _healUpMaxCount; // 최대 증가 횟수
    private int _currentHealUpCount; // 현재 증가 횟수
    private float _healUpRatePercent; // 치유량 증가 비율

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _currentHealUpCount = 0;
        _healUpMaxCount = codeInfo.GetCodeStatToInt(1);
        _healUpRatePercent = codeInfo.GetCodeStatToFloat(2);

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
        _currentHealUpCount = 0;

        _healUpMaxCount = codeInfo.GetCodeStatToInt(1);
        _healUpRatePercent = codeInfo.GetCodeStatToFloat(2);
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


    public override void OnPreRemoved()
    {
        owner.RemoveBuffDebuffType(buffDebuffType);
        owner.RemoveBuffStackData(codeId);
        base.OnPreRemoved();
        ListPool<BuffStackData>.Release(_stackDatas);
    }


    public override void OnAttackEnd(CharacterController target)
    {
        base.OnAttackEnd(target);
        if (target.AllianceType == owner.AllianceType)
        {
            var prevHealUpCount = _currentHealUpCount;
            ++_currentHealUpCount;
            _currentHealUpCount = Math.Min(_currentHealUpCount, _healUpMaxCount);

            if (prevHealUpCount != _currentHealUpCount)
            {
                _stackDatas[0].value = _currentHealUpCount;
                owner.SetBuffStackDataValue(CodeId, _stackDatas[0].value);

                owner.GetEffectCodeContainer().SetDirtyFlag(this);
            }
        }
    }

    public override CharacterController.DamageInfo OnDamaged(CharacterController.DamageInfo damageInfo, CharacterController attacker, bool isPure)
    {
        if (attacker.AllianceType == owner.AllianceType)
        {
            var prevHealUpCount = _currentHealUpCount;
            --_currentHealUpCount;
            _currentHealUpCount = Math.Max(_currentHealUpCount, 0);

            if (prevHealUpCount != _currentHealUpCount)
            {
                _stackDatas[0].value = _currentHealUpCount;
                owner.SetBuffStackDataValue(CodeId, _stackDatas[0].value);

                owner.GetEffectCodeContainer().SetDirtyFlag(this);
            }
        }
        return damageInfo;
    }

    public override float GetIncrementPercentGivenHealRate()
    {
        return _healUpRatePercent * _currentHealUpCount;
    }


}
