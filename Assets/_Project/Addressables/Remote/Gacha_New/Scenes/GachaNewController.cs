using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.Timeline;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// Gacha New - Timeline + Mark + Crosshair.
    /// SetItem -> ConfigureTimeline -> SpawnMarks -> OnMarkFound -> ShowCharacterSequence -> OnClickBack.
    /// </summary>
    public class GachaNewController : MonoBehaviour
    {
        #region Constants

        private const float MARK_PADDING = 100f;
        private const float MARK_MIN_DISTANCE = 120f;
        private const int MARK_PLACEMENT_MAX_RETRY = 30;
        private const int MAX_MARK_COUNT = 10;

        #endregion

        #region Serialized Fields

        [Header("Timeline")]
        [SerializeField] private PlayableDirector _playableDirector;

        [Header("Mark Prefabs")]
        [SerializeField] private GameObject _markPrefabR;
        [SerializeField] private GameObject _markPrefabSR;
        [SerializeField] private GameObject _markPrefabSSR;
        [SerializeField] private RectTransform _markContainer;

        [Header("Crosshair")]
        [SerializeField] private CrosshairController _crosshair;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private GameObject _skipButton;
        [SerializeField] private GameObject _closeButton;

        [Header("Search Count Items (Footer)")]
        [SerializeField] private List<GameObject> _searchCountItems;

        #endregion

        #region Private Fields

        private List<RewardItem> _resultData;
        private GachaInfo _specGachaData;
        private Action _onContinueGacha;
        private Action _onCleanup;
        private Action _onComplete;

        private readonly List<MarkController> _marks = new List<MarkController>(MAX_MARK_COUNT);
        private int _totalMarkCount;
        private int _foundCount;
        private int _skipCount;
        private bool _isTimelinePlaying;
        private bool _isCompleted;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_crosshair != null)
            {
                _crosshair.Initialize(this);

                // 버튼 RectTransform을 크로스헤어에 전달 (입력 필터링용)
                var rects = new List<RectTransform>(2);
                if (_skipButton != null) rects.Add(_skipButton.GetComponent<RectTransform>());
                if (_closeButton != null) rects.Add(_closeButton.GetComponent<RectTransform>());
                _crosshair.SetUIButtonRects(rects.ToArray());
            }

            // 런타임 버튼 이벤트 연결
            WireButton(_skipButton, OnClickSkip);
            WireButton(_closeButton, OnClickBack);
        }

        private void OnDestroy()
        {
            if (_playableDirector != null)
                _playableDirector.stopped -= OnTimelineStopped;

            UnwireButton(_skipButton, OnClickSkip);
            UnwireButton(_closeButton, OnClickBack);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// GachaBaseLayer -> SetItem.
        /// </summary>
        public void SetItem(List<RewardItem> datas, GachaInfo specGachaData, Action onContinueGacha, Action onCleanup, Action onComplete)
        {
            if (datas == null || datas.Count == 0)
            {
                Debug.LogError("[GachaNewController] SetItem: datas is null or empty.");
                onCleanup?.Invoke();
                return;
            }

            _resultData = datas;
            _specGachaData = specGachaData;
            _onContinueGacha = onContinueGacha;
            _onCleanup = onCleanup;
            _onComplete = onComplete;
            _totalMarkCount = Mathf.Min(datas.Count, MAX_MARK_COUNT);
            _skipCount = 0;
            _isTimelinePlaying = true;
            _isCompleted = false;

            // 1) Grade analysis
            GradeType highestGrade = GradeType.RARE;
            bool hasSSR = false;
            for (int i = 0; i < datas.Count; i++)
            {
                GradeType grade = GetGradeFromRewardItem(datas[i]);
                if (grade == GradeType.LEGENDARY) hasSSR = true;
                if (grade > highestGrade) highestGrade = grade;
            }

            // 2) Timeline configuration + play
            ConfigureTimeline(hasSSR, highestGrade);
            _playableDirector.Play();

            // 3) Sound - SSR or Normal start
            if (hasSSR)
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_start_001);
            else
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_start_002);

            // 4) Spawn marks
            SpawnMarks(datas);

            // 5) UI initialization
            _foundCount = 0;
            UpdateCountUI();
            InitSearchCountItems();
            _crosshair.SetEnabled(false);
            _skipButton.SetActive(true);
            _closeButton.SetActive(false);

            // 6) Enable crosshair after timeline completes
            Debug.Log($"[GachaNewController] Timeline duration: {_playableDirector.duration:F2}s, wrapMode: {_playableDirector.extrapolationMode}");
            _playableDirector.stopped += OnTimelineStopped;
            EnableCrosshairAfterTimeline().Forget();

            // 7) Safety fallback: 시간 기반 강제 활성화 (UniTask/stopped 이벤트 실패 대비)
            float safetyDelay = (float)_playableDirector.duration + 1f;
            Run.After(safetyDelay, () =>
            {
                if (_isCompleted || _crosshair == null) return;
                if (!_crosshair.IsEnabled)
                {
                    Debug.LogWarning($"[GachaNewController] Safety fallback → crosshair force-enabled after {safetyDelay:F1}s");
                    _crosshair.SetEnabled(true);
                }
            });
        }

        /// <summary>
        /// MarkController -> OnMarkFound.
        /// </summary>
        public void OnMarkFound(MarkController mark)
        {
            if (_isCompleted) return;

            _foundCount++;
            UpdateCountUI();
            ActivateSearchCountItem(_foundCount - 1);

            if (_foundCount >= _totalMarkCount)
            {
                OnAllMarksFound();
            }
        }

        /// <summary>
        /// CrosshairController -> GetUnfoundMarks.
        /// </summary>
        public List<MarkController> GetUnfoundMarks()
        {
            return _marks;
        }

        /// <summary>
        /// Skip button handler (2-stage).
        /// Stage 1: Timeline 중 → 타임라인 끝으로 점프 + 크로스헤어 활성화.
        /// Stage 2: 탐색 중 (또는 2회차) → 전체 스킵 + 씬 릴리즈 + 결과 팝업.
        /// </summary>
        public void OnClickSkip()
        {
            if (_isCompleted) return;

            _skipCount++;

            if (_skipCount == 1 && _isTimelinePlaying)
            {
                // === Stage 1: Timeline 스킵 → 탐색 Phase 진입 ===
                if (_playableDirector != null && _playableDirector.state == PlayState.Playing)
                {
                    _playableDirector.time = _playableDirector.duration;
                    _playableDirector.Evaluate();
                    _playableDirector.Stop();
                }
                // OnTimelineStopped에서 _isTimelinePlaying=false + crosshair 활성화 처리
                // Skip 버튼은 유지 (2차 스킵 대비)
            }
            else
            {
                // === Stage 2: 전체 스킵 → 씬 릴리즈 + 결과 팝업 ===
                _isCompleted = true;
                _skipButton.SetActive(false);
                _crosshair.SetEnabled(false);

                SoundManager.Instance.StopAllSound();
                SoundManager.Instance.IsPlayingGacha = false;

                if (_playableDirector != null && _playableDirector.state == PlayState.Playing)
                {
                    _playableDirector.time = _playableDirector.duration;
                    _playableDirector.Evaluate();
                    _playableDirector.Stop();
                }

                _foundCount = _totalMarkCount;
                UpdateCountUI();
                ActivateAllSearchCountItems();

                _onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Back/Close button handler.
        /// Restore GachaPopup + destroy gacha scene.
        /// </summary>
        public void OnClickBack()
        {
            if (_isCompleted) return;

            // Restore sound
            SoundManager.Instance.StopSFX(SoundFX.snd_sfx_gacha_result_ambient_001);
            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_command01);
            SoundManager.Instance.IsPlayingGacha = false;

            // Tutorial trigger
            TutorialManager.Instance?.HandleTutorialAction(TutorialTriggerType.CLOSE_POP_COMPLETE, nameof(GachaNewController));

            // 씬 언로드 + MainCanvas/카메라 복원 (GachaBaseLayer.UnloadGachaScene이 처리)
            _onCleanup?.Invoke();
        }

        #endregion

        #region Private Methods — Timeline

        /// <summary>
        /// PlayableDirector.stopped 이벤트 핸들러 (WrapMode.None 시 자동 호출).
        /// </summary>
        private void OnTimelineStopped(PlayableDirector director)
        {
            director.stopped -= OnTimelineStopped;
            _isTimelinePlaying = false;
            if (_isCompleted) return;
            Debug.Log("[GachaNewController] Timeline stopped → crosshair enabled.");
            _crosshair.SetEnabled(true);
        }

        /// <summary>
        /// WrapMode.Hold/Loop 등 stopped 이벤트가 발생하지 않는 경우를 위한 폴백.
        /// PlayableDirector.time이 duration에 도달하면 crosshair 활성화.
        /// </summary>
        private async UniTask EnableCrosshairAfterTimeline()
        {
            if (_playableDirector == null)
            {
                Debug.LogWarning("[GachaNewController] EnableCrosshairAfterTimeline: _playableDirector is null, aborting.");
                return;
            }

            var token = destroyCancellationToken;
            double duration = _playableDirector.duration;

            Debug.Log($"[GachaNewController] EnableCrosshairAfterTimeline started. duration={duration:F2}, state={_playableDirector.state}, extrapolationMode={_playableDirector.extrapolationMode}");

            // Timeline time이 duration에 도달하거나 재생이 멈출 때까지 대기
            try
            {
                await UniTask.WaitUntil(() =>
                    _isCompleted
                    || _playableDirector == null
                    || _playableDirector.time >= duration - 0.1
                    || _playableDirector.state != PlayState.Playing,
                    cancellationToken: token);
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[GachaNewController] EnableCrosshairAfterTimeline: cancelled.");
                return;
            }

            Debug.Log($"[GachaNewController] EnableCrosshairAfterTimeline: WaitUntil resolved. _isCompleted={_isCompleted}, time={_playableDirector?.time:F2}, state={_playableDirector?.state}");

            if (_isCompleted || token.IsCancellationRequested) return;

            // stopped 이벤트에서 이미 활성화했으면 중복 방지
            if (!_crosshair.IsEnabled)
            {
                Debug.Log($"[GachaNewController] Timeline fallback → crosshair enabled (time={_playableDirector?.time:F2}).");
                _crosshair.SetEnabled(true);
            }
        }

        /// <summary>
        /// Configure timeline track muting based on SSR presence and highest grade.
        /// </summary>
        private void ConfigureTimeline(bool hasSSR, GradeType highestGrade)
        {
            // Star sub-tracks: mute all except the highest grade
            SetTimelineTrackMute("SSR", highestGrade != GradeType.LEGENDARY);
            SetTimelineTrackMute("SR", highestGrade != GradeType.EPIC);
            SetTimelineTrackMute("R", highestGrade != GradeType.RARE);

            // Legend/Normal group tracks: mute based on SSR presence
            SetTimelineTrackMute("Legend", !hasSSR);
            SetTimelineTrackMute("Normal", hasSSR);
        }

        /// <summary>
        /// Set mute state on a timeline track by name.
        /// Searches both output tracks and child tracks inside GroupTracks.
        /// </summary>
        private void SetTimelineTrackMute(string trackName, bool muted)
        {
            if (_playableDirector == null) return;

            var timelineAsset = _playableDirector.playableAsset as TimelineAsset;
            if (timelineAsset == null) return;

            // Search output tracks (includes sub-tracks inside groups)
            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track.name == trackName)
                {
                    track.muted = muted;
                    return;
                }
            }

            // Search root tracks for GroupTrack and their children
            foreach (var track in timelineAsset.GetRootTracks())
            {
                if (track is GroupTrack && track.name == trackName)
                {
                    track.muted = muted;
                    return;
                }

                foreach (var child in track.GetChildTracks())
                {
                    if (child.name == trackName)
                    {
                        child.muted = muted;
                        return;
                    }
                }
            }
        }

        #endregion

        #region Private Methods — Marks

        /// <summary>
        /// Spawn mark instances based on reward item grades.
        /// Random placement within _markContainer with padding and minimum distance.
        /// </summary>
        private void SpawnMarks(List<RewardItem> datas)
        {
            // Clear previous marks
            for (int i = 0; i < _marks.Count; i++)
            {
                if (_marks[i] != null)
                    Destroy(_marks[i].gameObject);
            }
            _marks.Clear();

            Rect containerRect = _markContainer.rect;
            float minX = containerRect.xMin + MARK_PADDING;
            float maxX = containerRect.xMax - MARK_PADDING;
            float minY = containerRect.yMin + MARK_PADDING;
            float maxY = containerRect.yMax - MARK_PADDING;

            int count = _totalMarkCount;
            List<Vector2> placedPositions = new List<Vector2>(count);

            for (int i = 0; i < count; i++)
            {
                GradeType grade = GetGradeFromRewardItem(datas[i]);
                GameObject prefab = GetMarkPrefab(grade);

                if (prefab == null)
                {
                    Debug.LogWarning($"[GachaNewController] Mark prefab is null for grade {grade}");
                    continue;
                }

                GameObject markObj = Instantiate(prefab, _markContainer);
                MarkController mark = markObj.GetComponent<MarkController>();
                if (mark == null)
                {
                    Debug.LogError("[GachaNewController] MarkController component not found on mark prefab.");
                    Destroy(markObj);
                    continue;
                }

                // Random placement with minimum distance constraint
                Vector2 position = GetRandomPosition(minX, maxX, minY, maxY, placedPositions);
                mark.RectTransform.anchoredPosition = position;
                placedPositions.Add(position);

                mark.Initialize(this, i);
                _marks.Add(mark);
            }
        }

        /// <summary>
        /// Get a random position within bounds, respecting minimum distance from existing positions.
        /// Retries up to MARK_PLACEMENT_MAX_RETRY times, then accepts the last position.
        /// </summary>
        private Vector2 GetRandomPosition(float minX, float maxX, float minY, float maxY, List<Vector2> existing)
        {
            Vector2 candidate = Vector2.zero;

            for (int attempt = 0; attempt < MARK_PLACEMENT_MAX_RETRY; attempt++)
            {
                candidate = new Vector2(
                    UnityEngine.Random.Range(minX, maxX),
                    UnityEngine.Random.Range(minY, maxY));

                bool tooClose = false;
                for (int j = 0; j < existing.Count; j++)
                {
                    if (Vector2.Distance(candidate, existing[j]) < MARK_MIN_DISTANCE)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose) return candidate;
            }

            // Accept last candidate after max retries
            return candidate;
        }

        /// <summary>
        /// Select mark prefab based on grade.
        /// </summary>
        private GameObject GetMarkPrefab(GradeType grade)
        {
            switch (grade)
            {
                case GradeType.LEGENDARY:
                    return _markPrefabSSR;
                case GradeType.EPIC:
                    return _markPrefabSR;
                default:
                    return _markPrefabR;
            }
        }

        #endregion

        #region Private Methods — Grade

        /// <summary>
        /// Determine grade from RewardItem via character spec data.
        /// Falls back to RARE if character data is unavailable.
        /// </summary>
        private GradeType GetGradeFromRewardItem(RewardItem item)
        {
            if (!item.Id.GetCharacterId(out int charId)) return GradeType.RARE;
            CharacterInfo charInfo = SpecDataManager.Instance.GetCharacterData(charId);
            if (charInfo == null) return GradeType.RARE;
            return charInfo.grade_type;
        }

        #endregion

        #region Private Methods — Completion

        /// <summary>
        /// Called when all marks have been found.
        /// </summary>
        private void OnAllMarksFound()
        {
            _isCompleted = true;
            _crosshair.SetEnabled(false);
            _skipButton.SetActive(false);

            SoundManager.Instance.StopSFX(SoundFX.snd_sfx_gacha_result_ambient_001);
            SoundManager.Instance.IsPlayingGacha = false;

            Run.After(0.5f, () =>
            {
                _onComplete?.Invoke();
            });
        }

        #endregion

        #region Private Methods — UI

        private void UpdateCountUI()
        {
            if (_countText != null)
                _countText.text = $"{_foundCount}/{_totalMarkCount}";
        }

        private void InitSearchCountItems()
        {
            if (_searchCountItems == null) return;
            for (int i = 0; i < _searchCountItems.Count; i++)
            {
                if (_searchCountItems[i] != null)
                    _searchCountItems[i].SetActive(false);
            }
            // _totalMarkCount 초과 아이템은 비활성 유지 (1회 모집 시 1개만 표시)
        }

        private void ActivateSearchCountItem(int index)
        {
            if (_searchCountItems == null || index < 0 || index >= _searchCountItems.Count) return;
            if (_searchCountItems[index] != null)
                _searchCountItems[index].SetActive(true);
        }

        private void ActivateAllSearchCountItems()
        {
            if (_searchCountItems == null) return;
            int count = Mathf.Min(_totalMarkCount, _searchCountItems.Count);
            for (int i = 0; i < count; i++)
            {
                if (_searchCountItems[i] != null)
                    _searchCountItems[i].SetActive(true);
            }
        }

        #endregion

        #region Private Methods — Button Wiring

        private static void WireButton(GameObject buttonObj, UnityEngine.Events.UnityAction action)
        {
            if (buttonObj == null) return;
            var btn = buttonObj.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(action);
        }

        private static void UnwireButton(GameObject buttonObj, UnityEngine.Events.UnityAction action)
        {
            if (buttonObj == null) return;
            var btn = buttonObj.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveListener(action);
        }

        #endregion
    }
}
