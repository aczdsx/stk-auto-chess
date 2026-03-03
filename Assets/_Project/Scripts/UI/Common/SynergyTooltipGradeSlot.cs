using CookApps.TeamBattle.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 시너지 툴팁 글자에 하이라이팅을 위한 클래스
    /// </summary>
    public class SynergyTooltipGradeSlot : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private SimpleTextColorSwapper _textColorSwapper;
        [SerializeField] private Image _highlightBg;

        private const float BaseHeight = 50f;
        private const float HeightPerLine = 25f;

        private RectTransform _slotRT;

        private void Awake()
        {
            _slotRT = (RectTransform)transform;
        }

        public void SetGrade(string text, bool isHighlighted)
        {
            _text.text = text;
            _highlightBg.enabled = isHighlighted;
            _textColorSwapper?.Swap(isHighlighted ? SimpleSwapType.Normal : SimpleSwapType.Disabled);
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        public void AdjustHeight()
        {
            _text.ForceMeshUpdate();
            int lineCount = _text.textInfo.lineCount;
            if (lineCount == 0) return;

            float height = BaseHeight + (lineCount - 1) * HeightPerLine;
            _slotRT.sizeDelta = new Vector2(_slotRT.sizeDelta.x, height);
        }
    }
}
