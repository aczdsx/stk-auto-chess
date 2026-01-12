using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Mono.Cecil.Cil;

namespace CookApps.BattleSystem
{

    public partial class EffectCodeSkillPassiveBase : EffectCodeCharacterBase
    {
        protected virtual EffectCodeBase GetActiveSkillEffectCodeId(long passiveSkillId)
        {
            long skillEffectCodeId = passiveSkillId + 100000000;
            return owner.GetEffectCodeContainer().GetEffectCode((int)skillEffectCodeId) as EffectCodeBase;
        }

        protected virtual long GetOnlyActiveSkillEffectCodeId(long passiveSkillId)
        {
            long skillEffectCodeId = passiveSkillId + 100000000;
            return skillEffectCodeId;
        }

        protected virtual SkillPassive GetSpecSkillPassive(long passiveSkillId)
        {
            return SpecDataManager.Instance.GetSkillPassiveDataList(passiveSkillId).FirstOrDefault();
        }
        
    }
}//117263103
