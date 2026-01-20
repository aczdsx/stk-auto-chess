using System;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using R3;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 다운로드 진행률 팝업 파라미터
    /// </summary>
    public class DownloadProgressPopupData
    {
        public readonly AssetReference VideoAssetReference;
        public readonly long TotalDownloadSizeBytes;
        public readonly Action OnComplete;
        public readonly Action OnCancel;

        public DownloadProgressPopupData(
            AssetReference videoAssetReference,
            long totalDownloadSizeBytes,
            Action onComplete = null,
            Action onCancel = null)
        {
            VideoAssetReference = videoAssetReference;
            TotalDownloadSizeBytes = totalDownloadSizeBytes;
            OnComplete = onComplete;
            OnCancel = onCancel;
        }
    }

    /// <summary>
    /// Addressables 다운로드 진행률 표시 팝업 (영상 루프 재생 포함)
    /// </summary>
    public class DownloadProgressPopup : UILayerPopupBase
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private TextMeshProUGUI _downloadedSizeText;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private Image _progressFillImage;

        [Header("Video Player")]
        [SerializeField] private AddressableVideoPlayer _videoPlayer;
        [SerializeField] private RawImage _videoRawImage;
        [SerializeField] private GameObject _videoContainer;

        [Header("Optional")]
        [SerializeField] private CAButton _cancelButton;
        [SerializeField] private TextMeshProUGUI _cancelButtonText;

        private DownloadProgressPopupData _popupData;
        private float _currentProgress;
        private long _downloadedBytes;
        private bool _isComplete;

        protected override void Awake()
        {
            base.Awake();

            if (_cancelButton != null)
            {
                _cancelButton.OnClickAsObservable()
                    .Subscribe(this, (_, self) => self.OnClickCancelButton())
                    .AddTo(this);
            }
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            _popupData = param as DownloadProgressPopupData;
            if (_popupData == null)
            {
                Debug.LogError("[DownloadProgressPopup] Invalid popup data");
                return;
            }

            _currentProgress = 0f;
            _downloadedBytes = 0;
            _isComplete = false;

            SetupUI();
            LoadAndPlayVideoAsync().Forget();
        }

        protected override void OnPostExit()
        {
            base.OnPostExit();

            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
            }
        }

        private void SetupUI()
        {
            // 타이틀 텍스트 설정
            if (_titleText != null)
            {
                _titleText.text = LanguageManager.Instance.GetDefaultText("DOWNLOAD_PROGRESS_TITLE");
            }

            // 진행률 초기화
            UpdateProgressUI(0f, 0);

            // 취소 버튼 텍스트 설정
            if (_cancelButtonText != null)
            {
                _cancelButtonText.text = LanguageManager.Instance.GetDefaultText("COMMON_CANCEL");
            }

            // 슬라이더 초기화
            if (_progressSlider != null)
            {
                _progressSlider.value = 0f;
            }
        }

        private async UniTaskVoid LoadAndPlayVideoAsync()
        {
            if (_videoPlayer == null || _popupData.VideoAssetReference == null)
            {
                // 비디오가 없으면 비디오 컨테이너 숨김
                if (_videoContainer != null)
                {
                    _videoContainer.SetActive(false);
                }
                return;
            }

            if (_videoContainer != null)
            {
                _videoContainer.SetActive(true);
            }

            await _videoPlayer.LoadAndPlayAsync(_popupData.VideoAssetReference);
        }

        /// <summary>
        /// 다운로드 진행률 업데이트 (외부에서 호출)
        /// </summary>
        /// <param name="progress">0.0 ~ 1.0 사이의 진행률</param>
        /// <param name="downloadedBytes">다운로드된 바이트 수</param>
        public void UpdateProgress(float progress, long downloadedBytes)
        {
            _currentProgress = Mathf.Clamp01(progress);
            _downloadedBytes = downloadedBytes;
            UpdateProgressUI(_currentProgress, _downloadedBytes);
        }

        private void UpdateProgressUI(float progress, long downloadedBytes)
        {
            // 진행률 퍼센트 표시
            if (_progressText != null)
            {
                int percentage = Mathf.RoundToInt(progress * 100f);
                _progressText.text = $"{percentage}%";
            }

            // 다운로드 크기 표시
            if (_downloadedSizeText != null)
            {
                string downloadedStr = DownloadConfirmPopup.FormatFileSize(downloadedBytes);
                string totalStr = DownloadConfirmPopup.FormatFileSize(_popupData.TotalDownloadSizeBytes);
                _downloadedSizeText.text = $"{downloadedStr} / {totalStr}";
            }

            // 슬라이더 업데이트
            if (_progressSlider != null)
            {
                _progressSlider.value = progress;
            }
        }

        /// <summary>
        /// 다운로드 완료 처리
        /// </summary>
        public void OnDownloadComplete()
        {
            if (_isComplete)
            {
                return;
            }

            _isComplete = true;
            UpdateProgress(1f, _popupData.TotalDownloadSizeBytes);

            _popupData?.OnComplete?.Invoke();
            SceneUILayerManager.Instance.PopUILayer(this, true);
        }

        /// <summary>
        /// 다운로드 실패 처리
        /// </summary>
        public void OnDownloadFailed(string errorMessage = null)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Debug.LogError($"[DownloadProgressPopup] Download failed: {errorMessage}");
            }

            SceneUILayerManager.Instance.PopUILayer(this, false);
        }

        private void OnClickCancelButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_negative);
            _popupData?.OnCancel?.Invoke();
            SceneUILayerManager.Instance.PopUILayer(this, false);
        }

        /// <summary>
        /// 취소 버튼 표시/숨김
        /// </summary>
        public void SetCancelButtonVisible(bool visible)
        {
            if (_cancelButton != null)
            {
                _cancelButton.gameObject.SetActive(visible);
            }
        }
    }
}
