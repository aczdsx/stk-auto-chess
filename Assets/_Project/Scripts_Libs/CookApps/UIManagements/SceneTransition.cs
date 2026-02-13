using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

namespace CookApps.TeamBattle.UIManagements
{
    public static class SceneTransition
    {
        public enum TransitionType
        {
            FadeIn,
            FadeOut
        }
        public static bool IsFadeProcessing { get; private set; }
        // Static current transition for easy access
        private static SceneTransitionBase Current { get; set; }
        public static event Action<TransitionType> OnTransitionSound;


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

        public static async UniTask FadeInAsync(bool playSound = true)
        {
            if (Current == null)
                return;
            if (playSound)
                OnTransitionSound?.Invoke(TransitionType.FadeIn);
            IsFadeProcessing = true;
            await Current.FadeInAsync();
        }

        public static async UniTask FadeOutAsync(bool playSound = true)
        {
            Debug.Log($"[SceneTransition] FadeOutAsync 호출 - Current null 여부: {Current == null}");
            if (playSound)
                OnTransitionSound?.Invoke(TransitionType.FadeOut);
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
            
            UnityEngine.Object.Destroy(Current.CachedGo);
            Current = null;
        }
    }
}
