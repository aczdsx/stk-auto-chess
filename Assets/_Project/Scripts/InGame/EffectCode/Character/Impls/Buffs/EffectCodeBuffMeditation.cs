using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;

[UseEffectCodeIds(CodeId)]
public partial class EffectCodeBuffMeditation : EffectCodeBuffBase
{
    private const int CodeId = (int)EffectCodeNameType.BUFF_SPECIAL_MEDITATION;
    private const BuffDebuffType buffDebuffType = BuffDebuffType.Meditation;

    private float _healUpdateInterval = 1f;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _stackDatas = ListPool<BuffStackData>.Get();
        var buffStackData = GenericPool<BuffStackData>.Get();
        buffStackData.SetData(
            codeInfo.GetCodeStatToInt(0),
            codeInfo.GetCodeStatToFloat(1),
            codeInfo.GetCodeStat(2),
            source
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
            if (_stackDatas[i].source is null || _stackDatas[i].source == source)
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
                continue;
            }

            if (_stackDatas[i].elapsedTime >= _healUpdateInterval)
            {
                owner.GetHealed(_stackDatas[i].value, _stackDatas[i].source as CharacterController, CodeId, true);
                _stackDatas[i].elapsedTime = 0f;
                _stackDatas[i].duration -= _healUpdateInterval;
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