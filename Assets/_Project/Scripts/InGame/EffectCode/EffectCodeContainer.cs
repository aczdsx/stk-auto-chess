using System.Collections.Generic;
using CookApps.TeamBattle.Utility;
using UnityEngine.Pool;

namespace CookApps.BattleSystem
{
    public class EffectCodeContainer
    {
        private List<EffectCodeBase> effectCodes = ListPool<EffectCodeBase>.Get();
        public List<EffectCodeBase> EffectCodes => effectCodes;
        private object owner;
        public object Owner => owner;

        public delegate void EffectCodeFlagDirtyDelegate(EffectCodeInheritFlag dirtyFlag);

        public event EffectCodeFlagDirtyDelegate OnChangedDirtyFlag;

        public delegate void EffectCodeTypeDirtyDelegate(EffectCodeType dirtyType);

        public event EffectCodeTypeDirtyDelegate OnChangeDirtyType;

        public EffectCodeContainer(object onwer)
        {
            owner = onwer;
            OnChangedDirtyFlag = null;
            OnChangeDirtyType = null;
        }

        public void Clear()
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                effectCodes[i].OnPreRemoved();
                EffectCodePoolManager.Instance.Return(effectCodes[i]);
            }

            owner = null;
            ListPool<EffectCodeBase>.Release(effectCodes);
            effectCodes = null;
            foreach (KeyValuePair<EffectCodeInheritFlag, List<EffectCodeStatBase>> pair in effectCodesDividedByFlag)
            {
                ListPool<EffectCodeStatBase>.Release(pair.Value);
            }

            effectCodesDividedByFlag = null;
            isEffectCodesDividedByFlagDirty = null;
            foreach (KeyValuePair<EffectCodeType, List<EffectCodeBase>> pair in effectCodesDividedByType)
            {
                ListPool<EffectCodeBase>.Release(pair.Value);
            }

