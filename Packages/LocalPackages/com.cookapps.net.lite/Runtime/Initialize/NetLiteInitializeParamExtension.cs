/*
* Copyright (c) CookApps.
*/

using CookApps.NetLite.Constants;

namespace CookApps.NetLite.Initialize
{
    internal static class NetLiteInitializeParamExtension
    {
        /// <summary>
        /// NetLiteInitializeParam의 값이 유효한지 검사
        /// </summary>
        public static bool IsValid(this NetLiteInitializeParam param)
        {
            // Address와 Store가 유효한지 검사하고, 각각 유효하지 않으면 로그 출력
            bool isAddressValid = !string.IsNullOrEmpty(param.Address);
            bool isStoreValid = param.Store != StoreMap.Unspecified;

            if (!isAddressValid)
                UnityEngine.Debug.LogError("NetLiteInitializeParam: Address가 null이거나 비어 있습니다.");

            if (!isStoreValid)
                UnityEngine.Debug.LogError("NetLiteInitializeParam: Store가 Unspecified입니다.");

            return isAddressValid && isStoreValid;
        }
    }
}
