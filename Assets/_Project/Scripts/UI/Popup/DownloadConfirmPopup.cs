using System;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using R3;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 다운로드 확인 팝업 파라미터
    /// </summary>
    public class DownloadConfirmPopupData
    {
        public readonly long DownloadSizeBytes;
        public readonly Action OnConfirm;
        public readonly Action OnCancel;

        public DownloadConfirmPopupData(long downloadSizeBytes, Action onConfirm = null, Action onCancel = null)
        {
            DownloadSizeBytes = downloadSizeBytes;
            OnConfirm = onConfirm;
            OnCancel = onCancel;
        }
    }

    /// <summary>
    /// Addressables 다운로드 전 데이터량 확인 팝업
    /// </summary>
    public class DownloadConfirmPopup : UILayerPopupBase
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _downloadSizeText;
        [SerializeField] private CAButton _confirmButton;
        [SerializeField] private CAButton _cancelButton;
        [SerializeField] private TextMeshProUGUI _confirmButtonText;
        [SerializeField] private TextMeshProUGUI _cancelButtonText;

        private DownloadConfirmPopupData _popupData;

        protected override void Awake()
        {
            base.Awake();

            _confirmButton.OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickConfirmButton())
                .AddTo(this);

            _cancelButton.OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickCancelButton())
                .AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            _popupData = param as DownloadConfirmPopupData;
            if (_popupData == null)
            {
                Debug.LogError("[DownloadConfirmPopup] Invalid popup data");
                return;
            }

            SetupUI();
        }

        private void SetupUI()
        {
            // 타이틀 텍스트 설정
            if (_titleText != null)
            {
                _titleText.text = LanguageManager.Instance.GetDefaultText("DOWNLOAD_CONFIRM_TITLE");
            }

            // 설명 텍스트 설정
            if (_descriptionText != null)
            {
                _descriptionText.text = LanguageManager.Instance.GetDefaultText("DOWNLOAD_CONFIRM_DESC");
            }

            // 다운로드 크기 표시
            if (_downloadSizeText != null)
            {
                _downloadSizeText.text = FormatFileSize(_popupData.DownloadSizeBytes);
            }

            // 버튼 텍스트 설정
            if (_confirmButtonText != null)
            {
                _confirmButtonText.text = LanguageManager.Instance.GetDefaultText("COMMON_CONFIRM");
            }

            if (_cancelButtonText != null)
            {
                _cancelButtonText.text = LanguageManager.Instance.GetDefaultText("COMMON_CANCEL");
            }
        }

        private void OnClickConfirmButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);
            _popupData?.OnConfirm?.Invoke();
            SceneUILayerManager.Instance.PopUILayer(this, true);
        }

        private void OnClickCancelButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_negative);
            _popupData?.OnCancel?.Invoke();
            SceneUILayerManager.Instance.PopUILayer(this, false);
        }

        /// <summary>
        /// 바이트를 읽기 쉬운 포맷으로 변환 (KB, MB, GB)
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            if (bytes < 0)
            {
                return "0 B";
            }

            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F2} {suffixes[suffixIndex]}";
        }
    }
}
