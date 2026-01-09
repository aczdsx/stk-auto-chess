using System;
using System.Runtime.CompilerServices;

namespace CookApps.TeamBattle.UIManagements
{
    public class UILayerExitTask
    {
        public UILayer uiLayer;

        public UILayerExitTask(UILayer uiLayer)
        {
            this.uiLayer = uiLayer;
        }

        public UILayerExitAwaiter GetAwaiter()
        {
            return new UILayerExitAwaiter(this);
        }
    }

    public sealed class UILayerExitAwaiter : INotifyCompletion
    {
        private readonly UILayer uiLayer;
        private Action continuation;

        public UILayerExitAwaiter(in UILayerExitTask task)
        {
            uiLayer = task.uiLayer;
            SceneUILayerManager.OnUITransitionEvent += OnUILayerClosed;
        }

        public bool IsCompleted { get; private set; }

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            if (IsCompleted)
            {
                continuation?.Invoke();
                return;
            }

            this.continuation += continuation;
        }
    
        private void OnUILayerClosed(UILayerTransition transition, string key, UILayer uiLayer)
        {
            if (transition != UILayerTransition.ExitFinished)
                return;
            if (this.uiLayer != uiLayer)
                return;
        
            SceneUILayerManager.OnUITransitionEvent -= OnUILayerClosed;
            IsCompleted = true;

            var cachedContinuation = continuation;
            continuation = null;
            cachedContinuation?.Invoke();
        }
    }
}
