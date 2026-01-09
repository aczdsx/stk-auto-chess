using CookApps.TeamBattle.Utility;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public static class MainCameraHolder
    {
        public static Camera MainCamera { get; private set; }
        public static CameraGestureController CameraGestureController { get; private set; }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
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
            CameraGestureController = null;
            
            if (!ObjectRegistry.TryGetObject(RegistryKey.MainCamera, out var registeredObject) || !registeredObject)
                return;
            
            if (registeredObject.TryGetComponent(out Camera camera))
                MainCamera = camera;
            
            if (registeredObject.TryGetComponent(out CameraGestureController controller))
                CameraGestureController = controller;
        }

        public static Vector2 WorldPointToLocalPointInRectangle(Vector3 worldPoint, RectTransform parentTr, Camera uiCamera = null)
        {
            var screenPos = MainCamera.WorldToScreenPoint(worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentTr,
                screenPos,
                uiCamera,
                out Vector2 anchoredPos
            );

            return anchoredPos;
        }
    }
}
