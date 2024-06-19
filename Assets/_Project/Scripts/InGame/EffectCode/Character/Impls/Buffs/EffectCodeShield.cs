using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using CookApps.TeamBattle.Utility;
using UnityEngine.Pool;

[UseEffectCodeIds(CodeId)]
public class EffectCodeBuffShield : EffectCodeBuffBase
{
    public const int CodeId = (int)CharacterEffectType.SHIELD;
    private class ShieldData
    {
        public ObfuscatorFloat elapsedTime;
        public ObfuscatorFloat buffDuration;
        public ObfuscatorDouble shieldAmount;

        public bool AddDeltaTime(float dt)
        {
            elapsedTime += dt;
            return elapsedTime > buffDuration;
        }
    }

    private List<ShieldData> shields = null;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        shields = ListPool<ShieldData>.Get();
        var effectCodes = container.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseModifyShieldAmount);
        var shieldAmount = codeInfo.GetCodeStat(1);
        shieldAmount = EffectCodeForLoopHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallModifyShieldAmountLambda, shieldAmount);

        var shieldData = GenericPool<ShieldData>.Get();
        shieldData.elapsedTime = 0f;
        shieldData.buffDuration = codeInfo.GetCodeStatToFloat(0);
        shieldData.shieldAmount = shieldAmount;
        shields.Add(shieldData);

        owner.AddBuffDebuffType(BuffDebuffType.Shield);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        var effectCodes = container.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseModifyShieldAmount);
        var shieldAmount = codeInfo.GetCodeStat(1);

        shieldAmount = EffectCodeForLoopHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallModifyShieldAmountLambda, shieldAmount);

        var shieldData = GenericPool<ShieldData>.Get();
        shieldData.elapsedTime = 0f;
        shieldData.buffDuration = codeInfo.GetCodeStatToFloat(0);
        shieldData.shieldAmount = shieldAmount;
        shields.Add(shieldData);
    }

    public override void OnPreRemoved()
    {
        owner.RemoveBuffDebuffType(BuffDebuffType.Shield);
        base.OnPreRemoved();
        ListPool<ShieldData>.Release(shields);
    }

    public override void OnUpdate(float dt)
    {
        bool needRemove = false;
        for (int i = 0; i < shields.Count; i++)
        {
            if (shields[i] == null)
            {
                needRemove = true;
                continue;
            }

            if (shields[i].AddDeltaTime(dt))
            {
                GenericPool<ShieldData>.Release(shields[i]);
                shields[i] = null;
                needRemove = true;
            }
        }

        if (needRemove)
        {
            shields.RemoveAll(NullChecker<ShieldData>.NullCheck);
            if (shields.Count <= 0)
            {
                RemoveFromContainer();
            }
        }
    }

    public override double OnDamaged(double damageAmount, CharacterController attacker, bool isPure)
    {
        double originDamageAmount = damageAmount;
        bool hasShield = false;
        for (int i = 0; i < shields.Count; i++)
        {
            if (shields[i] == null)
                continue;

            hasShield = true;
            shields[i].shieldAmount -= (float)damageAmount;
            if (shields[i].shieldAmount <= 0)
            {
                damageAmount = -(int)shields[i].shieldAmount;
                GenericPool<ShieldData>.Release(shields[i]);
                shields[i] = null;
            }
            else
            {
                damageAmount = 0;
                break;
            }
        }

        if (!hasShield)
            return damageAmount;

        var reducedAmount = originDamageAmount - damageAmount;
        // [TODO]: show shield damage text

        return damageAmount;
    }

    public override bool IsNeedToShowIcon()
    {
        return false;
    }
}
