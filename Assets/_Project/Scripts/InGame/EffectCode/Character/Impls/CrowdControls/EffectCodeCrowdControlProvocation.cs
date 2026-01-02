using CookApps.Obfuscator;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 도발(Provocation) 크라우드 컨트롤 이펙트
/// 
/// 효과: 캐릭터의 공격 타겟을 도발한 캐릭터(source)로 강제 변경합니다.
/// - 도발이 적용되면 현재 타겟을 저장하고, 도발한 캐릭터를 새로운 타겟으로 설정합니다.
/// - 지속 시간 동안 계속 도발한 캐릭터를 타겟으로 유지합니다.
/// - 도발이 끝나면 이전 타겟으로 복원합니다 (이전 타겟이 살아있는 경우).
/// 
/// 특징:
/// - IsRemoveWithSource = true: 도발한 캐릭터(source)가 제거되면 함께 제거됩니다.
/// - Merge 시 지속 시간이 갱신되고 타겟이 다시 설정됩니다.
/// </summary>
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
