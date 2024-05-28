using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class ThrottleActionBase
{
    protected readonly float delayDuration;
    protected float currDuration;
    protected bool isFastForward;
    protected UniTask currentTask;

    private CancellationTokenSource cts;

    public ThrottleActionBase(float duration)
    {
        delayDuration = duration;
        cts = new ();
    }

    ~ThrottleActionBase()
    {
        // Debug.Log("~ThrottleActionBase");
        cts.Dispose();
    }

    private async UniTask Throttle(CancellationToken token)
    {
        // Debug.Log("Throttle Start");
        isFastForward = false;
        currDuration = delayDuration;
        float prevRealTime = Time.realtimeSinceStartup;
        while (currDuration > 0 && !isFastForward)
        {
            // Debug.Log($"currDuration: {currDuration}, isFastForward: {isFastForward}");
            await UniTask.Yield();
            if (token.IsCancellationRequested)
                throw new TaskCanceledException();
            var deltaTime = Time.realtimeSinceStartup - prevRealTime;
            prevRealTime = Time.realtimeSinceStartup;
            currDuration -= deltaTime;
        }

        await UniTask.SwitchToMainThread();
        // Debug.Log($"Throttle End");
    }

    protected void ThrottleInvoke()
    {
        if (currentTask.Status.IsCompleted())
        {
            // Debug.Log($"ThrottleInvoke");
            currentTask = Throttle(cts.Token).AttachExternalCancellation(cts.Token).ContinueWith(CallEventAction);
        } else
        {
            // Debug.Log($"Throttle Delay");
            currDuration = delayDuration;
        }
    }

    public void FastForward()
    {
        // Debug.Log($"Throttle FastForward");
        isFastForward = true;
    }

    protected void InvokeImmediately()
    {
        // Debug.Log($"Throttle InvokeImmediately");
        cts.Cancel();
        cts = new ();
        CallEventAction();
    }

    protected abstract void CallEventAction();
}

public class ThrottleAction : ThrottleActionBase
{
    public event Action ThrottledEvent;

    public ThrottleAction(float duration) : base(duration)
    {
    }

    ~ThrottleAction()
    {
    }

    public void RemoveAllListeners()
    {
        ThrottledEvent = null;
    }

    public new void ThrottleInvoke()
    {
        base.ThrottleInvoke();
    }

    public new void InvokeImmediately()
    {
        base.InvokeImmediately();
    }

    protected override void CallEventAction()
    {
        ThrottledEvent?.Invoke();
    }
}

public class ThrottleAction<T> : ThrottleActionBase
{
    public event Action<T> ThrottledEvent;
    private T data;

    public ThrottleAction(float duration) : base(duration)
    {
    }

    public void RemoveAllListeners()
    {
        ThrottledEvent = null;
    }

    public void ThrottleInvoke(T data)
    {
        this.data = data;
        ThrottleInvoke();
    }

    public void InvokeImmediately(T data)
    {
        this.data = data;
        InvokeImmediately();
    }

    protected override void CallEventAction()
    {
        ThrottledEvent?.Invoke(data);
    }
}

public class ThrottleAction<T1, T2> : ThrottleActionBase
{
    public event Action<T1, T2> ThrottledEvent;
    private T1 data1;
    private T2 data2;

    public ThrottleAction(float duration) : base(duration)
    {
    }

    public void RemoveAllListeners()
    {
        ThrottledEvent = null;
    }

    public void ThrottleInvoke(T1 data1, T2 data2)
    {
        this.data1 = data1;
        this.data2 = data2;
        ThrottleInvoke();
    }

    public void InvokeImmediately(T1 data1, T2 data2)
    {
        this.data1 = data1;
        this.data2 = data2;
        InvokeImmediately();
    }

    protected override void CallEventAction()
    {
        ThrottledEvent?.Invoke(data1, data2);
    }
}
