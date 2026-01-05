using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.Obfuscator;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;

/// <summary>
/// 속박(Entangle) 크라우드 컨트롤 이펙트
/// 
/// 효과: 캐릭터를 속박 상태로 만들어 이동을 제한합니다.
/// - Entangle CC를 추가하여 캐릭터의 이동 능력을 차단합니다.
/// - 지속 시간이 지나면 자동으로 제거됩니다.
/// 
/// 특징:
/// - IsRemoveWithSource = false: 적용한 캐릭터(source)가 제거되어도 지속됩니다.
/// - Merge 시 지속 시간이 갱신됩니다.
/// - 단순히 CC 상태만 관리하며, 추가적인 로직은 CharacterController에서 처리됩니다.
/// </summary>
public partial class EffectCodeCrowdControlEntangle : EffectCodeCharacterBase
{
    public override bool IsRemoveWithSource { get => false; }
    public override EffectCodeType Type { get => EffectCodeType.CrowdControl; }

    private ObfuscatorFloat elapsedTime;
    private ObfuscatorFloat duration;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        duration = codeInfo.GetCodeStatToFloat(0);
        elapsedTime = 0;
        owner.AddCrowdControl(CrowdControlType.Entangle);
    }

    public override void OnPreRemoved()
    {
        owner.RemoveCrowdControl(CrowdControlType.Entangle);
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
