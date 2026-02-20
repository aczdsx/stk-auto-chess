using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class SynergyGradeSlot : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
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
