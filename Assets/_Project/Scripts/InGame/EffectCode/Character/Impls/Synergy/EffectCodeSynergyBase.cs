using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using System.Collections.Generic;


public class EffectCodeSynergyBase : EffectCodeCharacterBase
{
    protected List<long> _synergyAddEffectCodeIds = new List<long>();

    public void AddSynergyAddEffectCodeIds(long effectCodeId)
    {
        _synergyAddEffectCodeIds.Add(effectCodeId);
    }
    public void AddSynergyAddEffectCodeIds(EffectCodeNameType effectCodeNameType)
    {
        _synergyAddEffectCodeIds.Add((long)effectCodeNameType);
    }

    public override void OnPreRemoved()
    {
        if (owner != null)
        {
            foreach (var effectCodeId in _synergyAddEffectCodeIds)
            {
                owner.GetEffectCodeContainer().RemoveEffectCode(effectCodeId);
            }
        }

        base.OnPreRemoved();
    }

}
