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
/// 아드리아 패시브 팀 도움
/// 
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeBuffAdriaPassiveTeamHelp : EffectCodeBuffBase
{
    private const int CodeId = (int)EffectCodeNameType.BUFF_ADRIA_PASSIVE_TEAM_HELP;
    private const BuffDebuffType buffDebuffType = BuffDebuffType.AdriaPassiveTeamHelp;
    public override bool IsNeedToShowIcon => true;


    private float _adReduceApReduceRatePercent; // 물리/마법 저항력 증가 비율
    private float _healRatePercent; // 치유력 증가 비율
    private int _currentTargetCount;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _currentTargetCount = 0;
        _adReduceApReduceRatePercent = codeInfo.GetCodeStatToInt(1);
        _healRatePercent = codeInfo.GetCodeStatToFloat(2);

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
        _currentTargetCount = 0;

        _adReduceApReduceRatePercent = codeInfo.GetCodeStatToFloat(1);
        _healRatePercent = codeInfo.GetCodeStatToFloat(2);

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

    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);
        int targetCount = 0;
        var targettiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(owner.CurrentTile, 1);
        foreach (var tile in targettiles)
        {
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                targetCount++;
            }
        }

        if (targetCount != _currentTargetCount)
        {
            _currentTargetCount = targetCount;
            _stackDatas[0].value = _currentTargetCount;
            owner.SetBuffStackDataValue(CodeId, _stackDatas[0].value);
            owner.GetEffectCodeContainer().SetDirtyFlag(this);
        }
    }

    public override float GetIncrementPercentGivenHealRate()
    {
        return _healRatePercent * _currentTargetCount;
    }

    public override double GetIncrementPercentADReduce()
    {
        return _adReduceApReduceRatePercent * _currentTargetCount;
    }

    public override double GetIncrementPercentAPReduce()
    {
        return _adReduceApReduceRatePercent * _currentTargetCount;
    }


    public override void OnPreRemoved()
    {
        owner.RemoveBuffDebuffType(buffDebuffType);
        owner.RemoveBuffStackData(codeId);
        base.OnPreRemoved();
        ListPool<BuffStackData>.Release(_stackDatas);
    }





}
