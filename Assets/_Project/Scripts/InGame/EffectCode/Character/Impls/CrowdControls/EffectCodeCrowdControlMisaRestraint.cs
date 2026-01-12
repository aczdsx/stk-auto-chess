using System.Numerics;
using CookApps.Obfuscator;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

[UseEffectCodeIds(CodeId)]
public partial class EffectCodeCrowdControlMisaRestraint : EffectCodeCharacterBase
{
    public const int CodeId = (int)EffectCodeNameType.CC_MISA_RESTRAINT;
    public override bool IsRemoveWithSource { get => false; }
    public override EffectCodeType Type { get => EffectCodeType.CrowdControl; }

    private ObfuscatorFloat elapsedTime;
    private ObfuscatorFloat duration;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        duration = codeInfo.GetCodeStatToFloat(0);
        elapsedTime = 0;
        owner.AddCrowdControl(CrowdControlType.MisaRestraint);
        owner.GetCharacterView().SetScale(UnityEngine.Vector3.zero);
    }

    public override void OnPreRemoved()
    {
        owner.RemoveCrowdControl(CrowdControlType.MisaRestraint);
        owner.GetCharacterView().SetScale(UnityEngine.Vector3.one);
        base.OnPreRemoved();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        this.source = source;
        duration = codeInfo.GetCodeStatToFloat(0);
        elapsedTime = 0;
    }

    public override void OnUpdate(float dt)
    {
        elapsedTime += dt;
        if (duration < elapsedTime)
        {
            RemoveFromContainer();
        }
    }
}
