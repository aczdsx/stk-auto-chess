using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;

[UseEffectCodeIds(CodeId)]
public partial class EffectCodeBuffNormalAttackShield : EffectCodeBuffBase
{
    private const int CodeId = (int)EffectCodeNameType.BUFF_NORMAL_ATTACK_SHIELD;
    private const BuffDebuffType buffDebuffType = BuffDebuffType.NormalAttackShield;

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

        var hasSameSource = false;
        foreach (var stackData in _stackDatas)
            if (stackData.sourceCodeId == codeInfo.GetCodeStatToInt(0))
            {
                hasSameSource = true;
                // 덮어 씌울 경우
                stackData.duration = codeInfo.GetCodeStatToFloat(1);
                stackData.value = codeInfo.GetCodeStat(2);
                stackData.elapsedTime = 0f;
                stackData.isShowValue = true;
                break;
            }

        if (hasSameSource)
            return;


        var buffStackData = GenericPool<BuffStackData>.Get();
        buffStackData.SetData(
            codeInfo.GetCodeStatToInt(0),
            codeInfo.GetCodeStatToFloat(1),
            codeInfo.GetCodeStat(2),
            source,
            isShowValue: true
        );
        _stackDatas.Add(buffStackData);
        owner.AddBuffStackData(CodeId, buffStackData);
    }

    public override CharacterController.DamageInfo OnDamaged(CharacterController.DamageInfo damageInfo,
     CharacterController attacker, bool isFirstDamage)
    {
        if(_stackDatas.Count == 0)
        {
            return damageInfo;
        }
        //기본공격을 받았다면.
        if (damageInfo.source <= 0)
        {
            damageInfo.damageAmount = 0d;
            --_stackDatas[0].value;
            if (_stackDatas[0].value <= 0)
            {
                RemoveFromContainer();
                return damageInfo;
            }
            else
            {
                owner.SetBuffStackDataValue(CodeId, _stackDatas[0].value);
            }

            var affectText = buffDebuffType.GetAffectToken();
            owner.ShowNormalText(affectText, hexColor: "5#5DC9FFFF").Forget();
        }


        return damageInfo;
        
    }

    public override bool TryRemoveWithSource(IEffectCodeSource source)
    {
        var isRemoved = false;
        for (var i = 0; i < _stackDatas.Count; i++)
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
    public override void OnPreRemoved()
    {
        owner.RemoveBuffDebuffType(buffDebuffType);
        owner.RemoveBuffStackData(codeId);
        base.OnPreRemoved();
        ListPool<BuffStackData>.Release(_stackDatas);
    }


}
