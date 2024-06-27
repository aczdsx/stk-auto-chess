using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using UnityEngine.Pool;

[UseEffectCodeIds(CodeId)]
public class EffectCodeDebffAtkSpeedDown : EffectCodeBuffBase
{
    private const int CodeId = (int)EffectCodeNameType.DEBUFF_ATK_SPEED_DOWN;
    private const BuffDebuffType buffDebuffType = BuffDebuffType.AttackSpeedDown;
    private List<BuffStackData> stackDatas = new List<BuffStackData>();

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        stackDatas = ListPool<BuffStackData>.Get();
        var buffStackData = GenericPool<BuffStackData>.Get();
        buffStackData.SetData(
            sourceCodeId: codeInfo.GetCodeStatToInt(0),
            duration: codeInfo.GetCodeStatToFloat(1),
            value: codeInfo.GetCodeStat(2),
            source: source
        );
        stackDatas.Add(buffStackData);
        owner.AddBuffDebuffType(buffDebuffType);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);

        var hasSameSource = false;
        foreach (var stackData in stackDatas)
        {
            if (stackData.sourceCodeId == codeInfo.GetCodeStatToInt(0))
            {
                hasSameSource = true;
                // 덮어 씌울 경우
                stackData.duration = codeInfo.GetCodeStatToFloat(1);
                stackData.value = codeInfo.GetCodeStat(2);
                stackData.elapsedTime = 0f;
                // 더할 경우
                // stackData.duration += codeInfo.GetCodeStatToFloat(1);
                // stackData.value = Math.Max(stackData.value, codeInfo.GetCodeStat(2));
                break;
            }
        }

        if (hasSameSource)
            return;

        var buffStackData = GenericPool<BuffStackData>.Get();
        buffStackData.SetData(
            sourceCodeId: codeInfo.GetCodeStatToInt(0),
            duration: codeInfo.GetCodeStatToFloat(1),
            value: codeInfo.GetCodeStat(2),
            source: source
        );
        stackDatas.Add(buffStackData);
    }

    public override bool TryRemoveWithSource(IEffectCodeSource source)
    {
        var isRemoved = false;
        for (int i = 0; i < stackDatas.Count; i++)
        {
            if (stackDatas[i].source == source)
            {
                GenericPool<BuffStackData>.Release(stackDatas[i]);
                stackDatas[i] = null;
                isRemoved = true;
            }
        }

        if (isRemoved)
            container.SetDirtyFlag(this);

        return false;
    }

    public override void OnUpdate(float dt)
    {
        bool needRemove = false;
        for (int i = 0; i < stackDatas.Count; i++)
        {
            if (stackDatas[i] == null)
            {
                needRemove = true;
                continue;
            }

            if (stackDatas[i].AddDeltaTime(dt))
            {
                GenericPool<BuffStackData>.Release(stackDatas[i]);
                stackDatas[i] = null;
                needRemove = true;
            }
        }

        if (needRemove)
        {
            stackDatas.RemoveAll(NullChecker<BuffStackData>.NullCheck);
            if (stackDatas.Count <= 0)
            {
                RemoveFromContainer();
            }

            if (container != null)
                container.SetDirtyFlag(this);
        }
    }

    public override void OnPreRemoved()
    {
        owner.RemoveBuffDebuffType(buffDebuffType);
        base.OnPreRemoved();
        ListPool<BuffStackData>.Release(stackDatas);
    }

    public override float GetIncrementPercentAttackSpeed()
    {
        double increaseRate = 0;
        for (int i = 0; i < stackDatas.Count; i++)
        {
            increaseRate += stackDatas[i]?.value ?? 0;
        }
        return -(float)increaseRate;
    }
}
