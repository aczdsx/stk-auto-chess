using CookApps.TeamBattle.Utility;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.TeamBattle.UIManagements
{
    public class DefaultUILayerLoader : CachedMonoBehaviour
    {
        [SerializeField] private AssetReferenceGameObject[] defaultUILayers;

        public AssetReferenceGameObject[] DefaultUILayers => defaultUILayers;

        internal async Awaitable LoadDefaultUILayers(object param)
        {
            Debug.Log($"[DefaultUILayerLoader] LoadDefaultUILayers: param type={param?.GetType().Name ?? "null"}, param value={param}");
            for (var i = 0; i < defaultUILayers.Length; i++)
            {
                var handle = defaultUILayers[i].LoadAssetAsync();
                var prefab = await handle.WaitUntilDone();
                var uiLayer = prefab.GetComponent<UILayer>();
                var type = uiLayer.GetType();
                Debug.Log($"[DefaultUILayerLoader] Loading UI Layer: {type.Name}, param={param}");
                await SceneUILayerManager.Instance.PushUILayerAsync(type, type.Name, param);
            }
        }
    }
}
