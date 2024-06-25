using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommanderSkillUI : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _coolTimeImage;
    [SerializeField] private TextMeshProUGUI _coolTimeText;

    public void UpdateCommanderSkillCoolTime(float elapsedTime, float durationTime)
    {
        float rate = elapsedTime / durationTime;
        _coolTimeText.text = $"{durationTime - elapsedTime:F1}";
        _coolTimeImage.fillAmount = rate;
    }

    public void SetIconColor(float fadeAlpha)
    {
        if (_iconImage != null)
        {
            Color color = _iconImage.color;
            color.a = fadeAlpha;
            _iconImage.color = color;
        }
    }
}
