using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using UnityEngine.Pool;

[UseEffectCodeIds(CodeId)]
public partial class EffectCodeDebuffPoision : EffectCodeDebuffBase
{
    public const int CodeId = (int) EffectCodeNameType.DEBUFF_POISON;
    private const BuffDebuffType buffDebuffType = BuffDebuffType.Poison;

    private float elapsedTime = 0f;
    private float updateInterval = 1f;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _stackDatas = ListPool<BuffStackData>.Get();
        var buffStackData = GenericPool<BuffStackData>.Get();
        buffStackData.SetData(
            sourceCodeId: codeInfo.GetCodeStatToInt(0),
            duration: codeInfo.GetCodeStatToInt(1),
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
        {
            if (stackData.sourceCodeId == codeInfo.GetCodeStatToInt(0))
            {
                hasSameSource = true;
                // 덮어 씌울 경우
                stackData.duration = codeInfo.GetCodeStatToInt(1);
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

        _stackDatas = ListPool<BuffStackData>.Get();
        var buffStackData = GenericPool<BuffStackData>.Get();
        buffStackData.SetData(
            sourceCodeId: codeInfo.GetCodeStatToInt(0),
            duration: codeInfo.GetCodeStatToInt(3),
            value: codeInfo.GetCodeStat(2),
            source: source
        );
        _stackDatas.Add(buffStackData);
        owner.AddBuffStackData(CodeId, buffStackData);
    }

    public override bool TryRemoveWithSource(IEffectCodeSource source)
    {
        var isRemoved = false;
        for (int i = 0; i < _stackDatas.Count; i++)
        {
            if (_stackDatas[i].source == source)
            {
                GenericPool<BuffStackData>.Release(_stackDatas[i]);
                _stackDatas[i] = null;
                isRemoved = true;
            }
        }

        if (isRemoved)
            container.SetDirtyFlag(this);

        return false;
    }

    public override void OnUpdate(float dt)
    {
        elapsedTime += dt;

        if (elapsedTime >= updateInterval)
        {
            elapsedTime = 0f;

            foreach (var data in _stackDatas)
            {
                var damage = owner.CalculateDamageAmount(data.value, 0, owner, codeId, true);
                owner.GetDamaged(damage, owner);
            }
        }

        bool needRemove = false;
        for (int i = 0; i < _stackDatas.Count; i++)
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
            if (_stackDatas.Count <= 0)
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
        owner.RemoveBuffStackData(codeId);
        base.OnPreRemoved();
        ListPool<BuffStackData>.Release(_stackDatas);
    }
}//240107002
