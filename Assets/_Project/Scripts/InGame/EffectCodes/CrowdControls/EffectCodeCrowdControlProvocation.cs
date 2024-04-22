using CookApps.Obfuscator;
using CookApps.SampleTeamBattle;
using CookApps.TeamBattle.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.TeamBattle.BattleSystem.CharacterController;

public class EffectCodeCrowdControlProvocation : EffectCodeCharacterBase
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
        owner.AddCrowdControlWrapped(CrowdControlType.Provocation);
        elapsedTime = 0;
        prevTarget = owner.Target;
        owner.Target = source as CharacterController;
    }

    public override void OnPreRemoved()
    {
        base.OnPreRemoved();
        // 도발이 끝나면 때리던 적 다시 때리자.
        if (prevTarget is {IsAlive: true})
        {
            owner.Target = prevTarget;
        }

        owner.RemoveCrowdControlWrapped(CrowdControlType.Provocation);
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
    }
}
