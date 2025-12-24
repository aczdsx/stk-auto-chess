using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.Utility
{
    // Intentionally no CreateAssetMenu to hide from the Create menu.
    public class SafeAreaSettings : ScriptableObject
    {
        [Header("Anchor Margin Multipliers (0 = ignore, 1 = exact safe area)")]
        [Range(0f, 1f)] public float left = 1f;
        [Range(0f, 1f)] public float right = 1f;
        [Range(0f, 1f)] public float top = 1f;
        [Range(0f, 1f)] public float bottom = 1f;

        public static SafeAreaSettings Active { get; private set; }

        public const string DefaultAddress = "Data/SafeAreaSettings.asset";

        public static void SetActive(SafeAreaSettings settings)
        {
            Active = settings;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoLoad()
        {
            InitializeAsync().Forget();
        }

        private static async UniTask InitializeAsync()
        {
            if (Active != null)
                return;

            // 1) Try Addressables by default address
            try
            {
                var handle = Addressables.LoadAssetAsync<SafeAreaSettings>(DefaultAddress);
                var so = await handle.WaitUntilDone();
                if (so != null)
                {
                    Active = so;
                    return;
                }
            }
            catch { /* ignore and fallback */ }

            // 2) Fallback to common Resources paths
            Active = Resources.Load<SafeAreaSettings>("SafeAreaSettings");
            if (Active == null)
                Active = Resources.Load<SafeAreaSettings>("SafeArea/SafeAreaSettings");
        }
    }
}
