using CookApps.TeamBattle.Utility;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public static class MainCameraHolder
    {
        public static Camera MainCamera { get; private set; }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Init()
        {
            ObjectRegistry.Registered += ObjectRegistryOnRegistered;
            ObjectRegistry.Unregistered += ObjectRegistryOnUnregistered;
        }

        private static void ObjectRegistryOnRegistered(RegistryKey regKey, IRegistrable regObj)
        {
            if (regKey == RegistryKey.MainCamera)
            {
                RefreshMainCamera();
            }
        }
        
        private static void ObjectRegistryOnUnregistered(RegistryKey regKey, IRegistrable regObj)
        {
            if (regKey == RegistryKey.MainCamera)
            {
                RefreshMainCamera();
            }
        }
        
        private static void RefreshMainCamera()
        {
            MainCamera = null;
            if (ObjectRegistry.TryGetObject(RegistryKey.MainCamera, out var registeredObject))
            {
                MainCamera = registeredObject != null ? registeredObject.GetComponent<Camera>() : null;
            }
        }

        public static Vector2 WorldPointToLocalPointInRectangle(Vector2 worldPoint, RectTransform parentTr)
        {
            var screenPos = MainCamera.WorldToScreenPoint(worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentTr,
                screenPos,
                null,
                out Vector2 anchoredPos
            );

            return anchoredPos;
        }
    }
}
