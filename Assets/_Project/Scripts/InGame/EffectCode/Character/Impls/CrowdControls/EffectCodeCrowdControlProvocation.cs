using CookApps.Obfuscator;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public partial class EffectCodeCrowdControlProvocation : EffectCodeCharacterBase
{
    public override bool IsRemoveWithSource { get => true; }
    public override EffectCodeType Type { get => EffectCodeType.CrowdControl; }

    private ObfuscatorFloat elapsedTime;
    private ObfuscatorFloat duration;
    private CharacterController prevTarget;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        duration = codeInfo.GetCodeStatToFloat(0);
        owner.AddCrowdControl(CrowdControlType.Provocation);
        elapsedTime = 0;
        prevTarget = owner.Target;
        owner.Target = source as CharacterController;
    }

    public override void OnPreRemoved()
    {
        // 도발이 끝나면 때리던 적 다시 때리자.
        if (prevTarget is {IsAlive: true})
        {
            owner.Target = prevTarget;
        }

        owner.RemoveCrowdControl(CrowdControlType.Provocation);
        base.OnPreRemoved();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        this.source = source;
        duration = codeInfo.GetCodeStatToFloat(0);
        elapsedTime = 0;
        owner.Target = source as CharacterController;
    }

    public override void OnUpdate(float dt)
    {
        elapsedTime += dt;
        if (duration < elapsedTime)
        {
            RemoveFromContainer();
        }
        else
        {
            owner.Target = source as CharacterController;
        }
    }
}
