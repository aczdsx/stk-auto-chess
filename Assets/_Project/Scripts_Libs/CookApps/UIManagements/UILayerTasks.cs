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
        private object result;

        public UILayerExitAwaiter(in UILayerExitTask task)
        {
            uiLayer = task.uiLayer;
            SceneUILayerManager.OnUITransitionEvent += OnUILayerClosed;
        }

        public bool IsCompleted { get; private set; }

        public object GetResult() => result;

        public void OnCompleted(Action continuation)
        {
            if (IsCompleted)
            {
                continuation?.Invoke();
                return;
            }

            this.continuation += continuation;
        }

        private void OnUILayerClosed(UILayerTransition transition, string key, UILayer uiLayer, object param)
        {
            if (transition != UILayerTransition.ExitFinished)
                return;
            if (this.uiLayer != uiLayer)
                return;

            SceneUILayerManager.OnUITransitionEvent -= OnUILayerClosed;
            result = param;
            IsCompleted = true;

            var cachedContinuation = continuation;
            continuation = null;
            cachedContinuation?.Invoke();
        }
    }

    public class UILayerExitTask<T>
    {
        public UILayer uiLayer;

        public UILayerExitTask(UILayer uiLayer)
        {
            this.uiLayer = uiLayer;
        }

        public UILayerExitAwaiter<T> GetAwaiter()
        {
            return new UILayerExitAwaiter<T>(this);
        }
    }

    public sealed class UILayerExitAwaiter<T> : INotifyCompletion
    {
        private readonly UILayer uiLayer;
        private Action continuation;
        private T result;

        public UILayerExitAwaiter(in UILayerExitTask<T> task)
        {
            uiLayer = task.uiLayer;
            SceneUILayerManager.OnUITransitionEvent += OnUILayerClosed;
        }

        public bool IsCompleted { get; private set; }

        public T GetResult() => result;

        public void OnCompleted(Action continuation)
        {
            if (IsCompleted)
            {
                continuation?.Invoke();
                return;
            }

            this.continuation += continuation;
        }

        private void OnUILayerClosed(UILayerTransition transition, string key, UILayer uiLayer, object param)
        {
            if (transition != UILayerTransition.ExitFinished)
                return;
            if (this.uiLayer != uiLayer)
                return;

            SceneUILayerManager.OnUITransitionEvent -= OnUILayerClosed;
            result = param is T typed ? typed : default;
            IsCompleted = true;

            var cachedContinuation = continuation;
            continuation = null;
            cachedContinuation?.Invoke();
        }
    }
}
