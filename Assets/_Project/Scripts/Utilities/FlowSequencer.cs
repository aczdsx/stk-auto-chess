using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public class FlowSequencer
{
    private readonly Queue<FlowSequence> sequenceQueue = new();

    private bool isRunning = false;
    
    public void AddSequence(FlowSequence sequence, bool autoStart = true)
    {
        sequenceQueue.Enqueue(sequence);
        
        if (autoStart && !isRunning)
            StartSequence();
    }

    public void StartSequence()
    {
        if (sequenceQueue.Count <= 0)
        {
            isRunning = false;
            return;
        }
        
        isRunning = true;
        
        var targetSequence = sequenceQueue.Dequeue();
        targetSequence.InvokeSequence().Forget();
        
        Debug.Log($"FlowSequencer: {targetSequence.sequenceName} sequence started.");
    }

    private void SequenceEnd(FlowSequence sequence)
    {
        Debug.Log($"FlowSequencer: {sequence.sequenceName} sequence ended.");
        
        StartSequence();
    }

    /// <summary>
    /// 만드는 법!
    ///var sequence = new FlowSequencer.FlowSequence.Builder("TEST", flowSequencer).OnFlow(TestFlow).Build();
    /// 넣고싶은 기능 빌더 통해서 넣으면 됩니다
    /// </summary>
    public class FlowSequence
    {
        public readonly string sequenceName;
        private readonly Func<CancellationToken, UniTask> onStartTask;
        private readonly Func<CancellationToken, UniTask> onEndTask;
        private readonly Func<CancellationToken, UniTask> flowTask;

        private readonly FlowSequencer sequencer;
        private readonly CancellationToken cancellationToken;

        private FlowSequence(string sequenceName, Func<CancellationToken, UniTask> onStartTask, Func<CancellationToken, UniTask> onEndTask, Func<CancellationToken, UniTask> flowTask, FlowSequencer sequencer, CancellationToken cancellationToken)
        {
            this.sequenceName = sequenceName;
            this.onStartTask = onStartTask;
            this.onEndTask = onEndTask;
            this.flowTask = flowTask;
            this.sequencer = sequencer;
            this.cancellationToken = cancellationToken;
        }

        public async UniTask InvokeSequence()
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (onStartTask != null)
                    await onStartTask(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                if (flowTask != null)
                    await flowTask(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                if (onEndTask != null)
                    await onEndTask(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"FlowSequencer: {sequenceName} sequence cancelled.");
            }
            finally
            {
                sequencer.SequenceEnd(this);
            }
        }
        
        #region Builder

        public class Builder
        {
            private readonly string sequenceName;
            private readonly FlowSequencer sequencer;
            private Func<CancellationToken, UniTask> onStartTask;
            private Func<CancellationToken, UniTask> onEndTask;
            private Func<CancellationToken, UniTask> flowTask;
            private CancellationToken cancellationToken;

            public Builder(string sequenceName, FlowSequencer sequencer)
            {
                this.sequenceName = sequenceName;
                this.sequencer = sequencer;
            }

            public Builder OnStart(Func<CancellationToken, UniTask> task)
            {
                onStartTask = task;
                return this;
            }

            public Builder OnFlow(Func<CancellationToken, UniTask> task)
            {
                flowTask = task;
                return this;
            }

            public Builder OnEnd(Func<CancellationToken, UniTask> task)
            {
                onEndTask = task;
                return this;
            }

            public Builder WithCancellationToken(CancellationToken token)
            {
                cancellationToken = token;
                return this;
            }

            public FlowSequence Build()
            {
                return new FlowSequence(
                    sequenceName,
                    onStartTask,
                    onEndTask,
                    flowTask,
                    sequencer,
                    cancellationToken
                );
            }
        }

        #endregion
    }
}