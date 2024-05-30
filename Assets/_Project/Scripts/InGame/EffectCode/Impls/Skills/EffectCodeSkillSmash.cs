
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine.Pool;

/// <summary>
/// 예시 스킬 코드
/// {0}초마다 적을 강타하여 공격력의 {1}%의 물리 피해를 입히고, 주변 적에게는 공격력의 {2}%의 물리 피해를 입힙니다.
/// </summary>
[UseEffectCodeIds(10101)]
public class EffectCodeSkillSmash : EffectCodeCharacterBase
{
    private ObfuscatorFloat cooltime;
    private ObfuscatorFloat power;
    private ObfuscatorInt splashRange;
    private AttackRangeShape _splashShapeType;
    private ObfuscatorFloat splashPower;

    private ObfuscatorFloat elapsedTime;

    private bool isReadyToActivate;
    private bool isSkillActivated;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        cooltime = codeInfo.GetCodeStatToFloat(0);
        power = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        splashRange = codeInfo.GetCodeStatToInt(2);
        _splashShapeType = (AttackRangeShape)codeInfo.GetCodeStatToInt(3);
        splashPower = codeInfo.GetCodeStatToFloat(4) * 0.01f;
        elapsedTime = 0f;
        isReadyToActivate = false;
        isSkillActivated = false;
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        cooltime = codeInfo.GetCodeStatToFloat(0);
        power = codeInfo.GetCodeStatToFloat(1);
        splashRange = codeInfo.GetCodeStatToInt(2);
        _splashShapeType = (AttackRangeShape)codeInfo.GetCodeStatToInt(3);
        splashPower = codeInfo.GetCodeStatToFloat(4);
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
        owner.AddNextState<CharacterStateSkill>(this);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;
        // 타겟에게 데미지를 입힘
        var ad = owner.AD * power;
        var damageInfo = owner.PrecalculateDamageAmount(ad, 0, owner.Target, codeId, true);
        owner.PostCalculateDamageAmount(ref damageInfo, owner.Target);
        owner.Target.GetDamaged(in damageInfo, owner);

        // 주변 적에게 데미지를 입힘
        using var _ = ListPool<CharacterController>.Get(out var enemies);
        InGameObjectManager.Instance.GetNearestEnemiesInRange(owner.Target, splashRange, _splashShapeType, enemies);
        foreach (var enemy in enemies)
        {
            if (enemy == owner.Target || !enemy.IsAlive)
                continue;
            ad = owner.AD * splashPower;
            damageInfo = owner.PrecalculateDamageAmount(ad, 0, enemy, codeId, true);
            owner.PostCalculateDamageAmount(ref damageInfo, enemy);
            enemy.GetDamaged(in damageInfo, owner);
        }
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        isSkillActivated = false;
    }
}
