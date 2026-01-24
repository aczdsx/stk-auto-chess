using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using UnityEngine.Pool;

[UseEffectCodeIds(CodeId)]
public partial class EffectCodeBuffImmune : EffectCodeBuffBase
{
    private const int CodeId = (int)EffectCodeNameType.BUFF_IMMUNE;
    private const BuffDebuffType buffDebuffType = BuffDebuffType.Immune;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _stackDatas = ListPool<BuffStackData>.Get();
        var buffStackData = GenericPool<BuffStackData>.Get();
        buffStackData.SetData(
            sourceCodeId: codeInfo.GetCodeStatToInt(0),
            duration: codeInfo.GetCodeStatToFloat(1),
            value: codeInfo.GetCodeStat(2),
            source: source
        );
        _stackDatas.Add(buffStackData);
        owner.AddBuffDebuffType(buffDebuffType);
        owner.AddBuffStackData(CodeId, buffStackData);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);

        var hasSameSource = false;
        foreach (var stackData in _stackDatas)
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

        if (hasSameSource)
            return;

        var buffStackData = GenericPool<BuffStackData>.Get();
        buffStackData.SetData(
            codeInfo.GetCodeStatToInt(0),
            codeInfo.GetCodeStatToFloat(1),
            codeInfo.GetCodeStat(2),
            source
        );
        _stackDatas.Add(buffStackData);
        owner.AddBuffStackData(CodeId, buffStackData);
    }

    public override bool TryRemoveWithSource(IEffectCodeSource source)
    {
        var isRemoved = false;
        for (var i = 0; i < _stackDatas.Count; i++)
        {
            if(_stackDatas[i] == null)
            {
                isRemoved = true;
                continue;
            }

            if (_stackDatas[i].source == source)
            {
                owner.RemoveBuffStackData(_stackDatas[i]);
                GenericPool<BuffStackData>.Release(_stackDatas[i]);
                _stackDatas[i] = null;
                isRemoved = true;
            }
        }


        if (isRemoved)
        {
            _stackDatas.RemoveAll(NullChecker<BuffStackData>.NullCheck);
            if (_stackDatas.Count <= 0) RemoveFromContainer();

            container?.SetDirtyFlag(this);
        }
        return false;
    }

    public override void OnUpdate(float dt)
    {
        var needRemove = false;
        for (var i = 0; i < _stackDatas.Count; i++)
        {
            if (_stackDatas[i] == null)
            {
                needRemove = true;
                continue;
            }

            if (_stackDatas[i].AddDeltaTime(dt))
            {
                owner.RemoveBuffStackData(_stackDatas[i]);
                GenericPool<BuffStackData>.Release(_stackDatas[i]);
                _stackDatas[i] = null;
                needRemove = true;
            }
        }

        if (needRemove)
        {
            _stackDatas.RemoveAll(NullChecker<BuffStackData>.NullCheck);
            if (_stackDatas.Count <= 0) RemoveFromContainer();

            if (container != null)
                container.SetDirtyFlag(this);
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