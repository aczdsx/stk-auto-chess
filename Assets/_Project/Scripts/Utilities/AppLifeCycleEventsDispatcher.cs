using System;
using CookApps.TeamBattle;

namespace CookApps.AutoBattler
{
    public class AppLifeCycleEventsDispatcher : SingletonMonoBehaviour<AppLifeCycleEventsDispatcher>
    {
        public static event Action OnQuit;
        public static event Action OnPause;
        public static event Action OnFocus;

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                OnPause?.Invoke();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                OnFocus?.Invoke();
            }
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            OnQuit?.Invoke();
        }
    }
}
