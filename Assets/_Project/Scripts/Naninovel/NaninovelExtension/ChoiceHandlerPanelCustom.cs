using System.Collections.Generic;
using LitMotion;
using LitMotion.Extensions;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 커스텀 ChoiceHandlerPanel - 배경 클릭 시 선택지 버튼 쉐이크 효과
    /// </summary>
    public class ChoiceHandlerPanelCustom : ChoiceHandlerPanel
    {
        private const string DIM_OBJECT_NAME = "Dim";

        [Header("Shake Settings")]
        [SerializeField] private float _shakeDuration = 0.5f;
        [SerializeField] private float _shakeStrengthX = 10f;
        [SerializeField] private float _shakeStrengthY = 5f;
        [SerializeField] private int _shakeVibrato = 30;

        private Button _dimButton;
        private readonly Dictionary<RectTransform, MotionHandle> _shakeHandles = new();

        protected override void Awake()
        {
            base.Awake();
            SetupDimButton();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_dimButton != null)
            {
                _dimButton.onClick.RemoveListener(OnDimClicked);
            }
        }

        private void SetupDimButton()
        {
            // Dim 오브젝트 찾기
            var dimTransform = transform.Find(DIM_OBJECT_NAME);
            if (dimTransform == null)
            {
                Debug.LogWarning($"[ChoiceHandlerPanelCustom] '{DIM_OBJECT_NAME}' object not found");
                return;
            }

            // Image의 RaycastTarget 활성화
            var dimImage = dimTransform.GetComponent<Image>();
            if (dimImage != null)
            {
                dimImage.raycastTarget = true;
            }

            // Button 컴포넌트 추가 (없으면)
            _dimButton = dimTransform.GetComponent<Button>();
            if (_dimButton == null)
            {
                _dimButton = dimTransform.gameObject.AddComponent<Button>();
                _dimButton.transition = Selectable.Transition.None;
            }

            _dimButton.onClick.AddListener(OnDimClicked);
        }

        private void OnDimClicked()
        {
            ShakeAllChoiceButtons();
            // SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_popup_warning);
        }

        private void ShakeAllChoiceButtons()
        {
            foreach (var button in ChoiceButtons)
            {
                if (button == null) continue;

                var rectTransform = button.GetComponent<RectTransform>();
                if (rectTransform == null) continue;

                // 기존 쉐이크 트윈 종료
                if (_shakeHandles.TryGetValue(rectTransform, out var prevHandle))
                    prevHandle.TryCancel();

                // 쉐이크 효과 적용 (좌우 + 상하 흔들림)
                var handle = LMotion.Shake.Create(Vector3.zero, new Vector3(_shakeStrengthX, _shakeStrengthY, 0f), _shakeDuration)
                    .WithFrequency(_shakeVibrato)
                    .BindToLocalPosition(rectTransform);
                _shakeHandles[rectTransform] = handle;
            }
        }
    }
}
