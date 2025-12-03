using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    public class RegisteredObject : CachedMonoBehaviour, IRegistrable
    {
        [SerializeField] private RegistryKey registryKey;
 
        public RegistryKey Key => registryKey;
        
        private void Awake()
        {
            ObjectRegistry.Register(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ObjectRegistry.Unregister(this);
        }
    }
}
