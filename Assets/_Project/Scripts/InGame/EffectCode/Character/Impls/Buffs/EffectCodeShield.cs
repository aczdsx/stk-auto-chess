using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using CookApps.TeamBattle.Utility;
using UnityEngine.Pool;

[UseEffectCodeIds(CodeId)]
public partial class EffectCodeBuffShield : EffectCodeBuffBase
{
    public const int CodeId = (int)EffectCodeNameType.SHIELD;

    public class ShieldData
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
    public List<ShieldData> Shields => shields;

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
        owner.UpdateHpBar();
        
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

        owner.UpdateHpBar();
    }

    public override void OnPreRemoved()
    {
        owner?.RemoveBuffDebuffType(BuffDebuffType.Shield);
        owner?.UpdateHpBar();
        base.OnPreRemoved();
        // 이미 반환된 리스트를 중복 반환하지 않도록 가드
        if (shields != null)
        {
            // 남아있는 데이터가 있으면 개별 반환 처리
            for (int i = 0; i < shields.Count; i++)
            {
                if (shields[i] != null)
                {
                    GenericPool<ShieldData>.Release(shields[i]);
                    shields[i] = null;
                }
            }

            shields.Clear();
            ListPool<ShieldData>.Release(shields);
            shields = null;
        }
    }

    public override void OnUpdate(float dt)
    {
        if (shields == null)
            return;

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

    public override CharacterController.DamageInfo OnDamaged(CharacterController.DamageInfo damageInfo, CharacterController attacker, bool isPure)
    {
        if (shields == null)
            return damageInfo;

        CharacterController.DamageInfo originDamageAmount = damageInfo;
        bool hasShield = false;
        for (int i = 0; i < shields.Count; i++)
        {
            if (shields[i] == null)
                continue;

            hasShield = true;
            shields[i].shieldAmount -= (float)damageInfo.damageAmount.Value;
            if (shields[i].shieldAmount <= 0)
            {
                damageInfo.damageAmount = -(int)shields[i].shieldAmount;
                GenericPool<ShieldData>.Release(shields[i]);
                shields[i] = null;
            }
            else
            {
                damageInfo.damageAmount = 0;
                break;
            }
        }

        if (!hasShield)
            return damageInfo;

        var reducedAmount = originDamageAmount.damageAmount - damageInfo.damageAmount;

        // owner.ShowShieldText(reducedAmount).Forget();

        return damageInfo;
    }
}
