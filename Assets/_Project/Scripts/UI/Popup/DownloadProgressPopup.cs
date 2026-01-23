using System;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Components;
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
        [Header("Localized Strings (Smart String)")]
        [SerializeField] private LocalizeStringEvent _progressLocalizeEvent;
        [SerializeField] private LocalizeStringEvent _downloadedSizeLocalizeEvent;

        [Header("Progress UI")]
        [SerializeField] private Slider _progressSlider;

        [Header("Video Player")]
        [SerializeField] private AddressableVideoPlayer _videoPlayer;
        [SerializeField] private GameObject _videoContainer;

        private DownloadProgressPopupData _popupData;
        private float _currentProgress;
        private long _downloadedBytes;
        private bool _isComplete;

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

            _videoPlayer.Stop();
        }

        private void SetupUI()
        {
            // 진행률 초기화
            UpdateProgressUI(0f, 0);

            // 슬라이더 초기화
            _progressSlider.value = 0f;
        }

        private async UniTaskVoid LoadAndPlayVideoAsync()
        {
            _videoContainer.SetActive(false);
            await _videoPlayer.LoadAndPlayAsync(_popupData.VideoAssetReference);
            _videoContainer.SetActive(true);
            _videoPlayer.Play();
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
            // 진행률 퍼센트 표시 (테이블 값: "{0}%")
            int percentage = Mathf.RoundToInt(progress * 100f);
            _progressLocalizeEvent.StringReference.Arguments = new object[] { percentage };
            _progressLocalizeEvent.RefreshString();

            // 다운로드 크기 표시 (테이블 값: "{0} / {1}")
            string downloadedStr = DownloadConfirmPopup.FormatFileSize(downloadedBytes);
            string totalStr = DownloadConfirmPopup.FormatFileSize(_popupData.TotalDownloadSizeBytes);
            _downloadedSizeLocalizeEvent.StringReference.Arguments = new object[] { downloadedStr, totalStr };
            _downloadedSizeLocalizeEvent.RefreshString();

            // 슬라이더 업데이트
            _progressSlider.value = progress;
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
    }
}