            effectCodesDividedByType = null;
            isEffectCodesDividedByTypeDirty = null;
            OnChangedDirtyFlag = null;
            OnChangeDirtyType = null;
        }

        /// <summary>
        /// 컨테이너에 이펙트코드를 추가한다.
        /// 동일한 codeId가 존재할 경우 해당 코드의 Merge함수를 호출한다.
        /// </summary>
        /// <param name="codeInfo"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public EffectCodeBase AddOrMergeEffectCode(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            // 같은 코드가 있는지 체크
            EffectCodeBase effectCode = null;
            for (var i = 0; i < effectCodes.Count; i++)
            {
                if (effectCodes[i].CodeId == codeInfo.CodeId)
                {
                    effectCodes[i].Merge(codeInfo, source);
                    effectCode = effectCodes[i];
                    break;
                }
            }

            // 없으면 생성
            if (effectCode == null)
            {
                effectCode = EffectCodePoolManager.Instance.Get(codeInfo.CodeId);
                if (effectCode == null)
                {
                    return null;
                }

                effectCode.Initialize(codeInfo, this, source);
                effectCodes.Add(effectCode);
                // 높은 아이디의 이펙트 코드가 먼저 발동해야하기때문에 정렬을 해준다.
                effectCodes.Sort(EffectCodeBase.SortByPriorityFunc);
            }

            // 이펙트 코드가 추가되었으므로 타입별로 나누어진 리스트를 업데이트하기 위해 더티 플래그를 세팅한다.
            if (!isEffectCodesDividedByTypeDirty.TryAdd(effectCode.Type, true))
            {
                isEffectCodesDividedByTypeDirty[effectCode.Type] = true;
            }
            else
            {
                effectCodesDividedByType.Add(effectCode.Type, new List<EffectCodeBase>());
            }

            OnChangeDirtyType?.Invoke(effectCode.Type);

            // 이펙트 코드가 스탯 이펙트 코드일 경우 플래그별로 나누어진 리스트를 업데이트하기 위해 더티 플래그를 세팅한다.
            if (effectCode is EffectCodeStatBase statEffectCode)
            {
                IReadOnlyList<EffectCodeInheritFlag> allFlagTypes = EffectCodeInheritFlagExtensions.GetAllFlagTypes();
                foreach (EffectCodeInheritFlag flag in allFlagTypes)
                {
                    if (!statEffectCode.GetFlag().HasFlag(flag))
                    {
                        continue;
                    }

                    if (isEffectCodesDividedByFlagDirty.TryAdd(flag, true))
                    {
                        effectCodesDividedByFlag.Add(flag, ListPool<EffectCodeStatBase>.Get());
                    }
                    else
                    {
                        isEffectCodesDividedByFlagDirty[flag] = true;
                    }

                    OnChangedDirtyFlag?.Invoke(flag);
                }
            }

            return effectCode;
        }

        /// <summary>
        /// 이펙트 코드 제거
        /// </summary>
        /// <param name="effectCode"></param>
        /// <returns></returns>
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
                effectCodesDividedByType.Add(effectCode.Type, ListPool<EffectCodeBase>.Get());
            }

            OnChangeDirtyType?.Invoke(effectCode.Type);

            if (effectCode is EffectCodeStatBase statEffectCode)
            {
                IReadOnlyList<EffectCodeInheritFlag> allFlagTypes = EffectCodeInheritFlagExtensions.GetAllFlagTypes();
                foreach (EffectCodeInheritFlag flag in allFlagTypes)
                {
                    if (!statEffectCode.GetFlag().HasFlag(flag))
                    {
                        continue;
                    }

                    if (isEffectCodesDividedByFlagDirty.ContainsKey(flag))
                    {
                        isEffectCodesDividedByFlagDirty[flag] = true;
                    }

                    OnChangedDirtyFlag?.Invoke(flag);
                }
            }

            effectCodes.Remove(effectCode);
            EffectCodePoolManager.Instance.Return(effectCode);
            return true;
        }

        /// <summary>
        /// codeId로 이펙트코드 제거
        /// </summary>
        public bool RemoveEffectCode(int effectCodeId, out EffectCodeBase effectCode)
        {
            var isRemoved = false;
            effectCode = null;
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
                return false;
            }

            RemoveEffectCode(effectCode);
            return true;
        }

        /// <summary>
        /// source가 같은 이펙트 코드를 제거
        /// 사용 예시.
        /// 주변 동료의 공격력을 n%증가 시키는 캐릭터가 있을 경우
        /// 해당 캐릭터가 죽었을 때 주변 동료의 공격력 증가 효과를 제거해야하고, 이때 사용한다.
        /// </summary>
        /// <param name="source"></param>
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
                        effectCodesDividedByType.Add(effectCode.Type, ListPool<EffectCodeBase>.Get());
                    }

                    OnChangeDirtyType?.Invoke(effectCode.Type);

                    if (effectCode is EffectCodeStatBase statEffectCode)
                    {
                        IReadOnlyList<EffectCodeInheritFlag> allFlagTypes = EffectCodeInheritFlagExtensions.GetAllFlagTypes();
                        foreach (EffectCodeInheritFlag flag in allFlagTypes)
                        {
                            if (!statEffectCode.GetFlag().HasFlag(flag))
                            {
                                continue;
                            }

                            if (isEffectCodesDividedByFlagDirty.ContainsKey(flag))
                            {
                                isEffectCodesDividedByFlagDirty[flag] = true;
                            }

                            OnChangedDirtyFlag?.Invoke(flag);
                        }
                    }

                    effectCodes[i].OnPreRemoved();
                    EffectCodePoolManager.Instance.Return(effectCodes[i]);
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
                effectCodesDividedByFlag.Add(flag, ListPool<EffectCodeStatBase>.Get());
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
                effectCodesDividedByType.Add(type, ListPool<EffectCodeBase>.Get());
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

        public void SetDirtyFlag(EffectCodeBase effectCode)
        {
            if (!isEffectCodesDividedByTypeDirty.TryAdd(effectCode.Type, true))
            {
                isEffectCodesDividedByTypeDirty[effectCode.Type] = true;
            }
            else
            {
                effectCodesDividedByType.Add(effectCode.Type, ListPool<EffectCodeBase>.Get());
            }

            OnChangeDirtyType?.Invoke(effectCode.Type);

            if (effectCode is EffectCodeStatBase statEffectCode)
            {
                IReadOnlyList<EffectCodeInheritFlag> allFlagTypes = EffectCodeInheritFlagExtensions.GetAllFlagTypes();
                foreach (EffectCodeInheritFlag flag in allFlagTypes)
                {
                    if (!statEffectCode.GetFlag().HasFlag(flag))
                    {
                        continue;
                    }

                    if (isEffectCodesDividedByFlagDirty.ContainsKey(flag))
                    {
                        isEffectCodesDividedByFlagDirty[flag] = true;
                    }

                    OnChangedDirtyFlag?.Invoke(flag);
                }
            }
        }
    }
}
