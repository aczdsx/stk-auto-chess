using System;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.Localization.Components;
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
        [Header("Localized Strings")]
        [SerializeField] private LocalizeStringEvent _downloadSizeLocalizeEvent;

        [Header("Buttons")]
        [SerializeField] private CAButton _confirmButton;
        [SerializeField] private CAButton _cancelButton;

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
            // 다운로드 크기를 Smart String 인자로 설정
            // 테이블 값: "다운로드 크기: {0}"
            _downloadSizeLocalizeEvent.StringReference.Arguments = new object[]
            {
                FormatFileSize(_popupData.DownloadSizeBytes)
            };
            _downloadSizeLocalizeEvent.RefreshString();
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
            Application.Quit();
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

        protected override void OnBackButton(ref bool offPrevUI) { }
    }
}
