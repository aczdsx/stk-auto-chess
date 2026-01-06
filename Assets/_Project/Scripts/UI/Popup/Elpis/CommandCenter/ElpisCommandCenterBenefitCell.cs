using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 커멘드 센터 혜택 아이템 셀
    /// </summary>
    public class ElpisCommandCenterBenefitCell : MonoBehaviour
    {
        [Header("UI 컴포넌트")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button shortcutButton;

        private ElpisCommandCenterBenefit currentData;
        private int currentIndex;

        /// <summary>
        /// 셀 데이터 설정
        /// </summary>
        public void SetData(ElpisCommandCenterBenefit data, int index)
        {
            currentData = data;
            currentIndex = index;

            UpdateUI();
        }

        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI()
        {
            if (currentData == null)
                return;

            // // 아이콘 설정
            // if (iconImage != null)
            // {
            //     if (currentData.icon != null)
            //     {
            //         iconImage.sprite = currentData.icon;
            //         iconImage.gameObject.SetActive(true);
            //     }
            //     else
            //     {
            //         iconImage.gameObject.SetActive(false);
            //     }
            // }
            //
            // // 타이틀 설정
            // if (titleText != null)
            // {
            //     titleText.text = currentData.title;
            // }
            //
            // // 설명 설정
            // if (descriptionText != null)
            // {
            //     descriptionText.text = currentData.description;
            // }
        }

        /// <summary>
        /// 바로가기 버튼 콜백 설정
        /// </summary>
        public void SetShortcutCallback(System.Action callback)
        {
            if (shortcutButton != null)
            {
                shortcutButton.onClick.RemoveAllListeners();
                shortcutButton.onClick.AddListener(() => callback?.Invoke());
            }
        }

        /// <summary>
        /// 셀 상태 초기화
        /// </summary>
        public void ResetState()
        {
            currentData = null;
            currentIndex = -1;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.gameObject.SetActive(false);
            }

            if (titleText != null)
            {
                titleText.text = string.Empty;
            }

            if (descriptionText != null)
            {
                descriptionText.text = string.Empty;
            }
        }

        /// <summary>
        /// 이벤트 리스너 정리
        /// </summary>
        public void ClearEventListeners()
        {
            if (shortcutButton != null)
            {
                shortcutButton.onClick.RemoveAllListeners();
            }
        }

        private void OnDestroy()
        {
            ClearEventListeners();
        }
    }
}
