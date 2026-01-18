using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;

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
        if(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat)
        {
            return;
        }
        if (owner != null && owner.GetEffectCodeContainer() != null)
        {
            foreach (var effectCodeId in _synergyAddEffectCodeIds)
            {
                owner.GetEffectCodeContainer().RemoveEffectCode(effectCodeId);
            }
        }

        base.OnPreRemoved();
    }

    protected virtual void RemoveSynergyAddEffectCodeIds(long effectCodeId)
    {
        if (owner == null)
        {
            return;
        }
        var index = -1;
        foreach (var currentEffectCodeId in _synergyAddEffectCodeIds)
        {

            if (currentEffectCodeId == effectCodeId)
            {
                owner.GetEffectCodeContainer().RemoveEffectCode(currentEffectCodeId);
                index = _synergyAddEffectCodeIds.IndexOf(currentEffectCodeId);
                break;
            }
        }
        if (index != -1)
        {
            _synergyAddEffectCodeIds.RemoveAt(index);
        }
    }

}
