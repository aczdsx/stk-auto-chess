using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.Obfuscator;
using CookApps.SampleTeamBattle;
using CookApps.TeamBattle.BattleSystem;
using UnityEngine;

public class EffectCodeCrowdControlEntangle : EffectCodeCharacterBase
{
    public override bool IsRemoveWithSource { get => false; }
    public override EffectCodeType Type { get => EffectCodeType.CrowdControl; }

    private ObfuscatorFloat elapsedTime;
    private ObfuscatorFloat duration;
    private IInGameEffectView effectView;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        duration = codeInfo.GetCodeStatToFloat(0);
        elapsedTime = 0;
        owner.AddCrowdControlWrapped(CrowdControlType.Entangle);
    }

    public override void OnPreRemoved()
    {
        base.OnPreRemoved();
        owner.RemoveCrowdControlWrapped(CrowdControlType.Entangle);
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
