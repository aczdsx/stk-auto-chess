using CookApps.AutoBattler;
using CookApps.TeamBattle.UIManagements;
using LitMotion;
using R3;
using TMPro;
using UnityEngine;

namespace CookApps.TeamBattle
{
    public class LobbyBGMPlayer : CachedMonoBehaviour
    {
        private const string NoteSymbol = "♪";

        [SerializeField] private RectTransform maskRect;
        [SerializeField] private TextMeshProUGUI textBGMName;
        [SerializeField] private CAButton btnPause;
        [SerializeField] private CAButton btnPlay;
        [SerializeField] private float scrollDuration = 8f;

        private string _currentAudioID;
        private float _cycleWidth;
        private MotionHandle _scrollHandle;

        private void Awake()
        {
            btnPause.OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickToggle())
                .AddTo(this);

            btnPlay.OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickToggle())
                .AddTo(this);
        }

        private void OnEnable()
        {
            SoundManager.Instance.OnBGMChanged += HandleBGMChanged;
            RefreshBGMInfo();
            UpdateToggleVisual();
        }

        private void OnDisable()
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.OnBGMChanged -= HandleBGMChanged;
            StopScroll();
        }

        private void HandleBGMChanged(string audioID)
        {
            RefreshBGMInfo();
            UpdateToggleVisual();
        }

        private void RefreshBGMInfo()
        {
            _currentAudioID = SoundManager.Instance.GetCurrentBGMId();

            var displayName = FormatBGMName(_currentAudioID);
            if (string.IsNullOrEmpty(displayName))
            {
                textBGMName.text = string.Empty;
                StopScroll();
                return;
            }

            var singleCopy = $"{displayName}  {NoteSymbol}  ";
            float maskWidth = maskRect.rect.width;

            // 두 카피를 이어 붙여서 끊김 없는 루프 구현
            textBGMName.text = singleCopy + singleCopy;
            textBGMName.ForceMeshUpdate();

            // 실제 렌더링된 문자 위치로 정확한 사이클 거리 계산
            var textInfo = textBGMName.textInfo;
            int singleLen = singleCopy.Length;
            if (textInfo.characterCount <= singleLen)
            {
                textBGMName.text = singleCopy;
                StopScroll();
                textBGMName.rectTransform.anchoredPosition = Vector2.zero;
                return;
            }

            _cycleWidth = textInfo.characterInfo[singleLen].origin
                        - textInfo.characterInfo[0].origin;

            // 텍스트가 마스크보다 짧으면 스크롤 불필요
            if (_cycleWidth <= maskWidth)
            {
                textBGMName.text = singleCopy;
                StopScroll();
                textBGMName.rectTransform.anchoredPosition = Vector2.zero;
                return;
            }

            StopScroll();

            if (SoundManager.Instance.IsBGMPlayerPaused)
            {
                textBGMName.rectTransform.anchoredPosition = Vector2.zero;
                return;
            }

            StartScroll();
        }

        private string FormatBGMName(string audioID)
        {
            return SoDataProvider.Instance.Get<AudioConfigSO>().GetDisplayName(audioID);
        }

        private void StartScroll()
        {
            _scrollHandle.TryCancel();
            _scrollHandle = default;

            if (_cycleWidth <= 0f)
                return;

            textBGMName.rectTransform.anchoredPosition = Vector2.zero;

            _scrollHandle = LMotion.Create(0f, -_cycleWidth, scrollDuration)
                .WithEase(Ease.Linear)
                .WithLoops(-1, LoopType.Restart)
                .Bind(x => textBGMName.rectTransform.anchoredPosition = new Vector2(x, 0f))
                .AddTo(this);
        }

        private void OnClickToggle()
        {
            var sm = SoundManager.Instance;

            if (sm.IsBGMPlayerPaused)
            {
                sm.UnPauseBGMPlayer();
                if (!_scrollHandle.IsActive() && _cycleWidth > 0f)
                    StartScroll();
            }
            else
            {
                sm.PauseBGMPlayer();
                _scrollHandle.TryCancel();
                _scrollHandle = default;
                textBGMName.rectTransform.anchoredPosition = Vector2.zero;
            }

            UpdateToggleVisual();
        }

        private void UpdateToggleVisual()
        {
            bool isPaused = SoundManager.Instance.IsBGMPlayerPaused;
            btnPause.gameObject.SetActive(!isPaused);
            btnPlay.gameObject.SetActive(isPaused);
        }

        private void StopScroll()
        {
            _scrollHandle.TryCancel();
            _scrollHandle = default;
        }

        protected override void OnDestroy()
        {
            StopScroll();
            base.OnDestroy();
        }
    }
}
