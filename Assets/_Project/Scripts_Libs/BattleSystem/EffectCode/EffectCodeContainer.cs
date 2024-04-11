using System.Collections.Generic;
using CookApps.TeamBattle.Utility;

namespace CookApps.TeamBattle.BattleSystem
{
    public class EffectCodeContainer
    {
        private List<EffectCodeBase> effectCodes = new ();
        public List<EffectCodeBase> EffectCodes => effectCodes;
        private object owner;
        public object Owner => owner;

        public delegate void EffectCodeFlagDirtyDelegate(EffectCodeInheritFlag dirtyFlag);

        public event EffectCodeFlagDirtyDelegate dirtyFlagEvent;

        public delegate void EffectCodeTypeDirtyDelegate(EffectCodeType dirtyType);

        public event EffectCodeTypeDirtyDelegate dirtyTypeEvent;

        public EffectCodeContainer(object onwer)
        {
            owner = onwer;
            dirtyFlagEvent = null;
            dirtyTypeEvent = null;
        }

        public void Clear()
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                effectCodes[i].OnPreRemoved();
                EffectCodePoolManager.Instance.Push(effectCodes[i]);
            }

            owner = null;
            effectCodes.Clear();
            effectCodesDividedByFlag.Clear();
            isEffectCodesDividedByFlagDirty.Clear();
            effectCodesDividedByType.Clear();
            isEffectCodesDividedByTypeDirty.Clear();
            dirtyFlagEvent = null;
            dirtyTypeEvent = null;
        }

        public EffectCodeBase AddEffectCode(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            EffectCodeBase effectCode = EffectCodePoolManager.Instance.GetEffectCodeBase(codeInfo.CodeId);
            if (effectCode == null)
            {
                return null;
            }

            effectCode.Initialize(codeInfo, this, source);
            effectCodes.Add(effectCode);

            // 높은 아이디의 이펙트 코드가 먼저 발동해야하기때문에 정렬을 계속 해준다.
            effectCodes.Sort(EffectCodeBase.SortByPriorityFunc);

            if (!isEffectCodesDividedByTypeDirty.TryAdd(effectCode.Type, true))
            {
                isEffectCodesDividedByTypeDirty[effectCode.Type] = true;
            }
            else
            {
                effectCodesDividedByType.Add(effectCode.Type, new List<EffectCodeBase>());
            }

            dirtyTypeEvent?.Invoke(effectCode.Type);

            if (effectCode is EffectCodeStatBase statEffectCode)
            {
                foreach (EffectCodeInheritFlag flag in statEffectCode.GetFlag().GetUniqueFlags())
                {
                    if (!isEffectCodesDividedByFlagDirty.ContainsKey(flag))
                    {
                        isEffectCodesDividedByFlagDirty.Add(flag, true);
                        effectCodesDividedByFlag.Add(flag, new List<EffectCodeStatBase>());
                    }
                    else
                    {
                        isEffectCodesDividedByFlagDirty[flag] = true;
                    }

                    dirtyFlagEvent?.Invoke(flag);
                }
            }

            return effectCode;
        }

        public EffectCodeBase AddOrMergeEffectCode(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            EffectCodeBase effectCode = null;
            for (var i = 0; i < effectCodes.Count; i++)
            {
                if (effectCodes[i].CodeId == codeInfo.CodeId)
                {
                    effectCodes[i].Merge(codeInfo, source);
                    if (effectCodes.Count <= 0)
                    {
                        break;
                    }

                    effectCode = effectCodes[i];
                    break;
                }
            }

            if (effectCode == null)
            {
                effectCode = EffectCodePoolManager.Instance.GetEffectCodeBase(codeInfo.CodeId);
                if (effectCode == null)
                {
                    return null;
                }

                effectCode.Initialize(codeInfo, this, source);
                effectCodes.Add(effectCode);
                // 높은 아이디의 이펙트 코드가 먼저 발동해야하기때문에 정렬을 계속 해준다.
                effectCodes.Sort(EffectCodeBase.SortByPriorityFunc);
            }

            if (!isEffectCodesDividedByTypeDirty.TryAdd(effectCode.Type, true))
            {
                isEffectCodesDividedByTypeDirty[effectCode.Type] = true;
            }
            else
            {
                effectCodesDividedByType.Add(effectCode.Type, new List<EffectCodeBase>());
            }

            dirtyTypeEvent?.Invoke(effectCode.Type);

            if (effectCode is EffectCodeStatBase statEffectCode)
            {
                foreach (EffectCodeInheritFlag flag in statEffectCode.GetFlag().GetUniqueFlags())
                {
                    if (!isEffectCodesDividedByFlagDirty.ContainsKey(flag))
                    {
                        isEffectCodesDividedByFlagDirty.Add(flag, true);
                        effectCodesDividedByFlag.Add(flag, new List<EffectCodeStatBase>());
                    }
                    else
                    {
                        isEffectCodesDividedByFlagDirty[flag] = true;
                    }

                    dirtyFlagEvent?.Invoke(flag);
                }
            }

            return effectCode;
        }

        public bool RemoveEffectCode(EffectCodeBase effectCode)
        {
            bool isRemoved = effectCodes.Remove(effectCode);

            if (!isRemoved)
            {
                return false;
            }

            effectCode.OnPreRemoved();

            if (!isEffectCodesDividedByTypeDirty.TryAdd(effectCode.Type, true))
            {
                isEffectCodesDividedByTypeDirty[effectCode.Type] = true;
            }
            else
            {
                effectCodesDividedByType.Add(effectCode.Type, new List<EffectCodeBase>());
            }

            dirtyTypeEvent?.Invoke(effectCode.Type);

            if (effectCode is EffectCodeStatBase statEffectCode)
            {
                foreach (EffectCodeInheritFlag flag in statEffectCode.GetFlag().GetUniqueFlags())
                {
                    if (isEffectCodesDividedByFlagDirty.ContainsKey(flag))
                    {
                        isEffectCodesDividedByFlagDirty[flag] = true;
                    }

                    dirtyFlagEvent?.Invoke(flag);
                }
            }

            effectCodes.Remove(effectCode);
            EffectCodePoolManager.Instance.Push(effectCode);
            return true;
        }

        public EffectCodeBase RemoveEffectCode(int effectCodeId)
        {
            var isRemoved = false;
            EffectCodeBase effectCode = null;
            foreach (EffectCodeBase effectCodeItem in effectCodes)
            {
                if (effectCodeItem.CodeId == effectCodeId)
                {
                    effectCode = effectCodeItem;
                    isRemoved = true;
                    break;
                }
            }

            if (!isRemoved)
            {
                return null;
            }

            effectCode.OnPreRemoved();

            if (!isEffectCodesDividedByTypeDirty.TryAdd(effectCode.Type, true))
            {
                isEffectCodesDividedByTypeDirty[effectCode.Type] = true;
            }
            else
            {
                effectCodesDividedByType.Add(effectCode.Type, new List<EffectCodeBase>());
            }

            dirtyTypeEvent?.Invoke(effectCode.Type);

            if (effectCode is EffectCodeStatBase statEffectCode)
            {
                foreach (EffectCodeInheritFlag flag in statEffectCode.GetFlag().GetUniqueFlags())
                {
                    if (isEffectCodesDividedByFlagDirty.ContainsKey(flag))
                    {
                        isEffectCodesDividedByFlagDirty[flag] = true;
                    }

                    dirtyFlagEvent?.Invoke(flag);
                }
            }

            effectCodes.Remove(effectCode);
            EffectCodePoolManager.Instance.Push(effectCode);
            return effectCode;
        }

        public void RemoveEffectCodesAssociatedWithSource(IEffectCodeSource source)
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                if (effectCodes[i].Source == source)
                {
                    EffectCodeBase effectCode = effectCodes[i];

                    if (!effectCode.IsRemoveWithSource)
                    {
                        continue;
                    }

                    if (!isEffectCodesDividedByTypeDirty.TryAdd(effectCode.Type, true))
                    {
                        isEffectCodesDividedByTypeDirty[effectCode.Type] = true;
                    }
                    else
                    {
                        effectCodesDividedByType.Add(effectCode.Type, new List<EffectCodeBase>());
                    }

                    dirtyTypeEvent?.Invoke(effectCode.Type);

                    if (effectCode is EffectCodeStatBase statEffectCode)
                    {
                        foreach (EffectCodeInheritFlag flag in statEffectCode.GetFlag().GetUniqueFlags())
                        {
                            if (isEffectCodesDividedByFlagDirty.ContainsKey(flag))
                            {
                                isEffectCodesDividedByFlagDirty[flag] = true;
                            }

                            dirtyFlagEvent?.Invoke(flag);
                        }
                    }

                    effectCodes[i].OnPreRemoved();
                    EffectCodePoolManager.Instance.Push(effectCodes[i]);
                    effectCodes[i] = null;
                }
            }

            effectCodes.RemoveAll(NullChecker<EffectCodeBase>.NullCheck);
        }

        public void RemoveAllEffectCodesWithoutSourceIsNull()
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                if (effectCodes[i].Source == null)
                {
                    continue;
                }

                {
                    EffectCodeBase effectCode = effectCodes[i];

                    if (!effectCode.IsRemoveWithSource)
                    {
                        continue;
                    }

                    if (!isEffectCodesDividedByTypeDirty.TryAdd(effectCode.Type, true))
                    {
                        isEffectCodesDividedByTypeDirty[effectCode.Type] = true;
                    }
                    else
                    {
                        effectCodesDividedByType.Add(effectCode.Type, new List<EffectCodeBase>());
                    }

                    dirtyTypeEvent?.Invoke(effectCode.Type);

                    if (effectCode is EffectCodeStatBase statEffectCode)
                    {
                        foreach (EffectCodeInheritFlag flag in statEffectCode.GetFlag().GetUniqueFlags())
                        {
                            if (isEffectCodesDividedByFlagDirty.ContainsKey(flag))
                            {
                                isEffectCodesDividedByFlagDirty[flag] = true;
                            }

                            dirtyFlagEvent?.Invoke(flag);
                        }
                    }

                    effectCodes[i].OnPreRemoved();
                    EffectCodePoolManager.Instance.Push(effectCodes[i]);
                    effectCodes[i] = null;
                }
            }

            effectCodes.RemoveAll(NullChecker<EffectCodeBase>.NullCheck);
        }

        // 더티 플래그 방식으로 리스트를 업데이트한다.
        private Dictionary<EffectCodeInheritFlag, List<EffectCodeStatBase>> effectCodesDividedByFlag = new ();
        private Dictionary<EffectCodeInheritFlag, bool> isEffectCodesDividedByFlagDirty = new ();

        public List<EffectCodeStatBase> GetCharacterEffectCodesByFlag(EffectCodeInheritFlag flag)
        {
            if (isEffectCodesDividedByFlagDirty.TryAdd(flag, false))
            {
                effectCodesDividedByFlag.Add(flag, new List<EffectCodeStatBase>());
            }

            if (!isEffectCodesDividedByFlagDirty[flag])
            {
                return effectCodesDividedByFlag[flag];
            }

            effectCodesDividedByFlag[flag].Clear();

            for (var i = 0; i < effectCodes.Count; i++)
            {
                if (effectCodes[i] is not EffectCodeStatBase statEffectCode)
                {
                    continue;
                }

                if (!statEffectCode.GetFlag().HasFlag(flag))
                {
                    continue;
                }

                effectCodesDividedByFlag[flag].Add(statEffectCode);
            }

            isEffectCodesDividedByFlagDirty[flag] = false;

            return effectCodesDividedByFlag[flag];
        }

        private Dictionary<EffectCodeType, List<EffectCodeBase>> effectCodesDividedByType = new ();
        private Dictionary<EffectCodeType, bool> isEffectCodesDividedByTypeDirty = new ();

        public List<EffectCodeBase> GetEffectCodesByType(EffectCodeType type)
        {
            if (isEffectCodesDividedByTypeDirty.TryAdd(type, false))
            {
                effectCodesDividedByType.Add(type, new List<EffectCodeBase>());
            }

            if (isEffectCodesDividedByTypeDirty[type])
            {
                effectCodesDividedByType[type].Clear();
                for (var i = 0; i < effectCodes.Count; i++)
                {
                    if (effectCodes[i].Type == type)
                    {
                        effectCodesDividedByType[type].Add(effectCodes[i]);
                    }
                }

                isEffectCodesDividedByTypeDirty[type] = false;
            }

            return effectCodesDividedByType[type];
        }

        public bool CheckExistEffectCode(int effectCodeId)
        {
            return GetEffectCode(effectCodeId) != null;
        }

        public EffectCodeBase GetEffectCode(int effectCodeId)
        {
            foreach (EffectCodeBase effectCode in effectCodes)
            {
                if (effectCode.CodeId == effectCodeId)
                {
                    return effectCode;
                }
            }

            return null;
        }
    }
}
