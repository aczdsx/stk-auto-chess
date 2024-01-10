using System;
using System.Collections.Generic;
using CookApps.Obfuscator;
using UnityEngine;

namespace CookApps.TeamBattle.BattleSystem
{
    public class InGameMainFlowManager : SingletonMonoBehaviour<InGameMainFlowManager>
    {
        private ObfuscatorFloat OneTick;

        public const int UpdatePriority_TopTier = int.MaxValue;
        public const int UpdatePriority_Objects = 0;
        public const int UpdatePriority_DOTween = -10000;
        public const int UpdatePriority_BottomTier = int.MinValue;

        private bool isPaused;
        public bool IsPaused => isPaused;
        private ObfuscatorFloat fastForwardRate;
        public float FastForwardRate => fastForwardRate;

        public void Clear()
        {
            isPaused = false;
            fastForwardRate = 1f;
            updateHandlers.Clear();
            lateUpdateHandlers.Clear();
            flowState = null;
            nextStates.Clear();
        }

        public void StartInGameMainLoop()
        {
            OneTick = 1f / Application.targetFrameRate;
            prevProcessingTime = Time.time;
            prevLateProcessingTime = Time.time;
            // Debug.Log("Start >> prevProcessingTime: " + prevProcessingTime + ", unscaledTime: " + Time.unscaledTime);
            deltaTime = 0f;
            isPaused = false;
            fastForwardRate = 1f;
            AddUpdateListener(UpdatePriority_TopTier, ManagedUpdate);

            // flowState = StatePool.Instance.GetState<FlowStateStageStart>();
            // CADebug.Assert(flowState != null, "시작 Flow State를 생성하지 못했다!");

            flowState.StateInit(this);
            flowState.StateStart();
        }

        public void StopInGameMainLoop()
        {
            updateHandlers.Clear();
            lateUpdateHandlers.Clear();
            flowState.StateEnd(true);
            // InGameStatePool.Instance.Push(flowState);
            while (nextStates.Count > 0)
            {
                flowState = nextStates.Dequeue();
                // InGameStatePool.Instance.Push(flowState);
            }
        }

        #region Managing update loop
        private ObfuscatorFloat prevProcessingTime;
        private ObfuscatorFloat deltaTime;

        public delegate void UpdateEventHandler(float dt);

        private class HandlerWithPriority
        {
            public int priority;
            public UpdateEventHandler handler;
        };

        private List<HandlerWithPriority> updateHandlers = new ();
        private bool isDirtyUpdateHandlers;

