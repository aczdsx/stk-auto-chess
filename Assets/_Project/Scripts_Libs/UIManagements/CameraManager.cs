using System;
using UnityEngine;

namespace CookApps.TeamBattle.UIManagements
{
    public class CameraManager : Singleton<CameraManager>
    {
        public static Camera Main { get; private set; }

        internal void SetMainCamera(Camera camera)
        {
            Main = camera;
        }

        internal void ReleaseMainCamera()
        {
            Main = null;
        }
    }
}
