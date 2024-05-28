using System;
using System.Runtime.CompilerServices;

namespace CookApps.TeamBattle.UIManagements
{
    public struct UILayerExitTask
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

    public struct UILayerExitAwaiter : INotifyCompletion
    {
        private UILayerExitTask task;
        private Action continuation;

        public UILayerExitAwaiter(in UILayerExitTask task) : this()
        {
            this.task = task;
            task.uiLayer.ExitEndCallback += OnUILayerClosed;
        }

        public bool IsCompleted { get; private set; }

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            this.continuation = continuation;
        }

        private void OnUILayerClosed(UILayer obj)
        {
            IsCompleted = true;
            task.uiLayer.ExitEndCallback -= OnUILayerClosed;
            continuation?.Invoke();
        }
    }
}