        private void Update()
        {
            if (isPaused)
            {
                prevProcessingTime = Time.time - deltaTime;
                return;
            }

            // Debug.Log("UpdateTick >> isPaused: " + isPaused + ", prevProcessingTime: " + prevProcessingTime + ", unscaledTime: " + Time.unscaledTime);
            float fastForwardRate = this.fastForwardRate;

            deltaTime = Time.time - prevProcessingTime;
            // 델타 타임이 너무 길게 들어오면 백그라운드 뭐 그런거 갓다온거다 스킵해주자
            if (deltaTime > 0.5f)
            {
                deltaTime %= OneTick * fastForwardRate;
            }

            if (isDirtyUpdateHandlers)
            {
                updateHandlers.Sort((a, b) => b.priority - a.priority);
                isDirtyUpdateHandlers = false;
            }

#if CALL_ALL_FRAME
        var unitTime = OneTick / fastForwardRate;
        while (deltaTime > unitTime)
        {
            deltaTime -= unitTime;
            for (int i = 0; i < updateHandlers.Count; i++)
            {
                updateHandlers[i].handler.Invoke(OneTick);
            }
        }
#elif CALL_MERGE_FRAME
        int count = 0;
        while (deltaTime > OneTick)
        {
            deltaTime -= OneTick;
            ++count;
        }
        // call
        if (count > 0)
        {
            for (int i = 0; i < updateHandlers.Count; i++)
            {
                updateHandlers[i].handler.Invoke(OneTick * fastForwardRate * count);
            }
        }
#else
            for (var i = 0; i < updateHandlers.Count; i++)
            {
                updateHandlers[i].handler.Invoke(deltaTime * fastForwardRate);
            }

            deltaTime = 0f;
#endif
            prevProcessingTime = Time.time - deltaTime;
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Resume()
        {
            isPaused = false;
        }

        public void SetPlaySpeed(float speed)
        {
            fastForwardRate = speed;
        }

        public void AddUpdateListener(int priority, UpdateEventHandler handler)
        {
            updateHandlers.Add(new HandlerWithPriority {priority = priority, handler = handler});
            isDirtyUpdateHandlers = true;
        }

        public void RemoveUpdateListener(UpdateEventHandler handler)
        {
            for (var i = 0; i < updateHandlers.Count; i++)
            {
                if (updateHandlers[i].handler == handler)
                {
                    updateHandlers.RemoveAt(i);
                    break;
                }
            }
        }
        #endregion

        #region Managing late update loop
        private ObfuscatorFloat prevLateProcessingTime;
        private ObfuscatorFloat lateDeltaTime;
        private List<HandlerWithPriority> lateUpdateHandlers = new ();
        private bool isDirtyLateUpdateHandlers;

        private void LateUpdate()
        {
            if (isPaused)
            {
                prevLateProcessingTime = Time.time - lateDeltaTime;
                return;
            }

            // Debug.Log("UpdateTick >> isPaused: " + isPaused + ", prevProcessingTime: " + prevProcessingTime + ", unscaledTime: " + Time.unscaledTime);
            float fastForwardRate = this.fastForwardRate;

            lateDeltaTime = Time.time - prevLateProcessingTime;
            // 델타 타임이 너무 길게 들어오면 백그라운드 뭐 그런거 갓다온거다 스킵해주자
            if (lateDeltaTime > 0.5f)
            {
                lateDeltaTime %= OneTick * fastForwardRate;
            }

            if (isDirtyLateUpdateHandlers)
            {
                lateUpdateHandlers.Sort((a, b) => b.priority - a.priority);
                isDirtyLateUpdateHandlers = false;
            }

#if CALL_ALL_FRAME
        var unitTime = OneTick / fastForwardRate;
        while (lateDeltaTime > unitTime)
        {
            lateDeltaTime -= unitTime;
            for (int i = 0; i < lateUpdateHandlers.Count; i++)
            {
                lateUpdateHandlers[i].handler.Invoke(OneTick);
            }
        }
#elif CALL_MERGE_FRAME
        int count = 0;
        while (lateDeltaTime > OneTick)
        {
            lateDeltaTime -= OneTick;
            ++count;
        }
        // call
        if (count > 0)
        {
            for (int i = 0; i < lateUpdateHandlers.Count; i++)
            {
                lateUpdateHandlers[i].handler.Invoke(OneTick * fastForwardRate * count);
            }
        }
#else
            for (var i = 0; i < lateUpdateHandlers.Count; i++)
            {
                lateUpdateHandlers[i].handler.Invoke(lateDeltaTime * fastForwardRate);
            }

            lateDeltaTime = 0f;
#endif
            prevLateProcessingTime = Time.time - lateDeltaTime;
        }

        public void AddLateUpdateListener(int priority, UpdateEventHandler handler)
        {
            lateUpdateHandlers.Add(new HandlerWithPriority {priority = priority, handler = handler});
            isDirtyLateUpdateHandlers = true;
        }

        public void RemoveLateUpdateListener(UpdateEventHandler handler)
        {
            for (var i = 0; i < lateUpdateHandlers.Count; i++)
            {
                if (lateUpdateHandlers[i].handler == handler)
                {
                    lateUpdateHandlers.RemoveAt(i);
                    break;
                }
            }
        }
        #endregion

        #region Managing game flow
        private StateBase flowState;
        public StateBase CurrentFlowState => flowState;
        private Queue<StateBase> nextStates = new ();

        public delegate void GameFlowChangedCallback(StateBase flowState);

        public static event GameFlowChangedCallback flowEventHandler;

        private void ManagedUpdate(float dt)
        {
            if (flowState == null)
            {
                if (nextStates.Count > 0)
                {
                    flowState = nextStates.Dequeue();
                    flowState.StateInit(this);
                    flowState.StateStart();
                    flowEventHandler?.Invoke(flowState);
                }

                return;
            }

            flowState.StateRunning(dt);

            // flow state는 한틱에 두개 이상 등록하면 꼬일 가능성이 커서 두개 이상 들어올때 마지막으로 들어온 것만 처리토록
            while (nextStates.Count > 1)
            {
                StateBase nextState = nextStates.Dequeue();
                StatePool.Instance.Push(nextState);
            }

            if (nextStates.Count > 0)
            {
                flowState.StateEnd(false);
                StatePool.Instance.Push(flowState);
                flowState = nextStates.Dequeue();
                flowState.StateInit(this);
                flowState.StateStart();
                flowEventHandler?.Invoke(flowState);
            }
        }

        public T AddNextState<T>() where T : StateBase, new()
        {
            var state = StatePool.Instance.GetState<T>();
            nextStates.Enqueue(state);
            return state;
        }

        public StateBase AddNextState(Type type)
        {
            StateBase state = StatePool.Instance.GetState(type);
            nextStates.Enqueue(state);
            return state;
        }
        #endregion
    }
}
