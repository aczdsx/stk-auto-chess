
using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle.Utility;
using Naninovel.Commands;
using UnityEngine.Pool;

namespace CookApps.BattleSystem
{
    public class EffectCodeContainerTeam
    {
        private Dictionary<AllianceType, EffectCodeContainer> _teamEccDic;


        public EffectCodeContainerTeam(object owner)
        {
            if (_teamEccDic == null)
            {
                _teamEccDic = new Dictionary<AllianceType, EffectCodeContainer>();
            }
            _teamEccDic.Add(AllianceType.None, new EffectCodeContainer(owner));
            _teamEccDic.Add(AllianceType.Player, new EffectCodeContainer(owner));
            _teamEccDic.Add(AllianceType.Enemy, new EffectCodeContainer(owner));
        }

        public void Clear()
        {
            foreach (var ecc in _teamEccDic.Values)
            {
                ecc.Clear();
            }
            _teamEccDic.Clear();
            _teamEccDic = null;
        }

        public void AddOrMergeEffectCode(EffectCodeInfo codeInfo, IEffectCodeSource source, AllianceType allianceType = AllianceType.None)
        {
            _teamEccDic[allianceType].AddOrMergeEffectCode(codeInfo, source);
        }

        public IReadOnlyList<EffectCodeStatBase> GetCharacterEffectCodesByFlag(EffectCodeInheritFlag effectCodeInheritFlag)
        {
            List<EffectCodeStatBase> outEffectCodeStatBaseList = ListPool<EffectCodeStatBase>.Get();
            foreach (var ecc in _teamEccDic.Values)
            {
                outEffectCodeStatBaseList.AddRange(ecc.GetCharacterEffectCodesByFlag(effectCodeInheritFlag));
            }
            return outEffectCodeStatBaseList;
        }

        public IReadOnlyList<EffectCodeBase> GetEffectCodesByTypeByFlag(EffectCodeType effectCodeType)
        {
            List<EffectCodeBase> outEffectCodeBaseList = ListPool<EffectCodeBase>.Get();
            foreach (var ecc in _teamEccDic.Values)
            {
                outEffectCodeBaseList.AddRange(ecc.GetEffectCodesByType(effectCodeType));
            }
            return outEffectCodeBaseList;
        }
        public void RemoveEffectCodesAssociatedWithSource(IEffectCodeSource source, AllianceType allianceType = AllianceType.None)
        {
            _teamEccDic[allianceType].RemoveEffectCodesAssociatedWithSource(source);
        }

        public bool RemoveEffectCode(long effectCodeId, AllianceType allianceType = AllianceType.None)
        {
            var eraseSuccess = _teamEccDic[allianceType].RemoveEffectCode(effectCodeId);
            if (_teamEccDic[allianceType].CheckExistEffectCode((int)effectCodeId))
            {
                _teamEccDic[allianceType].RemoveEffectCode(effectCodeId);
            }

            return eraseSuccess;
        }



    }
}