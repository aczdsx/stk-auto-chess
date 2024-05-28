using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class SwallowDelayActionBase
{
    protected float delayDuration;
    protected float currDuration;
    protected bool isFastForward;
    protected UniTask currentTask;

    private CancellationTokenSource cts;

    public SwallowDelayActionBase(float duration)
    {
        delayDuration = duration;
        cts = new ();
    }

    ~SwallowDelayActionBase()
    {
        cts.Dispose();
    }

    public void SetDuration(float duration)
    {
        delayDuration = duration;
    }

    private async UniTask Swallow(CancellationToken token)
    {
        isFastForward = false;
        currDuration = delayDuration;
        float prevRealTime = Time.realtimeSinceStartup;
        while (currDuration > 0 && !isFastForward)
        {
            await UniTask.Yield(PlayerLoopTiming.PreLateUpdate);
            if (token.IsCancellationRequested)
                throw new TaskCanceledException();
            var deltaTime = Time.realtimeSinceStartup - prevRealTime;
            prevRealTime = Time.realtimeSinceStartup;
            currDuration -= deltaTime;
        }

        await UniTask.SwitchToMainThread();
    }

    protected void DelayedInvoke()
    {
        if (currentTask.Status.IsCompleted())
        {
            currentTask = Swallow(cts.Token).AttachExternalCancellation(cts.Token).ContinueWith(CallEventAction);
        }
    }

    public void FastForward()
    {
        isFastForward = true;
    }

    protected void InvokeImmediately()
    {
        cts.Cancel();
        cts = new ();
        CallEventAction();
    }

    protected virtual void CallEventAction()
    {
        Debug.LogError($"SwallowDelayActionBase: run {Time.frameCount}");
    }
}

public class SwallowDelayAction : SwallowDelayActionBase
{
    private event Action DelayedEvent;

    public SwallowDelayAction(float duration) : base(duration)
    {
    }

    ~SwallowDelayAction()
    {
    }

    public void RemoveAllListeners()
    {
        DelayedEvent = null;
    }

    public new void DelayedInvoke()
    {
        base.DelayedInvoke();
    }

    public new void InvokeImmediately()
    {
        base.InvokeImmediately();
    }

    protected override void CallEventAction()
    {
        base.CallEventAction();
        DelayedEvent?.Invoke();
    }
}

public class SwallowDelayAction<T> : SwallowDelayActionBase
{
    public event Action<T> DelayedEvent;
    private T data;

    public SwallowDelayAction(float duration) : base(duration)
    {
    }

    public void RemoveAllListeners()
    {
        DelayedEvent = null;
    }

    public void DelayedInvoke(T data)
    {
        this.data = data;
        DelayedInvoke();
    }

    public void InvokeImmediately(T data)
    {
        this.data = data;
        InvokeImmediately();
    }

    protected override void CallEventAction()
    {
        base.CallEventAction();
        DelayedEvent?.Invoke(data);
    }
}

public class SwallowDelayAction<T1, T2> : SwallowDelayActionBase
{
    public event Action<T1, T2> DelayedEvent;
    private T1 data1;
    private T2 data2;

    public SwallowDelayAction(float duration) : base(duration)
    {
    }

    public void RemoveAllListeners()
    {
        DelayedEvent = null;
    }

    public void DelayedInvoke(T1 data1, T2 data2)
    {
        this.data1 = data1;
        this.data2 = data2;
        DelayedInvoke();
    }

    public void InvokeImmediately(T1 data1, T2 data2)
    {
        this.data1 = data1;
        this.data2 = data2;
        InvokeImmediately();
    }

    protected override void CallEventAction()
    {
        base.CallEventAction();
        DelayedEvent?.Invoke(data1, data2);
    }
}
