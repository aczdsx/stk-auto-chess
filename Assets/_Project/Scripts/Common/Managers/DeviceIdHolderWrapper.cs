using UnityEngine;

public static class DeviceIdHolder
{
    private static string deviceId;

    public static string DeviceId
    {
        get
        {
            if (!string.IsNullOrEmpty(deviceId))
                return deviceId;
#if !UNITY_EDITOR && UNITY_IOS
            deviceId = CookApps.iOS.KeyChain.GetKeyChainValue("saved_device_id");
            if (!string.IsNullOrEmpty(deviceId))
                return deviceId;
#endif
            deviceId = GetDeviceID();

#if !UNITY_EDITOR && UNITY_IOS
            CookApps.iOS.KeyChain.SetKeyChainValue("saved_device_id", deviceId);
#endif
            return deviceId;
        }
    }

    private static string GetDeviceID()
    {
        var result = string.Empty;
#if UNITY_IPHONE
		result = UnityEngine.iOS.Device.vendorIdentifier;
#endif
        if (string.IsNullOrEmpty(result))
            result = SystemInfo.deviceUniqueIdentifier;

        return result;
    }
}
