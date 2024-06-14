using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using UnityEngine.Pool;

[UseEffectCodeIds((int)CharacterEffectType.BUFF_AD_PERCENT_UP)]
public class EffectCodeBuffAtkUp : EffectCodeCharacterBase
{
    private List<BuffStackData> stackDatas = new List<BuffStackData>();

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        stackDatas = ListPool<BuffStackData>.Get();
        var buffStackData = GenericPool<BuffStackData>.Get();
        buffStackData.SetData(
            sourceId: codeInfo.GetCodeStatToInt(0),
            duration: codeInfo.GetCodeStatToFloat(1),
            value: codeInfo.GetCodeStat(2)
        );
        stackDatas.Add(buffStackData);
        owner.AddBuffDebuffType(BuffDebuffType.AttackUp);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);

        var hasSameSource = false;
        foreach (var stackData in stackDatas)
        {
            if (stackData.sourceId == codeInfo.GetCodeStatToInt(0))
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
            sourceId: codeInfo.GetCodeStatToInt(0),
            duration: codeInfo.GetCodeStatToFloat(1),
            value: codeInfo.GetCodeStat(2)
        );
        stackDatas.Add(buffStackData);
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

            container.SetDirtyFlag(this);
        }
    }

    public override void OnPreRemoved()
    {
        owner.RemoveBuffDebuffType(BuffDebuffType.AttackUp);
        base.OnPreRemoved();
        ListPool<BuffStackData>.Release(stackDatas);
    }

    public override double GetIncrementPercentAD()
    {
        double increaseRate = 0;
        for (int i = 0; i < stackDatas.Count; i++)
        {
            increaseRate += stackDatas[i]?.value ?? 0;
        }
        return increaseRate;
    }
}
