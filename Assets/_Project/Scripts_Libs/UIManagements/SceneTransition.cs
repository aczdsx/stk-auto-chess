using UnityEngine;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.UIManagements
{
    public static class SceneTransition
    {
        public static bool IsFadeProcessing { get; set; }
        // Static current transition for easy access
        private static SceneTransitionBase Current { get; set; }

        public static void Create<T>(object viewOption = null) where T : SceneTransitionBase, new()
        {
            // Replace any existing transition
            if (Current != null)
            {
                Clear();
            }

            var parent = SceneUILayerManager.Instance != null ? SceneUILayerManager.Instance.TransitionNode : null;
            var go = new GameObject(typeof(T).Name, typeof(RectTransform), typeof(T));
            var rect = go.GetComponent<RectTransform>();
            if (parent != null)
            {
                rect.SetParent(parent, false);
            }
            // Default to full-screen stretch
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            var comp = go.GetComponent<T>();
            Current = comp;
            comp.Initialize(viewOption);
        }

        public static async UniTask FadeInAsync()
        {
            if (Current == null)
                return;
            IsFadeProcessing = true;
            await Current.FadeInAsync();
        }

        public static async UniTask FadeOutAsync()
        {
            if (Current == null)
                return;
            await Current.FadeOutAsync();
            IsFadeProcessing = false;
            Clear();
        }
        
        private static void Clear()
        {
            if (Current == null)
                return;
            
            Object.Destroy(Current.CachedGo);
            Current = null;
        }
    }
}
