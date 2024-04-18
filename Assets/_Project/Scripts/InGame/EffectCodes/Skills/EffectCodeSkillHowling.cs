
using CookApps.Obfuscator;
using CookApps.TeamBattle.BattleSystem;
using UnityEngine.Pool;

/// <summary>
/// 예시 스킬 코드
/// {0}초마다 크게 소리쳐 {1}초간 {2} {3} 범위의 적들의 방어력을 {4}% 감소시킵니다.
/// </summary>
[UseEffectCodeIds(10102)]
public class EffectCodeHowling : EffectCodeCharacterBase
{
    private ObfuscatorFloat cooltime;
    private ObfuscatorFloat debuffDuration;
    private ObfuscatorInt range;
    private AttackRangeShape rangeShape;
    private ObfuscatorFloat debuffPower;

    private ObfuscatorFloat elapsedTime;

    private bool isReadyToActivate;
    private bool isSkillActivated;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        cooltime = codeInfo.GetCodeStatToFloat(0);
        debuffDuration = codeInfo.GetCodeStatToFloat(1);
        range = codeInfo.GetCodeStatToInt(2);
        rangeShape = (AttackRangeShape)codeInfo.GetCodeStatToInt(3);
        debuffPower = codeInfo.GetCodeStatToFloat(4) * 0.01f;
        elapsedTime = 0f;
        isReadyToActivate = false;
        isSkillActivated = false;
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        cooltime = codeInfo.GetCodeStatToFloat(0);
        debuffDuration = codeInfo.GetCodeStatToFloat(1);
        range = codeInfo.GetCodeStatToInt(2);
        rangeShape = (AttackRangeShape)codeInfo.GetCodeStatToInt(3);
        debuffPower = codeInfo.GetCodeStatToFloat(4);
    }

    public override void OnUpdate(float dt)
    {
        if (isReadyToActivate || isSkillActivated)
            return;

        elapsedTime += dt;
        if (elapsedTime >= cooltime)
        {
            elapsedTime = 0f;
            isSkillActivated = true;
        }
    }

    // public override void OnAttack()
    // {
    //     base.OnAttack();
    //     // 공격시에 쿨타임을 줄임
    //     elapsedTime += 0.5f;
    // }

    public override bool IsReadyToActivate()
    {
        return isReadyToActivate;
    }

    public override void Activate()
    {
        base.Activate();
        // 공격할 타겟부터 설정. 이미 공격중인 타겟이 있으면 스킵
        if (owner.Target is not {IsAlive: true}) // owner.Target == null || !owner.Target.IsAlive
        {
            // 검색 방식에 따라 타겟을 찾음
            owner.Target = InGameObjectManager.Instance.GetNearestEnemy(owner);
            if (owner.Target == null)
            {
                return;
            }
        }

        isReadyToActivate = false;
        isSkillActivated = true;
        var state = owner.AddNextState<CharacterStateSkill>();
        state.SetEffectCode(this);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        // 주변 적에게 데미지를 입힘
        using var _ = ListPool<CharacterController>.Get(out var enemies);
        InGameObjectManager.Instance.GetNearestEnemiesInRange(owner.Target, range, rangeShape, enemies);
        foreach (var enemy in enemies)
        {
            if (enemy == owner.Target || !enemy.IsAlive)
                continue;
            var debuffCodeInfo = GenericPool<EffectCodeInfo>.Get();
            debuffCodeInfo.Set(codeId*10+1, 0, 2, debuffDuration, debuffPower);
            enemy.GetEffectCodeContainer().AddOrMergeEffectCode(debuffCodeInfo, owner);
        }
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        isSkillActivated = false;
    }
}
