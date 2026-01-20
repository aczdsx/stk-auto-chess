using System;
using System.Collections.Generic;
using System.Threading;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 클라이언트에서 주기적으로 체크하여 서버에 진행 상황을 보고해야 하는 이벤트들을 관리
    /// </summary>
    public class ClientEventTracker : SingletonMonoBehaviour<ClientEventTracker>
    {
        /// <summary>
        /// 이벤트 트래커 설정
        /// </summary>
        private readonly struct TrackerConfig
        {
            public readonly EventType EventType;
            public readonly int IntervalSec;
            public readonly uint AddCountPerInterval;
            public readonly bool PauseOnBackground;

            public TrackerConfig(EventType eventType, int intervalSec, uint addCountPerInterval, bool pauseOnBackground)
            {
                EventType = eventType;
                IntervalSec = intervalSec;
                AddCountPerInterval = addCountPerInterval;
                PauseOnBackground = pauseOnBackground;
            }
        }

        private static readonly TrackerConfig[] _trackerConfigs =
        {
            new(EventType.ACC_PLAY_TIME, intervalSec: 60, addCountPerInterval: 1, pauseOnBackground: true),
            // 추후 다른 이벤트 타입 추가 가능
            // new(EventType.SOME_OTHER_EVENT, intervalSec: 30, addCountPerInterval: 1, pauseOnBackground: false),
        };

        private readonly Dictionary<EventType, CancellationTokenSource> _ctsMap = new();
        private bool _isTracking;
        private bool _isPaused;

        private void OnEnable()
        {
            AppLifeCycleEventsDispatcher.OnPause += OnAppPause;
            AppLifeCycleEventsDispatcher.OnFocus += OnAppFocus;
            AppLifeCycleEventsDispatcher.OnQuit += OnAppQuit;
        }

        private void OnDisable()
        {
            AppLifeCycleEventsDispatcher.OnPause -= OnAppPause;
            AppLifeCycleEventsDispatcher.OnFocus -= OnAppFocus;
            AppLifeCycleEventsDispatcher.OnQuit -= OnAppQuit;
        }

        /// <summary>
        /// 모든 이벤트 트래킹 시작
        /// </summary>
        public void StartTracking()
        {
            if (_isTracking) return;

            _isTracking = true;
            _isPaused = false;

            for (int i = 0; i < _trackerConfigs.Length; i++)
            {
                StartEventTracker(_trackerConfigs[i]);
            }
        }

        /// <summary>
        /// 모든 이벤트 트래킹 중지
        /// </summary>
        public void StopTracking()
        {
            if (!_isTracking) return;

            foreach (var cts in _ctsMap.Values)
            {
                cts?.Cancel();
            }
            _ctsMap.Clear();
            _isTracking = false;
        }

        private void StartEventTracker(TrackerConfig config)
        {
            if (_ctsMap.ContainsKey(config.EventType))
            {
                _ctsMap[config.EventType]?.Cancel();
            }

            var cts = new CancellationTokenSource();
            _ctsMap[config.EventType] = cts;

            TrackingLoop(config, cts.Token).Forget();
        }

        private async UniTaskVoid TrackingLoop(TrackerConfig config, CancellationToken token)
        {
            int intervalMs = config.IntervalSec * 1000;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await UniTask.Delay(intervalMs, cancellationToken: token);

                    if (_isPaused && config.PauseOnBackground) continue;

                    var specEventData = SpecDataManager.Instance.GetCurrentSpecEvent(config.EventType);
                    if (specEventData == null) continue;

                    await NetManager.Instance.Event.UpdateProgressAsync(
                        (uint)specEventData.event_id,
                        config.AddCountPerInterval,
                        token
                    );
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ClientEventTracker] {config.EventType} Error: {e.Message}");
                }
            }
        }

        private void OnAppPause() => _isPaused = true;
        private void OnAppFocus() => _isPaused = false;
        private void OnAppQuit() => StopTracking();
    }
}
