using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;
using UnityEngine;


/// <summary>
/// 아이콘을 위해 buff로 처리
/// 무조건 한개의 버프 스택만 유지한다.
/// </summary>.
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeBuffAprilStander : EffectCodeBuffBase
{
    private const int CodeId = (int)EffectCodeNameType.BUFF_SPECIAL_APRIL_STANDER;
    private const BuffDebuffType buffDebuffType = BuffDebuffType.AprilStander;
    public override bool IsNeedToShowIcon => true;

    private float _increaseTime;
    private float _elapsedTime;
    private float _attackSpeedIncreaseRate;
    private float _currentAttackSpeedIncreaseRate;

    private static readonly float _maxAttackSpeedIncreaseRate = 0.6f;
    private InGameTile _prevTile;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);

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

        _increaseTime = codeInfo.GetCodeStatToFloat(1);
        _attackSpeedIncreaseRate = codeInfo.GetCodeStatToFloat(2); 

        _currentAttackSpeedIncreaseRate = 0f;
        _elapsedTime = 0f;

    }

    // 사실상 머지콜은 안올것
    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);

        int newSourceCodeId = codeInfo.GetCodeStatToInt(0);
        
        _increaseTime = codeInfo.GetCodeStatToFloat(1);
        _attackSpeedIncreaseRate = codeInfo.GetCodeStatToFloat(2); 

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

    public override void OnCombatStart()
    {
        _prevTile = owner.CurrentTile;
    }

    public override void OnUpdate(float dt)
    {
        //todo 스텍데이터 업데이트 필요.
        if (_prevTile == null || owner.CurrentTile == null)
            return;

        _elapsedTime += dt;
        if (_elapsedTime < _increaseTime)
            return;

        _elapsedTime = 0f;
        var prevAttackSpeedIncreaseRate = _currentAttackSpeedIncreaseRate;

        if (_prevTile == owner.CurrentTile)
        {
            _currentAttackSpeedIncreaseRate += _attackSpeedIncreaseRate;
        }
        else
        {
            _currentAttackSpeedIncreaseRate -= _attackSpeedIncreaseRate * 0.5f;
        }

        _currentAttackSpeedIncreaseRate = Mathf.Clamp(_currentAttackSpeedIncreaseRate, 0f, _maxAttackSpeedIncreaseRate);
        if (prevAttackSpeedIncreaseRate != _currentAttackSpeedIncreaseRate)
        {
            owner.GetEffectCodeContainer().SetDirtyFlag(this);
        }

        _stackDatas[0].value = Mathf.Round(_currentAttackSpeedIncreaseRate * 100f);

        owner.SetBuffStackDataValue(CodeId, _stackDatas[0].value);


        _prevTile = owner.CurrentTile;
    }

    public override float GetIncrementPercentAttackSpeed()
    {
        return _currentAttackSpeedIncreaseRate;
    }
    public override void OnPreRemoved()
    {
        owner.RemoveBuffDebuffType(buffDebuffType);
        owner.RemoveBuffStackData(codeId);
        base.OnPreRemoved();
        ListPool<BuffStackData>.Release(_stackDatas);
    }


}
